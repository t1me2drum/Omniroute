using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Omniroute.Models;
using Omniroute.Services;

namespace Omniroute.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        var credentials = App.Repository.Credentials;
        if (credentials != null)
        {
            EmailText.Text = $"Email: {credentials.Email}";
        }

        var settings = App.Repository.Settings;

        // Тема
        ThemeComboBox.SelectedIndex = settings.Theme switch
        {
            ThemeMode.Light => 0,
            ThemeMode.Dark => 1,
            ThemeMode.System => 2,
            _ => 2
        };

        // Сповіщення
        NotificationsToggle.IsOn = settings.NotificationsEnabled;
        PowerLossToggle.IsOn = settings.NotifyOnPowerLoss;
        LowBatteryToggle.IsOn = settings.NotifyOnLowBattery;

        // API ключі
        if (!string.IsNullOrEmpty(settings.AccessKey))
        {
            AccessKeyBox.Text = settings.AccessKey;
        }
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            var theme = tag switch
            {
                "Light" => ThemeMode.Light,
                "Dark" => ThemeMode.Dark,
                _ => ThemeMode.System
            };

            App.Repository.UpdateSettings(s => s.Theme = theme);
        }
    }

    private void NotificationsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        App.Repository.UpdateSettings(s => s.NotificationsEnabled = NotificationsToggle.IsOn);
    }

    private async void SaveKeysButton_Click(object sender, RoutedEventArgs e)
    {
        var accessKey = AccessKeyBox.Text.Trim();
        var secretKey = SecretKeyBox.Password.Trim();

        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
        {
            await ShowDialog("Помилка", "Введіть обидва ключі");
            return;
        }

        App.Repository.UpdateSettings(s =>
        {
            s.AccessKey = accessKey;
            s.SecretKey = secretKey;
        });

        await ShowDialog("Успіх", "Ключі API збережено");
    }

    private async void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Вийти з акаунту",
            Content = "Ви впевнені, що хочете вийти?",
            PrimaryButtonText = "Вийти",
            CloseButtonText = "Скасувати",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // Зупинити моніторинг
            MonitorService.Stop();

            // Вийти
            App.Repository.Logout();

            // Перейти до екрану входу
            Frame.Navigate(typeof(LoginPage));
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        Frame.GoBack();
    }

    private async Task ShowDialog(string title, string message)
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
