using PowerHub.Models;

namespace PowerHub.Protocol;

/// <summary>
/// Протокол для кожної підтримуваної моделі (як DeviceModel.protocol в Android-версії)
/// </summary>
public static class Protocols
{
    public static IDeviceProtocol For(DeviceModel model) => model switch
    {
        DeviceModel.Delta2 => Delta2Protocol.Instance,
        DeviceModel.Delta2Max => Delta2MaxProtocol.Instance,
        DeviceModel.DeltaMax => DeltaMaxProtocol.Instance,
        DeviceModel.River2Max => River2MaxProtocol.Instance,
        DeviceModel.Delta3 or DeviceModel.Delta3Plus => Delta3Protocol.Standard,
        DeviceModel.Delta3Max => Delta3Protocol.Max,
        DeviceModel.DeltaPro3 => DeltaPro3Protocol.Instance,
        // Невідома модель: пробуємо найпоширеніший JSON-формат
        _ => Delta2Protocol.Instance
    };
}
