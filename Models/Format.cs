using System;
using System.Globalization;
using PowerHub.Protocol;

namespace PowerHub.Models;

/// <summary>
/// Форматування значень для інтерфейсу, трею і сповіщень (як watts/minutes/flowText в Android-версії)
/// </summary>
public static class Format
{
    private static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("uk-UA");

    public static string Watts(int? w) => w switch
    {
        null => "—",
        >= 1000 => (w.Value / 1000.0).ToString("0.00", Uk) + " кВт",
        _ => $"{w} Вт"
    };

    public static string Minutes(int? m)
    {
        if (m == null) return "—";
        var h = m.Value / 60;
        var mm = m.Value % 60;
        return h > 0 ? $"{h} год {mm} хв" : $"{mm} хв";
    }

    public static string WattHours(double wh) =>
        wh >= 1000 ? (wh / 1000).ToString("0.00", Uk) + " кВт·год" : wh.ToString("0", Uk) + " Вт·год";

    /// <summary>
    /// Що робить батарея — показується лише відповідна оцінка часу
    /// </summary>
    public static string FlowText(BatteryFlow flow) => flow switch
    {
        BatteryFlow.Charging c => c.Minutes.HasValue ? $"⏱ до повного заряду {Minutes(c.Minutes)}" : "⚡ заряджається",
        BatteryFlow.Discharging d => d.Minutes.HasValue ? $"⏱ вистачить на {Minutes(d.Minutes)}" : "🔋 розряджається",
        BatteryFlow.Full => "✓ повністю заряджено",
        BatteryFlow.Idle i => i.LoadW > 5 ? $"🔌 навантаження {i.LoadW} Вт · батарея в спокої" : "без навантаження",
        _ => "⏱ —"
    };

    /// <summary>
    /// Короткий стан мережі для картки станції
    /// </summary>
    public static string GridShort(GridStatus? grid, int? volt) => grid switch
    {
        GridStatus.Ok => "мережа ✓" + (volt.HasValue ? $" {volt} В" : ""),
        GridStatus.Weak => $"⚠ слабка мережа {volt} В",
        GridStatus.None => "без мережі",
        _ => "мережа —"
    };

    /// <summary>
    /// Стан мережі для сторінки станції
    /// </summary>
    public static string GridLong(GridStatus? grid, int? volt) => grid switch
    {
        GridStatus.Ok => "є" + (volt.HasValue ? $" ({volt} В)" : ""),
        GridStatus.Weak => $"слабка ({volt} В), заряд не йде",
        GridStatus.None => "немає",
        _ => "—"
    };
}
