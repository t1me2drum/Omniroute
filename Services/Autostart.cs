using System;
using Microsoft.Win32;

namespace PowerHub.Services;

/// <summary>
/// Запуск разом з Windows (як BootReceiver в Android-версії): запис у HKCU\...\Run,
/// застосунок стартує з ключем --background, тобто одразу в треї
/// </summary>
public static class Autostart
{
    public const string BackgroundArg = "--background";

    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "PowerHub";

    /// <summary>Запис автозапуску версій, що звалися Omniroute</summary>
    private const string LegacyValueName = "Omniroute";

    public static void Apply(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKey);
            key.DeleteValue(LegacyValueName, throwOnMissingValue: false);
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
    /// Застосунок перенесли в іншу папку або перейменували — оновити запис автозапуску
    /// і прибрати старий запис Omniroute
    /// </summary>
    public static void Refresh(bool enabled) => Apply(enabled);
}
