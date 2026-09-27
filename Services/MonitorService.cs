using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Omniroute.Api;
using Omniroute.Models;
using Omniroute.Protocol;

namespace Omniroute.Services;

/// <summary>
/// Фоновий сервіс моніторингу станцій через MQTT.
/// Тримає з'єднання (з повторним входом і наростаючою затримкою), збирає телеметрію,
/// раз на хвилину пише історію та перевіряє правила сповіщень.
/// </summary>
public static class MonitorService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan OfflineAfter = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan QuotaRefreshInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);

    private static readonly IDeviceProtocol Delta2 = new Delta2Protocol(isMax: false);
    private static readonly IDeviceProtocol Delta2Max = new Delta2Protocol(isMax: true);

    /// <summary>
    /// Останні параметри станції. Словник після публікації не змінюється,
    /// тому його можна читати без блокування.
    /// </summary>
    private sealed record Snapshot(DeviceParams Params, DateTime LastSeen);

    // Усі поля нижче захищені _lock
    private static readonly object _lock = new();
    private static CancellationTokenSource? _cts;
    private static MqttLink? _link;
    private static string? _userId;
    private static HashSet<string> _subscribed = new();
    private static Dictionary<string, Snapshot> _snapshots = new();

    private static DispatcherQueue? _dispatcher;
    private static long _lastRecordedMinute;
    private static DateTime _lastQuotaRequest = DateTime.MinValue;
    private static DateTime _lastCleanup = DateTime.MinValue;

    /// <summary>
    /// Поточний стан з'єднання для інтерфейсу
    /// </summary>
    public static string Status { get; private set; } = "Зупинено";

    /// <summary>
    /// Змінився стан з'єднання. Викликається в UI-потоці.
    /// </summary>
    public static event Action<string>? StatusChanged;

    public static bool IsRunning
    {
        get { lock (_lock) return _cts != null; }
    }

    /// <summary>
    /// Запам'ятати UI-потік: на ньому оновлюються станції та показуються сповіщення
    /// </summary>
    public static void Initialize(DispatcherQueue dispatcher)
    {
        _dispatcher = dispatcher;
        App.Repository.DevicesChanged += OnDevicesChanged;
    }

    /// <summary>
    /// Запустити моніторинг
    /// </summary>
    public static void Start()
    {
        CancellationToken token;
        lock (_lock)
        {
            if (_cts != null) return;
            _cts = new CancellationTokenSource();
            token = _cts.Token;
        }

        _ = Task.Run(() => ConnectLoopAsync(token));
        _ = Task.Run(() => TickLoopAsync(token));

        if (App.Repository.Credentials?.HasDeveloperKeys == true)
        {
            _ = Task.Run(SyncStationsSafeAsync);
        }
    }

    /// <summary>
    /// Зупинити моніторинг
    /// </summary>
    public static void Stop()
    {
        CancellationTokenSource? cts;
        MqttLink? link;
        lock (_lock)
        {
            cts = _cts;
            link = _link;
            _cts = null;
            _link = null;
            _userId = null;
            _subscribed = new HashSet<string>();
            _snapshots = new Dictionary<string, Snapshot>();
        }

        cts?.Cancel();
        link?.Dispose();
        SetStatus("Зупинено");

        // Без моніторингу станції не можуть вважатися онлайн
        _dispatcher?.TryEnqueue(() =>
        {
            foreach (var device in App.Repository.Devices)
            {
                device.IsOnline = false;
            }
        });
    }

    #region З'єднання

    private static async Task ConnectLoopAsync(CancellationToken token)
    {
        var backoff = MinBackoff;

        while (!token.IsCancellationRequested)
        {
            var credentials = App.Repository.Credentials;
            if (credentials == null)
            {
                SetStatus("Не виконано вхід");
                return;
            }

            SetStatus("Підключення…");
            MqttLink? link = null;

            try
            {
                // Токен і облікові дані MQTT отримуємо заново при кожному підключенні
                Session session;
                MqttCredentials mqtt;
                using (var cloud = new EcoflowCloud())
                {
                    session = await cloud.LoginAsync(credentials.ApiHost, credentials.Email, credentials.Password);
                    mqtt = await cloud.GetMqttCredentialsAsync(credentials.ApiHost, session);
                }

                var failure = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                link = new MqttLink(mqtt, session.UserId, OnMessage, OnConnectionChanged,
                    reason => failure.TrySetResult(reason));

                var serials = App.Repository.ActiveDevices.Select(d => d.SerialNumber).ToHashSet();
                lock (_lock)
                {
                    token.ThrowIfCancellationRequested();
                    _link = link;
                    _userId = session.UserId;
                    _subscribed = serials;
                }

                await link.ConnectAsync(serials.SelectMany(sn => TopicsFor(session.UserId, sn)));
                backoff = MinBackoff;

                // Далі MqttLink перепідключається сам; сюди повертаємось лише після фатальної помилки
                var reason = await failure.Task.WaitAsync(token);
                SetStatus($"Перепідключення… ({reason})");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                ReleaseLink(link);
                return;
            }
            catch (EcoflowException ex) when (ex.IsAuthError)
            {
                // Пароль змінено або обліковий запис недоступний: повтори не допоможуть,
                // зупиняємось, щоб після нового входу Start() запустив моніторинг знову
                ReleaseLink(link);
                StopOwned(token);
                SetStatus($"Помилка входу: {ex.Message}");
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MonitorService: connect failed - {ex}");
                SetStatus($"Перепідключення… ({ex.Message})");
            }

            ReleaseLink(link);

            try
            {
                await Task.Delay(backoff, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            backoff = TimeSpan.FromTicks(Math.Min(backoff.Ticks * 2, MaxBackoff.Ticks));
        }
    }

    /// <summary>
    /// Зупинити запуск, якому належить token (якщо його ще не замінив новий Start)
    /// </summary>
    private static void StopOwned(CancellationToken token)
    {
        CancellationTokenSource? cts = null;
        lock (_lock)
        {
            if (_cts != null && _cts.Token == token)
            {
                cts = _cts;
                _cts = null;
            }
        }
        cts?.Cancel();
    }

    private static void ReleaseLink(MqttLink? link)
    {
        if (link == null) return;
        lock (_lock)
        {
            if (_link == link)
            {
                _link = null;
                _userId = null;
            }
        }
        link.Dispose();
    }

    private static void OnConnectionChanged(bool connected)
    {
        SetStatus(connected ? "Підключено" : "Зв'язок втрачено, перепідключення…");
        if (connected)
        {
            _ = RequestAllQuotasAsync();
        }
    }

    private static IEnumerable<string> TopicsFor(string userId, string sn) => new[]
    {
        $"/app/device/property/{sn}",
        $"/app/{userId}/{sn}/thing/property/get_reply",
        $"/app/{userId}/{sn}/thing/property/set_reply"
    };

    /// <summary>
    /// Привести підписки у відповідність до списку станцій
    /// </summary>
    private static async void OnDevicesChanged()
    {
        try
        {
            MqttLink? link;
            string? userId;
            List<string> added, removed;

            var current = App.Repository.ActiveDevices.Select(d => d.SerialNumber).ToHashSet();
            lock (_lock)
            {
                link = _link;
                userId = _userId;
                if (link == null || userId == null)
                    return;

                added = current.Except(_subscribed).ToList();
                removed = _subscribed.Except(current).ToList();
                _subscribed = current;

                if (removed.Count > 0)
                {
                    _snapshots = _snapshots
                        .Where(kv => !removed.Contains(kv.Key))
                        .ToDictionary(kv => kv.Key, kv => kv.Value);
                }
            }

            await link.SubscribeAsync(added.SelectMany(sn => TopicsFor(userId, sn)));
            await link.UnsubscribeAsync(removed.SelectMany(sn => TopicsFor(userId, sn)));

            foreach (var sn in added)
            {
                var device = App.Repository.GetDevice(sn);
                if (device != null)
                    await RequestQuotaAsync(link, userId, device);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MonitorService: subscription sync failed - {ex.Message}");
        }
    }

    #endregion

    #region Повідомлення

    /// <summary>
    /// Обробка MQTT повідомлення (потік MQTT)
    /// </summary>
    private static void OnMessage(string topic, byte[] payload)
    {
        string sn;
        TopicKind kind;

        if (topic.StartsWith("/app/device/property/", StringComparison.Ordinal))
        {
            sn = topic[(topic.LastIndexOf('/') + 1)..];
            kind = TopicKind.Data;
        }
        else if (topic.EndsWith("/get_reply", StringComparison.Ordinal) || topic.EndsWith("/set_reply", StringComparison.Ordinal))
        {
            // /app/{userId}/{sn}/thing/property/get_reply
            var parts = topic.Split('/');
            if (parts.Length < 4) return;
            sn = parts[3];
            kind = topic.EndsWith("/get_reply", StringComparison.Ordinal) ? TopicKind.GetReply : TopicKind.SetReply;
        }
        else
        {
            return;
        }

        var device = App.Repository.GetDevice(sn);
        if (device == null)
            return;

        var parsed = ProtocolFor(device.Model).Parse(kind, payload);

        lock (_lock)
        {
            if (_cts == null)
                return;

            // Новий словник замість зміни старого: читачі тримають незмінну копію
            _snapshots.TryGetValue(sn, out var old);
            var merged = old != null ? new DeviceParams(old.Params) : new DeviceParams();
            foreach (var kv in parsed)
            {
                merged[kv.Key] = kv.Value;
            }

            // Будь-який трафік від станції означає, що вона на зв'язку, навіть якщо кадр не розібрано
            _snapshots[sn] = new Snapshot(merged, DateTime.Now);
        }
    }

    private static async Task RequestAllQuotasAsync()
    {
        MqttLink? link;
        string? userId;
        lock (_lock)
        {
            link = _link;
            userId = _userId;
        }
        if (link == null || userId == null)
            return;

        _lastQuotaRequest = DateTime.Now;
        foreach (var device in App.Repository.ActiveDevices)
        {
            await RequestQuotaAsync(link, userId, device);
        }
    }

    private static Task<bool> RequestQuotaAsync(MqttLink link, string userId, Device device)
    {
        var request = ProtocolFor(device.Model).QuotaRequest(device.SerialNumber);
        return link.PublishAsync($"/app/{userId}/{device.SerialNumber}/thing/property/get", request.Payload);
    }

    #endregion

    #region Періодична обробка

    private static async Task TickLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await TickAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MonitorService: tick failed - {ex}");
            }

            try
            {
                await Task.Delay(TickInterval, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private static async Task TickAsync()
    {
        var now = DateTime.Now;
        var minute = now.Ticks / TimeSpan.TicksPerMinute;
        var record = minute != _lastRecordedMinute;

        Dictionary<string, Snapshot> snapshots;
        lock (_lock)
        {
            snapshots = _snapshots;
        }

        var updates = new List<(Device Device, DeviceState State, bool Online, DateTime LastSeen)>();
        foreach (var device in App.Repository.ActiveDevices)
        {
            snapshots.TryGetValue(device.SerialNumber, out var snap);
            var online = snap != null && now - snap.LastSeen < OfflineAfter;
            var state = ProtocolFor(device.Model).GetState(snap?.Params ?? new DeviceParams());
            updates.Add((device, state, online, snap?.LastSeen ?? default));

            if (record && online)
            {
                await App.Repository.AddHistoryEntryAsync(new HistoryEntry
                {
                    SerialNumber = device.SerialNumber,
                    Timestamp = new DateTime(minute * TimeSpan.TicksPerMinute),
                    BatteryLevel = state.Soc ?? 0,
                    BatteryWatts = NetBatteryWatts(state),
                    InputWatts = state.InputW,
                    OutputWatts = state.OutputW,
                    Temperature = state.BatteryTempC,
                    HasAcInput = state.GridConnected ?? false
                });
            }
        }
        if (record)
            _lastRecordedMinute = minute;

        // Станції надсилають лише зміни; періодичний запит повного стану оновлює рідкісні поля
        if (now - _lastQuotaRequest > QuotaRefreshInterval)
            await RequestAllQuotasAsync();

        if (now - _lastCleanup > TimeSpan.FromDays(1))
        {
            _lastCleanup = now;
            await App.Repository.CleanupOldHistoryAsync();
        }

        // Властивості станцій прив'язані до інтерфейсу — змінюємо їх лише в UI-потоці
        _dispatcher?.TryEnqueue(() =>
        {
            foreach (var (device, state, online, lastSeen) in updates)
            {
                ApplyState(device, state, online, lastSeen);
                NotificationService.Evaluate(device, state, online);
            }
        });
    }

    private static void ApplyState(Device device, DeviceState state, bool online, DateTime lastSeen)
    {
        device.IsOnline = online;
        device.LastSeen = lastSeen;
        device.BatteryLevel = state.Soc;
        device.BatteryWatts = NetBatteryWatts(state);
        device.InputWatts = state.InputW;
        device.OutputWatts = state.OutputW;
        device.SolarWatts = state.SolarW;
        device.Temperature = state.BatteryTempC;
        device.Cycles = state.Cycles;
        device.GridConnected = state.GridConnected;
        device.TimeRemaining = state.ChargeRemainMin ?? state.DischargeRemainMin;
    }

    /// <summary>
    /// Баланс батареї: вхід мінус вихід (додатне — заряджається)
    /// </summary>
    private static int? NetBatteryWatts(DeviceState state) =>
        state.InputW.HasValue || state.OutputW.HasValue
            ? (state.InputW ?? 0) - (state.OutputW ?? 0)
            : null;

    #endregion

    /// <summary>
    /// Отримати протокол для пристрою
    /// </summary>
    private static IDeviceProtocol ProtocolFor(DeviceModel model)
    {
        return model switch
        {
            DeviceModel.Delta2 => Delta2,
            DeviceModel.Delta2Max => Delta2Max,
            // TODO: Додати інші протоколи (Delta Max, River 2 Max, Delta 3, Delta Pro 3)
            _ => Delta2
        };
    }

    private static async Task SyncStationsSafeAsync()
    {
        try
        {
            await App.Repository.SyncStationsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MonitorService: station sync failed - {ex.Message}");
        }
    }

    private static void SetStatus(string status)
    {
        Status = status;
        if (_dispatcher == null || _dispatcher.HasThreadAccess)
            StatusChanged?.Invoke(status);
        else
            _dispatcher.TryEnqueue(() => StatusChanged?.Invoke(status));
    }
}
