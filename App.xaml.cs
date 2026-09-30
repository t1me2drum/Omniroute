using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Omniroute.Data;
using Omniroute.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Omniroute;

public partial class App : Application
{
    // Один екземпляр на користувача: другий запуск лише показує вікно першого
    private const string InstanceMutexName = @"Local\Omniroute.SingleInstance";
    private const string ActivateEventName = @"Local\Omniroute.Activate";

    private static Mutex? _instanceMutex;
    private static EventWaitHandle? _activateEvent;
    private static TrayIcon? _tray;
    private static DispatcherQueue? _dispatcher;
    private static bool _exiting;

    public static MainWindow? MainWindow { get; private set; }
    public static Repository Repository { get; private set; } = null!;

    /// <summary>
    /// Застосунок завершується (а не ховається в трей)
    /// </summary>
    public static bool IsExiting => _exiting;

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) => LogCrash(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogCrash(e.ExceptionObject as Exception);
    }

    /// <summary>
    /// Необроблений виняток іде в журнал діагностики (%LocalAppData%\Omniroute\diag.log)
    /// </summary>
    private static void LogCrash(Exception? ex) => DiagLog.Log("CRASH", "необроблений виняток", ex);

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (!AcquireSingleInstance())
        {
            // Уже запущено: попросити перший екземпляр показати вікно і завершитися
            _activateEvent?.Set();
            Environment.Exit(0);
            return;
        }

        _dispatcher = DispatcherQueue.GetForCurrentThread();
        DiagLog.Log("app", $"запуск {typeof(App).Assembly.GetName().Version?.ToString(3)}");

        Repository = new Repository();

        // Сповіщення: реєстрацію треба виконати до перевірки, чи не натисканням на сповіщення запущено застосунок
        NotificationService.Initialize();
        NotificationService.Invoked += sn => _dispatcher.TryEnqueue(() => ShowMainWindow(sn));
        var launchedForDevice = NotificationDevice();

        // Моніторинг оновлює станції та показує сповіщення в UI-потоці
        MonitorService.Initialize(_dispatcher);
        MonitorService.ParamsUpdated += () => _tray?.SetTooltip("Omniroute\n" + MonitorService.Summary());
        MonitorService.StatusChanged += _ => _tray?.SetTooltip("Omniroute\n" + MonitorService.Summary());

        _tray = new TrayIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));
        _tray.OpenRequested += () => ShowMainWindow();
        _tray.ExitRequested += () => ExitApp();

        // Шлях до exe міг змінитися (нова збірка в іншій папці)
        Autostart.Refresh(Repository.Settings.StartWithWindows);

        MainWindow = new MainWindow();
        MainWindow.ApplyTheme(Repository.Settings.Theme);

        // Автозапуск з Windows: одразу в трей, вікно не показуємо
        var background = Environment.GetCommandLineArgs().Contains(Autostart.BackgroundArg);
        if (!background || launchedForDevice != null)
            ShowMainWindow(launchedForDevice);

        if (Repository.IsLoggedIn)
        {
            MonitorService.Start();
        }
    }

    private static bool AcquireSingleInstance()
    {
        _instanceMutex = new Mutex(true, InstanceMutexName, out var createdNew);
        _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);
        if (!createdNew)
            return false;

        var thread = new Thread(() =>
        {
            while (_activateEvent.WaitOne())
            {
                if (_exiting) return;
                _dispatcher?.TryEnqueue(() => ShowMainWindow());
            }
        })
        {
            IsBackground = true,
            Name = "Omniroute activation"
        };
        thread.Start();
        return true;
    }

    /// <summary>
    /// Серійний номер станції, якщо застосунок запущено натисканням на сповіщення
    /// </summary>
    private static string? NotificationDevice()
    {
        try
        {
            var activation = AppInstance.GetCurrent().GetActivatedEventArgs();
            if (activation.Kind == ExtendedActivationKind.AppNotification &&
                activation.Data is AppNotificationActivatedEventArgs notification &&
                notification.Arguments.TryGetValue("sn", out var sn))
                return sn;
        }
        catch (Exception ex)
        {
            DiagLog.Log("app", "activation args unavailable", ex);
        }
        return null;
    }

    /// <summary>
    /// Показати вікно (з трею, повторного запуску або сповіщення) і, за потреби, відкрити станцію
    /// </summary>
    public static void ShowMainWindow(string? serialNumber = null)
    {
        if (MainWindow == null || _exiting)
            return;

        MainWindow.ShowAndFocus();
        if (!string.IsNullOrEmpty(serialNumber))
            MainWindow.OpenDevice(serialNumber);
    }

    /// <summary>
    /// Повний вихід (пункт «Вийти» в треї або закриття вікна, коли трей вимкнено)
    /// </summary>
    public static void ExitApp(bool windowClosing = false)
    {
        if (_exiting) return;
        _exiting = true;
        DiagLog.Log("app", "вихід");

        MonitorService.Stop();
        NotificationService.Shutdown();
        _tray?.Dispose();
        _tray = null;
        _activateEvent?.Set(); // розбудити потік очікування, щоб він завершився

        if (windowClosing)
        {
            // Не закриваємо вікно вдруге з його ж обробника Closing: вихід — після того, як воно закриється
            _dispatcher?.TryEnqueue(() => Current.Exit());
            return;
        }

        MainWindow?.Close();
        Current.Exit();
    }
}
