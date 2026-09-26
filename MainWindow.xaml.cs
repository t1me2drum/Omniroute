using Microsoft.UI.Xaml;
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
}
