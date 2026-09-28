using System.Collections.Generic;
using System.Text.Json;
using Omniroute.Models;

namespace Omniroute.Data;

/// <summary>
/// Зберігання списку пристроїв (тільки ідентифікація й налаштування, без телеметрії)
/// </summary>
public class DeviceStore
{
    private const string DevicesKey = "devices";
    private readonly LocalStore _store = LocalStore.Default;

    /// <summary>
    /// Завантажити список пристроїв
    /// </summary>
    public List<Device> LoadDevices()
    {
        if (_store.Get(DevicesKey) is string json)
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
        _store.Set(DevicesKey, JsonSerializer.Serialize(devices));
    }
}
