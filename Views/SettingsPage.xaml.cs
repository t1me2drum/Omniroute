using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Omniroute.Api;
using Omniroute.Models;
using Omniroute.Services;

namespace Omniroute.Views;

public sealed partial class SettingsPage : Page
{
    // Під час початкового заповнення обробники подій не повинні нічого зберігати
    private bool _loading;

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        _loading = true;

        var credentials = App.Repository.Credentials;
        if (credentials != null)
        {
            EmailText.Text = $"Email: {credentials.Email}";

            // API ключі (секретний ключ не показуємо)
            if (!string.IsNullOrEmpty(credentials.AccessKey))
            {
                AccessKeyBox.Text = credentials.AccessKey;
            }
        }

        var settings = App.Repository.Settings;

        // Тема
        ThemeComboBox.SelectedIndex = settings.Theme switch
        {
            ThemeMode.Light => 0,
            ThemeMode.Dark => 1,
            _ => 2
        };

        // Сповіщення
        NotificationsToggle.IsOn = settings.NotificationsEnabled;
        PowerLossToggle.IsOn = settings.NotifyOnPowerLoss;
        LowBatteryToggle.IsOn = settings.NotifyOnLowBattery;
        FullChargeToggle.IsOn = settings.NotifyOnFullCharge;
        OfflineToggle.IsOn = settings.NotifyOnOffline;

        _loading = false;
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading)
            return;

        if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            var theme = tag switch
            {
                "Light" => ThemeMode.Light,
                "Dark" => ThemeMode.Dark,
                _ => ThemeMode.System
            };

            App.Repository.UpdateSettings(s => s.Theme = theme);
            App.MainWindow?.ApplyTheme(theme);
        }
    }

    private void NotificationsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading)
            App.Repository.UpdateSettings(s => s.NotificationsEnabled = NotificationsToggle.IsOn);
    }

    private void PowerLossToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading)
            App.Repository.UpdateSettings(s => s.NotifyOnPowerLoss = PowerLossToggle.IsOn);
    }

    private void LowBatteryToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading)
            App.Repository.UpdateSettings(s => s.NotifyOnLowBattery = LowBatteryToggle.IsOn);
    }

    private void FullChargeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading)
            App.Repository.UpdateSettings(s => s.NotifyOnFullCharge = FullChargeToggle.IsOn);
    }

    private void OfflineToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading)
            App.Repository.UpdateSettings(s => s.NotifyOnOffline = OfflineToggle.IsOn);
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

        SaveKeysButton.IsEnabled = false;
        KeysProgress.IsActive = true;

        try
        {
            // Ключі зберігаються (зашифрованими) лише якщо з ними вдалося отримати список станцій
            var result = await App.Repository.SaveDeveloperKeysAsync(accessKey, secretKey);
            SecretKeyBox.Password = string.Empty;

            var message = $"Ключі збережено.\nДодано: {result.Added}, оновлено: {result.Updated}, прибрано: {result.Removed}";
            if (result.Unsupported.Count > 0)
            {
                message += $"\n\nНепідтримувані станції:\n{string.Join("\n", result.Unsupported)}";
            }
            await ShowDialog("Успіх", message);
        }
        catch (EcoflowException ex)
        {
            await ShowDialog("Ключі не прийнято", ex.Message);
        }
        catch (Exception ex)
        {
            await ShowDialog("Помилка", ex.Message);
        }
        finally
        {
            SaveKeysButton.IsEnabled = true;
            KeysProgress.IsActive = false;
        }
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
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // Зупинити моніторинг
            MonitorService.Stop();

            // Вийти
            App.Repository.Logout();
            NotificationService.Reset();

            // Перейти до екрану входу без можливості повернутися назад
            Frame.Navigate(typeof(LoginPage));
            Frame.BackStack.Clear();
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack();
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
