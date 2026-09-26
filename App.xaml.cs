using Microsoft.UI.Xaml;
using Omniroute.Data;
using Omniroute.Services;
using System;

namespace Omniroute;

public partial class App : Application
{
    private Window? _window;
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

        _window = new MainWindow();
        _window.Activate();

        // Запуск фонового моніторингу якщо користувач авторизований
        if (Repository.IsLoggedIn)
        {
            MonitorService.Start();
        }
    }
}
