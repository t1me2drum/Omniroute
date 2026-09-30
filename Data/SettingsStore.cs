using System.Text.Json;
using PowerHub.Models;

namespace PowerHub.Data;

/// <summary>
/// Зберігання налаштувань застосунку
/// </summary>
public class SettingsStore
{
    private const string SettingsKey = "app_settings";
    private readonly LocalStore _store = LocalStore.Default;

    /// <summary>
    /// Завантажити налаштування
    /// </summary>
    public AppSettings LoadSettings()
    {
        if (_store.Get(SettingsKey) is string json)
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
        _store.Set(SettingsKey, json);
    }
}
