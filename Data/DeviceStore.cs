using System.Collections.Generic;
using System.Text.Json;
using Omniroute.Models;
using Windows.Storage;

namespace Omniroute.Data;

/// <summary>
/// Зберігання списку пристроїв (тільки ідентифікація й налаштування, без телеметрії)
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
    public List<Device> LoadDevices()
    {
        if (_settings.Values.TryGetValue(DevicesKey, out var value) && value is string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<Device>>(json) ?? new List<Device>();
            }
            catch
            {
                return new List<Device>();
            }
        }
        return new List<Device>();
    }

    /// <summary>
    /// Зберегти список пристроїв
    /// </summary>
    public void SaveDevices(IEnumerable<Device> devices)
    {
        _settings.Values[DevicesKey] = JsonSerializer.Serialize(devices);
    }
}
