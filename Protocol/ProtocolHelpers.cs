namespace Omniroute.Protocol;

/// <summary>
/// Розширення для роботи з параметрами пристрою
/// </summary>
public static class DeviceParamsExtensions
{
    /// <summary>
    /// Отримати числове значення
    /// </summary>
    public static double? GetNumber(this DeviceParams parameters, string key)
    {
        if (!parameters.TryGetValue(key, out var value) || value == null)
            return null;

        return value switch
        {
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            bool b => b ? 1.0 : 0.0,
            string s when double.TryParse(s, out var d) => d,
            _ => null
        };
    }

    /// <summary>
    /// Отримати ціле число
    /// </summary>
    public static int? GetInt(this DeviceParams parameters, string key)
    {
        var num = parameters.GetNumber(key);
        return num.HasValue ? (int)Math.Round(num.Value) : null;
    }

    /// <summary>
    /// Отримати абсолютне значення
    /// </summary>
    public static int? GetAbsInt(this DeviceParams parameters, string key)
    {
        var num = parameters.GetNumber(key);
        return num.HasValue ? (int)Math.Round(Math.Abs(num.Value)) : null;
    }

    /// <summary>
    /// Отримати булеве значення
    /// </summary>
    public static bool? GetFlag(this DeviceParams parameters, string key)
    {
        var num = parameters.GetNumber(key);
        return num.HasValue ? num.Value != 0.0 : null;
    }

    /// <summary>
    /// Сума значень кількох ключів
    /// </summary>
    public static int? SumOf(this DeviceParams parameters, params string[] keys)
    {
        var values = keys.Select(k => parameters.GetInt(k)).Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return values.Count > 0 ? values.Sum() : null;
    }

    /// <summary>
    /// Валідувати хвилини (фільтрує sentinel значення)
    /// </summary>
    public static int? ValidMinutes(int? minutes)
    {
        return minutes.HasValue && minutes.Value >= 1 && minutes.Value <= 5998 ? minutes : null;
    }
}

/// <summary>
/// Загальні опції для елементів керування
/// </summary>
public static class ControlOptions
{
    public static readonly List<(string, int)> ScreenTimeoutOptions = new()
    {
        ("Ніколи", 0),
        ("10 с", 10),
        ("30 с", 30),
        ("1 хв", 60),
        ("5 хв", 300),
        ("30 хв", 1800)
    };

    public static readonly List<(string, int)> StandbyOptions = new()
    {
        ("Ніколи", 0),
        ("30 хв", 30),
        ("1 год", 60),
        ("2 год", 120),
        ("4 год", 240),
        ("6 год", 360),
        ("12 год", 720),
        ("24 год", 1440)
    };
}
