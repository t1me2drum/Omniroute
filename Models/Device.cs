using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Omniroute.Models;

/// <summary>
/// Модель зарядної станції EcoFlow.
/// Зберігаються лише ідентифікація та налаштування; телеметрія живе тільки в пам'яті
/// і оновлюється з UI-потоку, тому на неї можна прив'язувати інтерфейс.
/// </summary>
public class Device : ObservableObject
{
    public string SerialNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DeviceModel Model { get; set; }

    /// <summary>
    /// Станція прийшла зі списку акаунта (синхронізація її оновлює і видаляє)
    /// </summary>
    public bool IsImported { get; set; }

    // Налаштування
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Видалена користувачем: синхронізація з акаунтом не повертає її назад
    /// </summary>
    public bool IsDeleted { get; set; }

    #region Телеметрія (не зберігається)

    private bool _isOnline;
    private DateTime _lastSeen;
    private int? _batteryLevel;
    private int? _batteryWatts;
    private int? _timeRemaining;
    private int? _inputWatts;
    private int? _outputWatts;
    private int? _solarWatts;
    private int? _temperature;
    private int? _cycles;
    private bool? _gridConnected;

    [JsonIgnore]
    public bool IsOnline
    {
        get => _isOnline;
        set
        {
            if (SetProperty(ref _isOnline, value))
            {
                OnPropertyChanged(nameof(IsOffline));
                OnPropertyChanged(nameof(OnlineText));
            }
        }
    }

    [JsonIgnore]
    public DateTime LastSeen { get => _lastSeen; set => SetProperty(ref _lastSeen, value); }

    /// <summary>Заряд батареї 0-100%</summary>
    [JsonIgnore]
    public int? BatteryLevel
    {
        get => _batteryLevel;
        set
        {
            if (SetProperty(ref _batteryLevel, value))
            {
                OnPropertyChanged(nameof(BatteryText));
                OnPropertyChanged(nameof(BatteryPercent));
            }
        }
    }

    /// <summary>Баланс батареї: вхід мінус вихід (додатне — заряджається)</summary>
    [JsonIgnore]
    public int? BatteryWatts { get => _batteryWatts; set => SetProperty(ref _batteryWatts, value); }

    /// <summary>Хвилини до повного заряду/розряду</summary>
    [JsonIgnore]
    public int? TimeRemaining { get => _timeRemaining; set => SetProperty(ref _timeRemaining, value); }

    /// <summary>Вхід (AC/сонце)</summary>
    [JsonIgnore]
    public int? InputWatts
    {
        get => _inputWatts;
        set { if (SetProperty(ref _inputWatts, value)) OnPropertyChanged(nameof(InputText)); }
    }

    /// <summary>Вихід (AC/DC/USB)</summary>
    [JsonIgnore]
    public int? OutputWatts
    {
        get => _outputWatts;
        set { if (SetProperty(ref _outputWatts, value)) OnPropertyChanged(nameof(OutputText)); }
    }

    [JsonIgnore]
    public int? SolarWatts { get => _solarWatts; set => SetProperty(ref _solarWatts, value); }

    [JsonIgnore]
    public int? Temperature { get => _temperature; set => SetProperty(ref _temperature, value); }

    [JsonIgnore]
    public int? Cycles { get => _cycles; set => SetProperty(ref _cycles, value); }

    [JsonIgnore]
    public bool? GridConnected
    {
        get => _gridConnected;
        set { if (SetProperty(ref _gridConnected, value)) OnPropertyChanged(nameof(GridText)); }
    }

    // Готові рядки для інтерфейсу
    [JsonIgnore] public string BatteryText => _batteryLevel.HasValue ? $"{_batteryLevel}%" : "--";
    [JsonIgnore] public double BatteryPercent => _batteryLevel ?? 0;
    [JsonIgnore] public string InputText => FormatWatts(_inputWatts);
    [JsonIgnore] public string OutputText => FormatWatts(_outputWatts);
    [JsonIgnore] public string ModelName => Model.GetDisplayName();
    [JsonIgnore] public bool IsOffline => !_isOnline;
    [JsonIgnore] public string OnlineText => _isOnline ? "Онлайн" : "Офлайн";
    [JsonIgnore] public string GridText => _gridConnected switch
    {
        true => "⚡ Від мережі",
        false => "🔋 Від батареї",
        null => string.Empty
    };

    public static string FormatWatts(int? watts) => watts.HasValue ? $"{watts} Вт" : "-- Вт";

    #endregion
}
