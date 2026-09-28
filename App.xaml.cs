using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Omniroute.Data;
using Omniroute.Services;
using System;
using System.IO;

namespace Omniroute;

public partial class App : Application
{
    public static MainWindow? MainWindow { get; private set; }
    public static Repository Repository { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) => LogCrash(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogCrash(e.ExceptionObject as Exception);
    }

    /// <summary>
    /// Записати необроблений виняток у %LocalAppData%\Omniroute\crash.log
    /// </summary>
    private static void LogCrash(Exception? ex)
    {
        try
        {
            Directory.CreateDirectory(LocalStore.AppFolder);
            File.AppendAllText(Path.Combine(LocalStore.AppFolder, "crash.log"), $"[{DateTime.Now:O}] {ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Нема куди повідомити про помилку запису журналу
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Ініціалізація Repository
        Repository = new Repository();

        // Ініціалізація сповіщень
        NotificationService.Initialize();

        // Моніторинг оновлює станції та показує сповіщення в UI-потоці
        MonitorService.Initialize(DispatcherQueue.GetForCurrentThread());

        MainWindow = new MainWindow();
        MainWindow.ApplyTheme(Repository.Settings.Theme);
        MainWindow.Activate();

        // Запуск фонового моніторингу якщо користувач авторизований
        if (Repository.IsLoggedIn)
        {
            MonitorService.Start();
        }
    }
}
