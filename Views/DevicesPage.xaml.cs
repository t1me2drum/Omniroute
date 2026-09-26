using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Omniroute.Models;
using System.Collections.ObjectModel;

namespace Omniroute.Views;

public sealed partial class DevicesPage : Page
{
    public ObservableCollection<Device> Devices { get; } = new();

    public DevicesPage()
    {
        InitializeComponent();
        Loaded += DevicesPage_Loaded;
    }

    private async void DevicesPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadDevicesAsync();
    }

    private async Task LoadDevicesAsync()
    {
        ShowLoading(true);

        try
        {
            // Завантажити з Repository
            var devices = App.Repository.Devices.Where(d => !d.IsDeleted).ToList();

            Devices.Clear();
            foreach (var device in devices)
            {
                Devices.Add(device);
            }

            // Оновити видимість
            UpdateEmptyState();
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("Помилка завантаження", ex.Message);
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private void ShowLoading(bool isLoading)
    {
        LoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        DevicesScrollViewer.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateEmptyState()
    {
        bool isEmpty = Devices.Count == 0;
        EmptyPanel.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        DevicesScrollViewer.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadDevicesAsync();
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Додати станцію",
            Content = "Функція додавання станцій буде реалізована найближчим часом",
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(SettingsPage));
    }

    private void DeviceButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string serialNumber)
        {
            // TODO: Навігація до екрану деталей пристрою
            Frame.Navigate(typeof(DeviceDetailsPage), serialNumber);
        }
    }

    private async Task ShowErrorDialog(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
