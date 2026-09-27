using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Omniroute.Data;
using Omniroute.Services;
using System;

namespace Omniroute;

public partial class App : Application
{
    public static MainWindow? MainWindow { get; private set; }
    public static Repository Repository { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
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
