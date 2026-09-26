namespace Omniroute.Services;

/// <summary>
/// Фоновий сервіс моніторингу станцій
/// </summary>
public static class MonitorService
{
    private static bool _isRunning = false;

    /// <summary>
    /// Запустити моніторинг
    /// </summary>
    public static void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        // TODO: Реалізувати фоновий моніторинг через MQTT
    }

    /// <summary>
    /// Зупинити моніторинг
    /// </summary>
    public static void Stop()
    {
        _isRunning = false;
        // TODO: Закрити MQTT з'єднання
    }
}
