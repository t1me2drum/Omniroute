namespace Omniroute.Models;

/// <summary>
/// Налаштування застосунку
/// </summary>
public class AppSettings
{
    // Тема
    public ThemeMode Theme { get; set; } = ThemeMode.System;

    // Застаріле: ключі Developer API раніше зберігалися тут відкритим текстом.
    // Під час запуску вони переносяться в зашифрований CredentialStore і очищаються.
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }

    // Сповіщення
    public bool NotificationsEnabled { get; set; } = true;
    public bool NotifyOnPowerLoss { get; set; } = true;
    public bool NotifyOnLowBattery { get; set; } = true;
    public int LowBatteryThreshold { get; set; } = 20;
    public bool NotifyOnFullCharge { get; set; } = false;
    public bool NotifyOnOffline { get; set; } = true;

    // Сортування пристроїв
    public DeviceSortMode SortMode { get; set; } = DeviceSortMode.Custom;
}

/// <summary>
/// Режим теми
/// </summary>
public enum ThemeMode
{
    Light,
    Dark,
    System
}

/// <summary>
/// Режим сортування пристроїв
/// </summary>
public enum DeviceSortMode
{
    Custom,        // Власний порядок (перетягування)
    OnlineFirst,   // Спершу онлайн
    Name,          // За назвою
    BatteryLevel   // За рівнем заряду
}
