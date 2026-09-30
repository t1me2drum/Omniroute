using System;

namespace Omniroute.Models;

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
    DeltaPro3,       // MR51
    Delta3Max        // P231 / назва продукту "Delta 3 Max" (додано в кінець, бо enum зберігається числом)
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
            "DELTA 3 MAX" or "DELTA 3 MAX PLUS" => DeviceModel.Delta3Max,
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
            DeviceModel.Delta3Max => "Delta 3 Max",
            _ => "Unknown"
        };
    }

    public static bool UsesProtobuf(this DeviceModel model)
    {
        return model is DeviceModel.Delta3 or DeviceModel.Delta3Plus or DeviceModel.Delta3Max or DeviceModel.DeltaPro3;
    }
}
