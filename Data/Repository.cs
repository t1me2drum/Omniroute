using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Omniroute.Api;
using Omniroute.Models;

namespace Omniroute.Data;

/// <summary>
/// Результат синхронізації списку станцій з акаунтом
/// </summary>
public record SyncResult(int Added, int Updated, int Removed, IReadOnlyList<string> Unsupported);

/// <summary>
/// Центральний репозиторій даних застосунку. Потокобезпечний: MonitorService звертається з фонових потоків.
/// </summary>
public class Repository
{
    private readonly object _lock = new();
    private readonly CredentialStore _credentialStore;
    private readonly SettingsStore _settingsStore;
    private readonly DeviceStore _deviceStore;
    private readonly List<Device> _devices;
    private Credentials? _credentials;

    /// <summary>
    /// Список станцій змінився (додано, видалено, перейменовано). Може викликатися з будь-якого потоку.
    /// </summary>
    public event Action? DevicesChanged;

    public AppSettings Settings { get; }

    public Credentials? Credentials
    {
        get { lock (_lock) return _credentials; }
    }

    /// <summary>
    /// Токен при кожному підключенні отримується заново, тож достатньо збережених email і пароля
    /// </summary>
    public bool IsLoggedIn => Credentials != null;

    /// <summary>
    /// Усі станції, включно з видаленими (копія списку)
    /// </summary>
    public IReadOnlyList<Device> Devices
    {
        get { lock (_lock) return _devices.ToList(); }
    }

    /// <summary>
    /// Станції, які не видалені користувачем, у порядку відображення
    /// </summary>
    public IReadOnlyList<Device> ActiveDevices
    {
        get
        {
            lock (_lock)
                return _devices.Where(d => !d.IsDeleted).OrderBy(d => d.DisplayOrder).ToList();
        }
    }

    public Repository()
    {
        _credentialStore = new CredentialStore();
        _settingsStore = new SettingsStore();
        _deviceStore = new DeviceStore();

        // Завантажити дані
        Settings = _settingsStore.LoadSettings();
        _credentials = _credentialStore.LoadCredentials();
        _devices = _deviceStore.LoadDevices();

        MigrateDeveloperKeys();

        // Ініціалізація бази даних
        HistoryDbContext.EnsureCreated();
    }

    /// <summary>
    /// Перенести ключі Developer API з відкритих налаштувань у зашифроване сховище
    /// </summary>
    private void MigrateDeveloperKeys()
    {
        if (string.IsNullOrEmpty(Settings.SecretKey) && string.IsNullOrEmpty(Settings.AccessKey))
            return;

        if (_credentials != null && !_credentials.HasDeveloperKeys &&
            !string.IsNullOrEmpty(Settings.AccessKey) && !string.IsNullOrEmpty(Settings.SecretKey))
        {
            _credentials.AccessKey = Settings.AccessKey;
            _credentials.SecretKey = Settings.SecretKey;
            _credentialStore.SaveCredentials(_credentials);
        }

        Settings.AccessKey = null;
        Settings.SecretKey = null;
        _settingsStore.SaveSettings(Settings);
    }

    #region Облікові дані

    /// <summary>
    /// Зберегти облікові дані після входу
    /// </summary>
    public void SaveCredentials(Credentials credentials)
    {
        lock (_lock)
        {
            _credentials = credentials;
            _credentialStore.SaveCredentials(credentials);
        }
    }

    /// <summary>
    /// Перевірити ключі Developer API синхронізацією і лише тоді зберегти їх
    /// </summary>
    public async Task<SyncResult> SaveDeveloperKeysAsync(string accessKey, string secretKey)
    {
        var result = await SyncStationsAsync(accessKey, secretKey);

        lock (_lock)
        {
            if (_credentials != null)
            {
                _credentials.AccessKey = accessKey;
                _credentials.SecretKey = secretKey;
                _credentialStore.SaveCredentials(_credentials);
            }
        }

        return result;
    }

    /// <summary>
    /// Вийти з акаунту
    /// </summary>
    public void Logout()
    {
        lock (_lock)
        {
            _credentials = null;
            _credentialStore.ClearCredentials();
            _devices.Clear();
            _deviceStore.SaveDevices(_devices);
        }
        DevicesChanged?.Invoke();
    }

    #endregion

    #region Налаштування

    /// <summary>
    /// Оновити налаштування
    /// </summary>
    public void UpdateSettings(Action<AppSettings> updateAction)
    {
        lock (_lock)
        {
            updateAction(Settings);
            _settingsStore.SaveSettings(Settings);
        }
    }

    #endregion

    #region Пристрої

    /// <summary>
    /// Отримати пристрій за серійним номером
    /// </summary>
    public Device? GetDevice(string serialNumber)
    {
        lock (_lock)
            return _devices.FirstOrDefault(d => d.SerialNumber == serialNumber && !d.IsDeleted);
    }

