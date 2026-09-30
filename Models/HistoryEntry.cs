using System;

namespace Omniroute.Models;

/// <summary>
/// Запис історії заряду станції
/// </summary>
public class HistoryEntry
{
    public int Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    // Дані телеметрії
    public int BatteryLevel { get; set; }
    public int? BatteryWatts { get; set; }
    public int? InputWatts { get; set; }
    public int? OutputWatts { get; set; }
    public int? SolarWatts { get; set; }
    public int? AcInWatts { get; set; }
    public int? Temperature { get; set; }

    // Стан мережі. HasAcInput лишився від першої схеми; Grid = null, коли стан мережі невідомий
    public bool HasAcInput { get; set; }
    public bool? Grid { get; set; }
}
