using System.Collections.ObjectModel;
using System.Text.Json;
using Omniroute.Models;
using Windows.Storage;

namespace Omniroute.Data;

/// <summary>
/// Зберігання списку пристроїв
/// </summary>
public class DeviceStore
{
    private const string DevicesKey = "devices";
    private readonly ApplicationDataContainer _settings;

    public DeviceStore()
    {
        _settings = ApplicationData.Current.LocalSettings;
    }

    /// <summary>
    /// Завантажити список пристроїв
    /// </summary>
    public ObservableCollection<Device> LoadDevices()
    {
        if (_settings.Values.TryGetValue(DevicesKey, out var value) && value is string json)
        {
            try
            {
                var devices = JsonSerializer.Deserialize<List<Device>>(json) ?? new List<Device>();
                return new ObservableCollection<Device>(devices);
            }
            catch
            {
                return new ObservableCollection<Device>();
            }
        }
        return new ObservableCollection<Device>();
    }

    /// <summary>
    /// Зберегти список пристроїв
    /// </summary>
    public void SaveDevices(IEnumerable<Device> devices)
    {
        var json = JsonSerializer.Serialize(devices);
        _settings.Values[DevicesKey] = json;
    }

    /// <summary>
    /// Додати або оновити пристрій
    /// </summary>
    public void SaveDevice(Device device, ObservableCollection<Device> devices)
    {
        var existing = devices.FirstOrDefault(d => d.SerialNumber == device.SerialNumber);
        if (existing != null)
        {
            var index = devices.IndexOf(existing);
            devices[index] = device;
        }
        else
        {
            devices.Add(device);
        }
        SaveDevices(devices);
    }

    /// <summary>
    /// Видалити пристрій
    /// </summary>
    public void RemoveDevice(string serialNumber, ObservableCollection<Device> devices)
    {
        var device = devices.FirstOrDefault(d => d.SerialNumber == serialNumber);
        if (device != null)
        {
            device.IsDeleted = true;
            SaveDevices(devices);
        }
    }
}
