namespace Omniroute.Models;

/// <summary>
/// Модель зарядної станції EcoFlow
/// </summary>
public class Device
{
    public string SerialNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DeviceModel Model { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastSeen { get; set; }

    // Стан батареї
    public int BatteryLevel { get; set; } // 0-100%
    public int? BatteryWatts { get; set; } // Потужність заряду/розряду
    public int? TimeRemaining { get; set; } // Хвилини до повного заряду/розряду

    // Входи/виходи
    public int? InputWatts { get; set; } // Вхід (AC/сонце)
    public int? OutputWatts { get; set; } // Вихід (AC/DC/USB)

    // Температура
    public int? Temperature { get; set; }

    // Стан виходів
    public bool AcEnabled { get; set; }
    public bool DcEnabled { get; set; }
    public bool UsbEnabled { get; set; }

    // Налаштування
    public int DisplayOrder { get; set; }
    public bool IsDeleted { get; set; }
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
    public static DeviceModel DetectFromSerial(string serialNumber)
    {
        if (string.IsNullOrEmpty(serialNumber)) return DeviceModel.Unknown;

        return serialNumber[..4] switch
        {
            "R331" => DeviceModel.Delta2,
            "R351" => DeviceModel.Delta2Max,
            "R611" => DeviceModel.River2Max,
            "P231" => DeviceModel.Delta3,
            "MR51" => DeviceModel.DeltaPro3,
            _ when serialNumber.StartsWith("DA") => DeviceModel.DeltaMax,
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
