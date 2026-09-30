using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using PowerHub.Models;
using PowerHub.Views;

namespace PowerHub;

public sealed partial class MainWindow : Window
{
    private bool _activated;

    public MainWindow()
    {
        InitializeComponent();

        Title = "PowerHub";
        try
        {
            AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));
        }
        catch (Exception ex)
        {
            Services.DiagLog.Log("app", "window icon not set", ex);
        }
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1000, 780));
        AppWindow.Closing += AppWindow_Closing;

        // Навігація до початкового екрану
        var startPage = App.Repository.IsLoggedIn ? typeof(DevicesPage) : typeof(LoginPage);
        RootFrame.Navigate(startPage);
    }

    /// <summary>
    /// Закриття вікна ховає застосунок у трей (моніторинг і сповіщення працюють далі),
    /// якщо це не вимкнено в налаштуваннях
    /// </summary>
    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (App.IsExiting)
            return;

        if (App.Repository.Settings.CloseToTray)
        {
            args.Cancel = true;
            sender.Hide();
        }
        else
        {
            // Вікно закривається саме; лишається зупинити моніторинг і прибрати іконку з трею
            App.ExitApp(windowClosing: true);
        }
    }

    /// <summary>
    /// Показати вікно поверх інших (перший показ — через Activate)
    /// </summary>
    public void ShowAndFocus()
    {
        if (!_activated)
        {
            _activated = true;
            Activate();
            return;
        }

        AppWindow.Show();
        if (AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized } presenter)
            presenter.Restore();
        Activate();
        SetForegroundWindow(WinRT.Interop.WindowNative.GetWindowHandle(this));
    }

    /// <summary>
    /// Відкрити сторінку станції (натискання на сповіщення)
    /// </summary>
    public void OpenDevice(string serialNumber)
    {
        if (!App.Repository.IsLoggedIn || App.Repository.GetDevice(serialNumber) == null)
            return;

        if (RootFrame.Content is DeviceDetailsPage page && page.SerialNumber == serialNumber)
            return;

        if (RootFrame.Content is not DevicesPage)
        {
            RootFrame.Navigate(typeof(DevicesPage));
            RootFrame.BackStack.Clear();
        }
        RootFrame.Navigate(typeof(DeviceDetailsPage), serialNumber);
    }

    /// <summary>
    /// Застосувати тему до всього вмісту вікна
    /// </summary>
    public void ApplyTheme(ThemeMode theme)
    {
        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = theme switch
            {
                ThemeMode.Light => ElementTheme.Light,
                ThemeMode.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hwnd);
}