    /// <summary>
    /// Додати станцію вручну (або повернути раніше видалену)
    /// </summary>
    public Device AddDevice(string serialNumber, string? name)
    {
        var sn = serialNumber.Trim().ToUpperInvariant();
        Device device;

        lock (_lock)
        {
            device = _devices.FirstOrDefault(d => d.SerialNumber == sn) ?? new Device { SerialNumber = sn };
            if (!_devices.Contains(device))
            {
                device.DisplayOrder = _devices.Count == 0 ? 0 : _devices.Max(d => d.DisplayOrder) + 1;
                _devices.Add(device);
            }

            device.IsDeleted = false;
            device.Model = DeviceModelExtensions.DetectFromSerial(sn);
            device.Name = string.IsNullOrWhiteSpace(name) ? (device.Name is { Length: > 0 } n ? n : sn) : name.Trim();
            _deviceStore.SaveDevices(_devices);
        }

        DevicesChanged?.Invoke();
        return device;
    }

    /// <summary>
    /// Видалити пристрій. Станція лишається в списку з позначкою, щоб синхронізація не повертала її.
    /// </summary>
    public void RemoveDevice(string serialNumber)
    {
        lock (_lock)
        {
            var device = _devices.FirstOrDefault(d => d.SerialNumber == serialNumber);
            if (device == null)
                return;

            device.IsDeleted = true;
            _deviceStore.SaveDevices(_devices);
        }
        DevicesChanged?.Invoke();
    }

    /// <summary>
    /// Синхронізувати список станцій з акаунтом через Developer API.
    /// Вручну додані станції не змінюються, видалені користувачем не повертаються.
    /// </summary>
    public async Task<SyncResult> SyncStationsAsync(string? accessKey = null, string? secretKey = null)
    {
        var credentials = Credentials;
        accessKey ??= credentials?.AccessKey;
        secretKey ??= credentials?.SecretKey;

        if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
            throw new EcoflowException("Ключі Developer API не задано");

        List<CloudDevice> cloudDevices;
        using (var openApi = new EcoflowOpenApi())
        {
            cloudDevices = await openApi.ListDevicesAsync(accessKey, secretKey, credentials?.ApiHost ?? "api.ecoflow.com");
        }

        var unsupported = new List<string>();
        var supported = new Dictionary<string, (CloudDevice Cloud, DeviceModel Model)>();
        foreach (var c in cloudDevices)
        {
            var model = DeviceModelExtensions.DetectFromSerial(c.Sn, c.ProductName);
            if (model == DeviceModel.Unknown)
                unsupported.Add($"{c.Name} ({c.ProductName ?? c.Sn})");
            else
                supported[c.Sn] = (c, model);
        }

        int added = 0, updated = 0, removed = 0;
        lock (_lock)
        {
            // Імпортовані станції, яких більше немає в акаунті, прибираємо
            removed = _devices.RemoveAll(d => d.IsImported && !supported.ContainsKey(d.SerialNumber));

            foreach (var (sn, (cloud, model)) in supported)
            {
                var existing = _devices.FirstOrDefault(d => d.SerialNumber == sn);
                if (existing == null)
                {
                    _devices.Add(new Device
                    {
                        SerialNumber = sn,
                        Name = cloud.Name,
                        Model = model,
                        IsImported = true,
                        DisplayOrder = _devices.Count == 0 ? 0 : _devices.Max(d => d.DisplayOrder) + 1
                    });
                    added++;
                }
                else if (!existing.IsDeleted &&
                         (existing.Name != cloud.Name || existing.Model != model || !existing.IsImported))
                {
                    existing.Name = cloud.Name;
                    existing.Model = model;
                    existing.IsImported = true;
                    updated++;
                }
            }

            _deviceStore.SaveDevices(_devices);
        }

        if (added + updated + removed > 0)
            DevicesChanged?.Invoke();

        return new SyncResult(added, updated, removed, unsupported);
    }

    #endregion

    #region Історія

    /// <summary>
    /// Додати запис історії
    /// </summary>
    public Task AddHistoryEntryAsync(HistoryEntry entry) => HistoryDbContext.AddEntryAsync(entry);

    /// <summary>
    /// Отримати історію для пристрою
    /// </summary>
    public Task<List<HistoryEntry>> GetHistoryAsync(string serialNumber, DateTime from, DateTime to) =>
        HistoryDbContext.GetHistoryAsync(serialNumber, from, to);

    /// <summary>
    /// Очистити історію, старшу за 30 днів
    /// </summary>
    public Task<int> CleanupOldHistoryAsync() => HistoryDbContext.CleanupOldEntriesAsync();

    #endregion
}
