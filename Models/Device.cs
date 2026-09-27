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
    public bool IsOnline { get => _isOnline; set => SetProperty(ref _isOnline, value); }

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
    public bool? GridConnected { get => _gridConnected; set => SetProperty(ref _gridConnected, value); }

    // Готові рядки для інтерфейсу
    [JsonIgnore] public string BatteryText => _batteryLevel.HasValue ? $"{_batteryLevel}%" : "--";
    [JsonIgnore] public double BatteryPercent => _batteryLevel ?? 0;
    [JsonIgnore] public string InputText => FormatWatts(_inputWatts);
    [JsonIgnore] public string OutputText => FormatWatts(_outputWatts);
    [JsonIgnore] public string ModelName => Model.GetDisplayName();

    public static string FormatWatts(int? watts) => watts.HasValue ? $"{watts} Вт" : "-- Вт";

    #endregion
}

/// <summary>
/// Підтримувані моделі станцій
/// </summary>
public enum DeviceModel
{
    Unknown,
    Delta2,          // R331
    Delta2Max,       // R351
    DeltaMax,        // DA
    River2Max,       // R611
    Delta3,          // P231
    Delta3Plus,      // P231
    DeltaPro3        // MR51
}

/// <summary>
/// Розширення для моделей
/// </summary>
public static class DeviceModelExtensions
{
    /// <summary>
    /// Визначити модель за префіксом серійного номера, а якщо він невідомий — за назвою продукту
    /// (хмара не завжди повертає productName, тому серійний номер перевіряється першим)
    /// </summary>
    public static DeviceModel DetectFromSerial(string serialNumber, string? productName = null)
    {
        var sn = serialNumber ?? string.Empty;

        if (sn.StartsWith("R331", StringComparison.Ordinal)) return DeviceModel.Delta2;
        if (sn.StartsWith("R351", StringComparison.Ordinal)) return DeviceModel.Delta2Max;
        if (sn.StartsWith("R611", StringComparison.Ordinal)) return DeviceModel.River2Max;
        if (sn.StartsWith("P231", StringComparison.Ordinal)) return DeviceModel.Delta3;
        if (sn.StartsWith("MR51", StringComparison.Ordinal)) return DeviceModel.DeltaPro3;
        if (sn.StartsWith("DA", StringComparison.Ordinal)) return DeviceModel.DeltaMax;

        return productName?.Trim().ToUpperInvariant() switch
        {
            "DELTA 2" => DeviceModel.Delta2,
            "DELTA 2 MAX" => DeviceModel.Delta2Max,
            "DELTA 3" => DeviceModel.Delta3,
            "DELTA 3 PLUS" => DeviceModel.Delta3Plus,
            "DELTA PRO 3" => DeviceModel.DeltaPro3,
            "DELTA MAX" => DeviceModel.DeltaMax,
            "RIVER 2 MAX" => DeviceModel.River2Max,
            _ => DeviceModel.Unknown
        };
    }

    public static string GetDisplayName(this DeviceModel model)
    {
        return model switch
        {
            DeviceModel.Delta2 => "Delta 2",
            DeviceModel.Delta2Max => "Delta 2 Max",
            DeviceModel.DeltaMax => "Delta Max",
            DeviceModel.River2Max => "River 2 Max",
            DeviceModel.Delta3 => "Delta 3",
            DeviceModel.Delta3Plus => "Delta 3 Plus",
            DeviceModel.DeltaPro3 => "Delta Pro 3",
            _ => "Unknown"
        };
    }

    public static bool UsesProtobuf(this DeviceModel model)
    {
        return model is DeviceModel.Delta3 or DeviceModel.Delta3Plus or DeviceModel.DeltaPro3;
    }
}
