using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Omniroute.Api;
using Omniroute.Models;
using Omniroute.Services;

namespace Omniroute.Views;

public sealed partial class DevicesPage : Page
{
    public ObservableCollection<Device> Devices { get; } = new();

    public DevicesPage()
    {
        InitializeComponent();
        Loaded += DevicesPage_Loaded;
        Unloaded += DevicesPage_Unloaded;
    }

    private void DevicesPage_Loaded(object sender, RoutedEventArgs e)
    {
        App.Repository.DevicesChanged += OnDevicesChanged;
        MonitorService.StatusChanged += OnStatusChanged;
        OnStatusChanged(MonitorService.Status);
        LoadDevices();
    }

    private void DevicesPage_Unloaded(object sender, RoutedEventArgs e)
    {
        App.Repository.DevicesChanged -= OnDevicesChanged;
        MonitorService.StatusChanged -= OnStatusChanged;
    }

    /// <summary>
    /// DevicesChanged може прийти з фонового потоку
    /// </summary>
    private void OnDevicesChanged() => DispatcherQueue.TryEnqueue(LoadDevices);

    private void OnStatusChanged(string status) => StatusText.Text = status;

    private void LoadDevices()
    {
        Devices.Clear();
        foreach (var device in App.Repository.ActiveDevices)
        {
            Devices.Add(device);
        }

        UpdateEmptyState();
    }

    private void ShowLoading(bool isLoading)
    {
        LoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        if (isLoading)
        {
            EmptyPanel.Visibility = Visibility.Collapsed;
            DevicesScrollViewer.Visibility = Visibility.Collapsed;
        }
        else
        {
            UpdateEmptyState();
        }
    }

    private void UpdateEmptyState()
    {
        bool isEmpty = Devices.Count == 0;
        EmptyPanel.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        DevicesScrollViewer.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (App.Repository.Credentials?.HasDeveloperKeys != true)
        {
            LoadDevices();
            return;
        }

        ShowLoading(true);
        try
        {
            var result = await App.Repository.SyncStationsAsync();
            LoadDevices();

            if (result.Unsupported.Count > 0)
            {
                await ShowDialog("Непідтримувані станції", string.Join("\n", result.Unsupported));
            }
        }
        catch (EcoflowException ex)
        {
            await ShowDialog("Не вдалося синхронізувати станції", ex.Message);
        }
        catch (Exception ex)
        {
            await ShowDialog("Не вдалося синхронізувати станції", ex.Message);
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var snBox = new TextBox { Header = "Серійний номер", PlaceholderText = "R331ZEB4ZE8M0000" };
        var nameBox = new TextBox { Header = "Назва (необов'язково)" };
        var hint = new TextBlock
        {
            Text = "Щоб підтягнути всі станції акаунта автоматично, введіть ключі Developer API в налаштуваннях.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.7
        };

        var dialog = new ContentDialog
        {
            Title = "Додати станцію",
            Content = new StackPanel { Spacing = 12, Children = { snBox, nameBox, hint } },
            PrimaryButtonText = "Додати",
            CloseButtonText = "Скасувати",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        var sn = snBox.Text.Trim();
        if (sn.Length < 4)
        {
            await ShowDialog("Помилка", "Введіть коректний серійний номер");
            return;
        }

        var device = App.Repository.AddDevice(sn, nameBox.Text);
        if (device.Model == DeviceModel.Unknown)
        {
            await ShowDialog("Невідома модель",
                "Модель станції не визначено за серійним номером. Показники можуть не відображатися.");
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(SettingsPage));
    }

    private void DeviceButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string serialNumber)
        {
            Frame.Navigate(typeof(DeviceDetailsPage), serialNumber);
        }
    }

    private async Task ShowDialog(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot
        };
        await dialog.ShowAsync();
    }
}
