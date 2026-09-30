using System;
using Microsoft.Win32;

namespace Omniroute.Services;

/// <summary>
/// Запуск разом з Windows (як BootReceiver в Android-версії): запис у HKCU\...\Run,
/// застосунок стартує з ключем --background, тобто одразу в треї
/// </summary>
public static class Autostart
{
    public const string BackgroundArg = "--background";

    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Omniroute";

    public static void Apply(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKey);
            if (enabled && Environment.ProcessPath is string exe)
                key.SetValue(ValueName, $"\"{exe}\" {BackgroundArg}");
            else
                key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch (Exception ex)
        {
            DiagLog.Log("autostart", "registry update failed", ex);
        }
    }

    /// <summary>
    /// Застосунок перенесли в іншу папку — оновити шлях у записі автозапуску
    /// </summary>
    public static void Refresh(bool enabled)
    {
        if (enabled)
            Apply(true);
    }
}
