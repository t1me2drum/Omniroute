using Omniroute.Api;
using Omniroute.Data;
using Omniroute.Models;
using Omniroute.Protocol;
using System.Collections.Concurrent;

namespace Omniroute.Services;

/// <summary>
/// Фоновий сервіс моніторингу станцій через MQTT
/// </summary>
public static class MonitorService
{
    private static bool _isRunning = false;
    private static MqttLink? _mqttClient;
    private static readonly ConcurrentDictionary<string, DeviceParams> _deviceStates = new();
    private static readonly ConcurrentDictionary<string, IDeviceProtocol> _protocols = new();
    private static System.Threading.Timer? _updateTimer;

    /// <summary>
    /// Запустити моніторинг
    /// </summary>
    public static async Task StartAsync()
    {
        if (_isRunning) return;

        var credentials = App.Repository.Credentials;
        if (credentials == null || string.IsNullOrEmpty(credentials.MqttUrl))
        {
            System.Diagnostics.Debug.WriteLine("MonitorService: No credentials available");
            return;
        }

        _isRunning = true;

        try
        {
            // MQTT облікові дані
            var mqttCreds = new MqttCredentials(
                credentials.MqttUrl,
                credentials.MqttPort,
                credentials.MqttUsername ?? "",
                credentials.MqttPassword ?? ""
            );

            // Створити MQTT клієнт
            _mqttClient = new MqttLink(
                mqttCreds,
                credentials.UserId ?? "",
                OnMessage,
                OnConnected,
                OnFatal
            );

            // Підготувати топіки для підписки
            var topics = new List<string>();
            foreach (var device in App.Repository.Devices.Where(d => !d.IsDeleted))
            {
                topics.Add($"/app/device/property/{device.SerialNumber}");

                // Зберегти протокол
                _protocols[device.SerialNumber] = GetProtocolForDevice(device);
            }

            // Підключитися
            await _mqttClient.ConnectAsync(topics);

            // Запустити таймер оновлення UI
            _updateTimer = new System.Threading.Timer(
                _ => UpdateDevicesState(),
                null,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2)
            );

            System.Diagnostics.Debug.WriteLine("MonitorService: Started successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MonitorService: Failed to start - {ex.Message}");
            _isRunning = false;
        }
    }

    /// <summary>
    /// Зупинити моніторинг
    /// </summary>
    public static void Stop()
    {
        if (!_isRunning) return;

        _updateTimer?.Dispose();
        _updateTimer = null;

        _mqttClient?.Dispose();
        _mqttClient = null;

        _deviceStates.Clear();
        _protocols.Clear();

        _isRunning = false;

        System.Diagnostics.Debug.WriteLine("MonitorService: Stopped");
    }

    /// <summary>
    /// Обробка MQTT повідомлення
    /// </summary>
    private static void OnMessage(string topic, byte[] payload)
    {
        try
        {
            // Визначити серійний номер з топіку
            var parts = topic.Split('/');
            if (parts.Length < 4) return;

            var serialNumber = parts[^1];

            // Знайти протокол
            if (!_protocols.TryGetValue(serialNumber, out var protocol))
                return;

            // Розпарсити повідомлення
            var newParams = protocol.Parse(TopicKind.Data, payload);

            // Оновити або створити стан
            _deviceStates.AddOrUpdate(
                serialNumber,
                newParams,
                (_, existing) =>
                {
                    // Злити нові параметри з існуючими
                    foreach (var kvp in newParams)
                    {
                        existing[kvp.Key] = kvp.Value;
                    }
                    return existing;
                });

            System.Diagnostics.Debug.WriteLine($"MonitorService: Updated state for {serialNumber}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MonitorService: Message handling error - {ex.Message}");
        }
    }

    /// <summary>
    /// Обробка зміни статусу підключення
    /// </summary>
    private static void OnConnected(bool isConnected)
    {
        System.Diagnostics.Debug.WriteLine($"MonitorService: MQTT {(isConnected ? "connected" : "disconnected")}");
    }

    /// <summary>
    /// Обробка критичної помилки
    /// </summary>
    private static void OnFatal(string error)
    {
        System.Diagnostics.Debug.WriteLine($"MonitorService: Fatal error - {error}");
        Stop();
    }

    /// <summary>
    /// Оновити стан пристроїв в Repository
    /// </summary>
    private static void UpdateDevicesState()
    {
        foreach (var kvp in _deviceStates)
        {
            var serialNumber = kvp.Key;
            var parameters = kvp.Value;

            if (!_protocols.TryGetValue(serialNumber, out var protocol))
                continue;

            // Отримати нормалізований стан
            var state = protocol.GetState(parameters);

            // Оновити пристрій в Repository
            var device = App.Repository.GetDevice(serialNumber);
            if (device != null)
            {
                device.IsOnline = true;
                device.LastSeen = DateTime.Now;
                device.BatteryLevel = state.Soc ?? 0;
                device.BatteryWatts = state.InputW;
                device.InputWatts = state.InputW;
                device.OutputWatts = state.OutputW;
                device.Temperature = state.BatteryTempC;

                // Зберегти зміни
                App.Repository.SaveDevice(device);

                // Додати в історію
                var historyEntry = new HistoryEntry
                {
                    SerialNumber = serialNumber,
                    Timestamp = DateTime.Now,
                    BatteryLevel = state.Soc ?? 0,
                    BatteryWatts = state.InputW,
                    InputWatts = state.InputW,
                    OutputWatts = state.OutputW,
                    Temperature = state.BatteryTempC,
                    HasAcInput = state.GridConnected ?? false
                };

                _ = App.Repository.AddHistoryEntryAsync(historyEntry);
            }
        }
    }

    /// <summary>
    /// Отримати протокол для пристрою
    /// </summary>
    private static IDeviceProtocol GetProtocolForDevice(Device device)
    {
        return device.Model switch
        {
            DeviceModel.Delta2 => new Delta2Protocol(isMax: false),
            DeviceModel.Delta2Max => new Delta2Protocol(isMax: true),
            // TODO: Додати інші протоколи (Delta3, DeltaPro3)
            _ => new Delta2Protocol(isMax: false)
        };
    }

    /// <summary>
    /// Запустити моніторинг (синхронна версія для зворотної сумісності)
    /// </summary>
    public static void Start()
    {
        _ = StartAsync();
    }

    public static bool IsRunning => _isRunning;
}
