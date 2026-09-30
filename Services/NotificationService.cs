using System;
using System.Collections.Generic;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Omniroute.Models;
using Omniroute.Protocol;

namespace Omniroute.Services;

/// <summary>
/// Сповіщення Windows за правилами AlertEngine з Android-версії: кожне правило спрацьовує один раз,
/// коли умова стає істинною, і знову «озброюється» лише після того, як умова зникне
/// (для заряду й напруги — з гістерезисом)
/// </summary>
public static class NotificationService
{
    private static bool _isInitialized;

    private sealed class AlertMemory
    {
        public bool LowFired;
        public bool FullFired;
        public GridStatus? Grid;
        public bool? Online;
    }

    private static readonly Dictionary<string, AlertMemory> _memory = new();

    /// <summary>
    /// Натиснуто сповіщення про станцію (серійний номер). Викликається з фонового потоку.
    /// </summary>
    public static event Action<string?>? Invoked;

    public static void Initialize()
    {
        if (_isInitialized) return;

        try
        {
            AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
            AppNotificationManager.Default.Register();
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            DiagLog.Log("notify", "initialization failed", ex);
        }
    }

    /// <summary>
    /// Зняти реєстрацію під час виходу, інакше Windows вважає застосунок активним для сповіщень
    /// </summary>
    public static void Shutdown()
    {
        if (!_isInitialized) return;
        try
        {
            AppNotificationManager.Default.Unregister();
        }
        catch
        {
            // Застосунок і так завершується
        }
        _isInitialized = false;
    }

    /// <summary>
    /// Перевірити правила сповіщень для нового стану станції. Викликається з UI-потоку.
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
                if (settings.NotifyOnLowBattery)
                    Show(device, $"Низький заряд: {soc}%", $"{device.Name} скоро розрядиться", urgent: true);
            }
            else if (soc >= settings.LowBatteryThreshold + 3)
            {
                m.LowFired = false;
            }

            if (soc >= 100 && !m.FullFired)
            {
                m.FullFired = true;
                if (settings.NotifyOnFullCharge)
                    Show(device, "Повністю заряджено", $"{device.Name}: 100%");
            }
            else if (soc <= 95)
            {
                m.FullFired = false;
            }

            // Гістерезис 5 В, щоб напруга біля порогу не перемикала «слабка ↔ норма» туди-сюди
            var threshold = settings.WeakGridVolt + (m.Grid == GridStatus.Weak ? 5 : 0);
            var grid = state.GetGridStatus(threshold);
            if (grid.HasValue)
            {
                var prev = m.Grid;
                m.Grid = grid;
                if (prev.HasValue && prev != grid && settings.NotifyOnPowerLoss)
                {
                    var volt = state.AcInVolt.HasValue ? $"{state.AcInVolt} В" : "—";
                    switch (grid.Value)
                    {
                        case GridStatus.None:
                            Show(device, "Живлення зникло", $"{device.Name} працює від батареї, заряд {soc}%", urgent: true);
                            break;
                        case GridStatus.Weak:
                            Show(device, $"Слабка мережа: {volt}", $"{device.Name} не заряджається від мережі, заряд {soc}%", urgent: true);
                            break;
                        case GridStatus.Ok when prev == GridStatus.Weak:
                            Show(device, $"Напруга відновилася: {volt}", $"{device.Name}: мережа в нормі, заряд {soc}%");
                            break;
                        case GridStatus.Ok:
                            Show(device, "Живлення з'явилося", $"{device.Name}: мережа {volt}, заряд {soc}%");
                            break;
                    }
                }
            }
        }

        var prevOnline = m.Online;
        m.Online = online;
        if (prevOnline == true && !online && settings.NotifyOnOffline)
        {
            Show(device, "Станція не на зв'язку", $"{device.Name} не надсилає дані понад 3 хв", urgent: true);
        }
    }

    /// <summary>
    /// Забути стан правил (після виходу з акаунту)
    /// </summary>
    public static void Reset() => _memory.Clear();

    private static void Show(Device device, string title, string text, bool urgent = false)
    {
        if (!_isInitialized || !App.Repository.Settings.NotificationsEnabled)
            return;

        try
        {
            var builder = new AppNotificationBuilder()
                .AddArgument("sn", device.SerialNumber)
                .AddText(title)
                .AddText(text);
            if (urgent)
                builder.SetScenario(AppNotificationScenario.Urgent);
            AppNotificationManager.Default.Show(builder.BuildNotification());
        }
        catch (Exception ex)
        {
            DiagLog.Log("notify", "show failed", ex);
        }
    }

    private static void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        args.Arguments.TryGetValue("sn", out var sn);
        Invoked?.Invoke(sn);
    }
}
