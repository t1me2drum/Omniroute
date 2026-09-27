using Microsoft.UI.Xaml;
using Omniroute.Models;
using Omniroute.Views;

namespace Omniroute;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Налаштування вікна
        Title = "Omniroute - EcoFlow Monitor";

        // Навігація до початкового екрану
        var startPage = App.Repository.IsLoggedIn ? typeof(DevicesPage) : typeof(LoginPage);
        RootFrame.Navigate(startPage);
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
}
