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
    public bool NotifyOnFullCharge { get; set; } = true;
    public bool NotifyOnOffline { get; set; } = true;

    /// <summary>
    /// Нижче цієї напруги мережа вважається слабкою: станція не заряджається (В)
    /// </summary>
    public int WeakGridVolt { get; set; } = Protocol.DeviceStateLogic.DefaultWeakGridVolt;

    // Робота у фоні
    /// <summary>Закриття вікна ховає застосунок у трей, моніторинг і сповіщення працюють далі</summary>
    public bool CloseToTray { get; set; } = true;

    /// <summary>Запускати разом з Windows (згорнутим у трей)</summary>
    public bool StartWithWindows { get; set; }

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
