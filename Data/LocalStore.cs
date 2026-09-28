using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Omniroute.Data;

/// <summary>
/// Просте сховище ключ-значення у файлі %LocalAppData%\Omniroute\settings.json.
/// Замінює ApplicationData.LocalSettings, яке недоступне без MSIX-пакета.
/// </summary>
public sealed class LocalStore
{
    public static readonly string AppFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Omniroute");

    private static readonly Lazy<LocalStore> _default = new(() => new LocalStore(Path.Combine(AppFolder, "settings.json")));

    public static LocalStore Default => _default.Value;

    private readonly object _lock = new();
    private readonly string _path;
    private readonly Dictionary<string, string> _values;

    private LocalStore(string path)
    {
        _path = path;
        _values = Load(path);
    }

    public string? Get(string key)
    {
        lock (_lock)
            return _values.TryGetValue(key, out var value) ? value : null;
    }

    public void Set(string key, string value)
    {
        lock (_lock)
        {
            _values[key] = value;
            Save();
        }
    }

    public void Remove(string key)
    {
        lock (_lock)
        {
            if (_values.Remove(key))
                Save();
        }
    }

    private static Dictionary<string, string> Load(string path)
    {
        try
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? new();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LocalStore: failed to load {path} - {ex.Message}");
        }
        return new Dictionary<string, string>();
    }

    /// <summary>
    /// Запис через тимчасовий файл, щоб збій посеред запису не зіпсував налаштування
    /// </summary>
    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(_values));
        File.Move(temp, _path, overwrite: true);
    }
}
