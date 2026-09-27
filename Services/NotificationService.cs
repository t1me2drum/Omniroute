using System;
using System.Collections.Generic;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Omniroute.Models;
using Omniroute.Protocol;

namespace Omniroute.Services;

/// <summary>
/// Сервіс Windows сповіщень
/// </summary>
public static class NotificationService
{
    private static bool _isInitialized = false;

    /// <summary>
    /// Пам'ять правил для кожної станції: правило спрацьовує один раз, коли умова стає істинною,
    /// і знову «озброюється» лише після того, як умова зникне (для заряду — з гістерезисом)
    /// </summary>
    private sealed class AlertMemory
    {
        public bool LowFired;
        public bool FullFired;
        public bool? Grid;
        public bool? Online;
    }

    private static readonly Dictionary<string, AlertMemory> _memory = new();

    /// <summary>
    /// Ініціалізувати сервіс сповіщень
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized) return;

        try
        {
            // Зареєструвати обробник сповіщень
            AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
            AppNotificationManager.Default.Register();

            _isInitialized = true;
            System.Diagnostics.Debug.WriteLine("NotificationService: Initialized");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NotificationService: Initialization failed - {ex.Message}");
        }
    }

    /// <summary>
    /// Перевірити правила сповіщень для нового стану станції.
    /// Викликається з UI-потоку MonitorService.
    /// </summary>
    public static void Evaluate(Device device, DeviceState state, bool online)
    {
        if (!_memory.TryGetValue(device.SerialNumber, out var m))
        {
            m = new AlertMemory();
            _memory[device.SerialNumber] = m;
        }

        var settings = App.Repository.Settings;
        var soc = state.Soc;

        if (online && soc.HasValue)
        {
            if (soc <= settings.LowBatteryThreshold && !m.LowFired)
            {
                m.LowFired = true;
                ShowLowBatteryNotification(device.Name, soc.Value);
            }
            else if (soc >= settings.LowBatteryThreshold + 3)
            {
                m.LowFired = false;
            }

            if (soc >= 100 && !m.FullFired)
            {
                m.FullFired = true;
                ShowFullChargeNotification(device.Name);
            }
            else if (soc <= 95)
            {
                m.FullFired = false;
            }
        }

        if (online && state.GridConnected is bool grid)
        {
            var prev = m.Grid;
            m.Grid = grid;
            if (prev.HasValue && prev != grid)
            {
                if (grid)
                    ShowPowerRestoredNotification(device.Name);
                else
                    ShowPowerLossNotification(device.Name, soc);
            }
        }

        var prevOnline = m.Online;
        m.Online = online;
        if (prevOnline == true && !online)
        {
            ShowOfflineNotification(device.Name);
        }
    }

    /// <summary>
    /// Забути стан правил (після виходу з акаунту)
    /// </summary>
    public static void Reset() => _memory.Clear();

    /// <summary>
    /// Показати сповіщення про відключення живлення
    /// </summary>
    public static void ShowPowerLossNotification(string deviceName, int? batteryLevel = null)
    {
        if (!App.Repository.Settings.NotificationsEnabled || !App.Repository.Settings.NotifyOnPowerLoss)
            return;

        var details = batteryLevel.HasValue
            ? $"Живлення зникло, працює від батареї ({batteryLevel}%)"
            : "Живлення зникло, працює від батареї";

        Show(new AppNotificationBuilder()
            .AddText($"⚡ {deviceName}")
            .AddText(details)
            .SetScenario(AppNotificationScenario.Urgent));
    }

    /// <summary>
    /// Показати сповіщення про підключення живлення
    /// </summary>
    public static void ShowPowerRestoredNotification(string deviceName)
    {
        if (!App.Repository.Settings.NotificationsEnabled || !App.Repository.Settings.NotifyOnPowerLoss)
            return;

        Show(new AppNotificationBuilder()
            .AddText($"✓ {deviceName}")
            .AddText("Живлення з'явилося, заряджається від мережі"));
    }

    /// <summary>
    /// Показати сповіщення про низький заряд
    /// </summary>
    public static void ShowLowBatteryNotification(string deviceName, int batteryLevel)
    {
        if (!App.Repository.Settings.NotificationsEnabled || !App.Repository.Settings.NotifyOnLowBattery)
            return;

        if (batteryLevel > App.Repository.Settings.LowBatteryThreshold)
            return;

        Show(new AppNotificationBuilder()
            .AddText($"🔋 {deviceName}")
            .AddText($"Низький заряд: {batteryLevel}%")
            .SetScenario(AppNotificationScenario.Urgent));
    }

    /// <summary>
    /// Показати сповіщення про повний заряд
    /// </summary>
    public static void ShowFullChargeNotification(string deviceName)
    {
        if (!App.Repository.Settings.NotificationsEnabled || !App.Repository.Settings.NotifyOnFullCharge)
            return;

        Show(new AppNotificationBuilder()
            .AddText($"✓ {deviceName}")
            .AddText("Повністю заряджено"));
    }

    /// <summary>
    /// Показати сповіщення про втрату зв'язку
    /// </summary>
    public static void ShowOfflineNotification(string deviceName)
    {
        if (!App.Repository.Settings.NotificationsEnabled || !App.Repository.Settings.NotifyOnOffline)
            return;

        Show(new AppNotificationBuilder()
            .AddText($"⚠ {deviceName}")
            .AddText("Станція не надсилає дані понад 3 хв")
            .SetScenario(AppNotificationScenario.Urgent));
    }

    private static void Show(AppNotificationBuilder builder)
    {
        if (!_isInitialized)
            return;

        try
        {
            AppNotificationManager.Default.Show(builder.BuildNotification());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NotificationService: Failed to show notification - {ex.Message}");
        }
    }

    /// <summary>
    /// Обробник натискання на сповіщення
    /// </summary>
    private static void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        // TODO: Обробити натискання на сповіщення (відкрити відповідний пристрій)
        System.Diagnostics.Debug.WriteLine("Notification clicked");
    }

    /// <summary>
    /// Очистити всі сповіщення
    /// </summary>
    public static void ClearAll()
    {
        try
        {
            _ = AppNotificationManager.Default.RemoveAllAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NotificationService: Failed to clear notifications - {ex.Message}");
        }
    }
}
