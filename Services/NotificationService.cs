using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace Omniroute.Services;

/// <summary>
/// Сервіс Windows сповіщень
/// </summary>
public static class NotificationService
{
    private static bool _isInitialized = false;

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
    /// Показати сповіщення про відключення живлення
    /// </summary>
    public static void ShowPowerLossNotification(string deviceName)
    {
        if (!App.Repository.Settings.NotificationsEnabled || !App.Repository.Settings.NotifyOnPowerLoss)
            return;

        var notification = new AppNotificationBuilder()
            .AddText($"⚡ {deviceName}")
            .AddText("Відключено від мережі")
            .SetScenario(AppNotificationScenario.Urgent)
            .BuildNotification();

        AppNotificationManager.Default.Show(notification);
    }

    /// <summary>
    /// Показати сповіщення про підключення живлення
    /// </summary>
    public static void ShowPowerRestoredNotification(string deviceName)
    {
        if (!App.Repository.Settings.NotificationsEnabled || !App.Repository.Settings.NotifyOnPowerLoss)
            return;

        var notification = new AppNotificationBuilder()
            .AddText($"✓ {deviceName}")
            .AddText("Підключено до мережі")
            .BuildNotification();

        AppNotificationManager.Default.Show(notification);
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

        var notification = new AppNotificationBuilder()
            .AddText($"🔋 {deviceName}")
            .AddText($"Низький заряд: {batteryLevel}%")
            .SetScenario(AppNotificationScenario.Urgent)
            .BuildNotification();

        AppNotificationManager.Default.Show(notification);
    }

    /// <summary>
    /// Показати сповіщення про повний заряд
    /// </summary>
    public static void ShowFullChargeNotification(string deviceName)
    {
        if (!App.Repository.Settings.NotificationsEnabled || !App.Repository.Settings.NotifyOnFullCharge)
            return;

        var notification = new AppNotificationBuilder()
            .AddText($"✓ {deviceName}")
            .AddText("Повністю заряджено")
            .BuildNotification();

        AppNotificationManager.Default.Show(notification);
    }

    /// <summary>
    /// Показати сповіщення про втрату зв'язку
    /// </summary>
    public static void ShowOfflineNotification(string deviceName)
    {
        if (!App.Repository.Settings.NotificationsEnabled || !App.Repository.Settings.NotifyOnOffline)
            return;

        var notification = new AppNotificationBuilder()
            .AddText($"⚠ {deviceName}")
            .AddText("Станція не на зв'язку")
            .SetScenario(AppNotificationScenario.Urgent)
            .BuildNotification();

        AppNotificationManager.Default.Show(notification);
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
            AppNotificationManager.Default.RemoveAllAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NotificationService: Failed to clear notifications - {ex.Message}");
        }
    }
}
