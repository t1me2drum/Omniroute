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
    public int? Temperature { get; set; }

    // Стан мережі
    public bool HasAcInput { get; set; }
}

/// <summary>
/// Агреговані дані історії
/// </summary>
public class HistoryStats
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public double AverageInput { get; set; }
    public double AverageOutput { get; set; }
    public double TotalEnergyIn { get; set; }  // Wh
    public double TotalEnergyOut { get; set; } // Wh

    public TimeSpan TimeWithoutPower { get; set; }
    public int PowerOutageCount { get; set; }
}
