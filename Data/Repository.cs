using System.Collections.ObjectModel;
using Omniroute.Models;

namespace Omniroute.Data;

/// <summary>
/// Центральний репозиторій даних застосунку
/// </summary>
public class Repository
{
    private readonly CredentialStore _credentialStore;
    private readonly SettingsStore _settingsStore;
    private readonly DeviceStore _deviceStore;
    private readonly HistoryDbContext _historyDb;

    public ObservableCollection<Device> Devices { get; private set; }
    public AppSettings Settings { get; private set; }
    public Credentials? Credentials { get; private set; }

    public bool IsLoggedIn => Credentials?.Token != null &&
                             Credentials.TokenExpiry > DateTime.Now;

    public Repository()
    {
        _credentialStore = new CredentialStore();
        _settingsStore = new SettingsStore();
        _deviceStore = new DeviceStore();
        _historyDb = new HistoryDbContext();

        // Завантажити дані
        Settings = _settingsStore.LoadSettings();
        Credentials = _credentialStore.LoadCredentials();
        Devices = _deviceStore.LoadDevices();

        // Ініціалізація бази даних
        _historyDb.Database.EnsureCreated();
    }

    #region Облікові дані

    /// <summary>
    /// Зберегти облікові дані після входу
    /// </summary>
    public void SaveCredentials(Credentials credentials)
    {
        Credentials = credentials;
        _credentialStore.SaveCredentials(credentials);
    }

    /// <summary>
    /// Вийти з акаунту
    /// </summary>
    public void Logout()
    {
        Credentials = null;
        _credentialStore.ClearCredentials();
        Devices.Clear();
        _deviceStore.SaveDevices(Devices);
    }

    #endregion

    #region Налаштування

    /// <summary>
    /// Оновити налаштування
    /// </summary>
    public void UpdateSettings(Action<AppSettings> updateAction)
    {
        updateAction(Settings);
        _settingsStore.SaveSettings(Settings);
    }

    #endregion

    #region Пристрої

    /// <summary>
    /// Додати або оновити пристрій
    /// </summary>
    public void SaveDevice(Device device)
    {
        _deviceStore.SaveDevice(device, Devices);
    }

    /// <summary>
    /// Видалити пристрій
    /// </summary>
    public void RemoveDevice(string serialNumber)
    {
        _deviceStore.RemoveDevice(serialNumber, Devices);
    }

    /// <summary>
    /// Отримати пристрій за серійним номером
    /// </summary>
    public Device? GetDevice(string serialNumber)
    {
        return Devices.FirstOrDefault(d => d.SerialNumber == serialNumber && !d.IsDeleted);
    }

    #endregion

    #region Історія

    /// <summary>
    /// Додати запис історії
    /// </summary>
    public async Task AddHistoryEntryAsync(HistoryEntry entry)
    {
        await _historyDb.AddEntryAsync(entry);
    }

    /// <summary>
    /// Отримати історію для пристрою
    /// </summary>
    public async Task<List<HistoryEntry>> GetHistoryAsync(string serialNumber, DateTime from, DateTime to)
    {
        return await _historyDb.GetHistoryAsync(serialNumber, from, to);
    }

    /// <summary>
    /// Очистити стару історію
    /// </summary>
    public async Task CleanupOldHistoryAsync()
    {
        await _historyDb.CleanupOldEntriesAsync();
    }

    #endregion
}
