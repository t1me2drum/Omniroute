using System.Text.Json;
using Omniroute.Models;
using Windows.Storage;

namespace Omniroute.Data;

/// <summary>
/// Зберігання налаштувань застосунку
/// </summary>
public class SettingsStore
{
    private const string SettingsKey = "app_settings";
    private readonly ApplicationDataContainer _settings;

    public SettingsStore()
    {
        _settings = ApplicationData.Current.LocalSettings;
    }

    /// <summary>
    /// Завантажити налаштування
    /// </summary>
    public AppSettings LoadSettings()
    {
        if (_settings.Values.TryGetValue(SettingsKey, out var value) && value is string json)
        {
            try
            {
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }
        return new AppSettings();
    }

    /// <summary>
    /// Зберегти налаштування
    /// </summary>
    public void SaveSettings(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        _settings.Values[SettingsKey] = json;
    }
}
