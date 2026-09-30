using System;
using System.Diagnostics;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Omniroute.Models;
using Omniroute.Services;
using Windows.ApplicationModel.DataTransfer;

namespace Omniroute.Views;

public sealed partial class SettingsPage : Page
{
    // Під час створення сторінки й початкового заповнення обробники подій не повинні нічого зберігати
    // (слайдер із Minimum=150 змінює значення ще під час InitializeComponent)
    private bool _loading = true;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            MonitorService.StatusChanged += OnStatusChanged;
            OnStatusChanged(MonitorService.Status);
        };
        Unloaded += (_, _) => MonitorService.StatusChanged -= OnStatusChanged;
        LoadSettings();
    }

    private void OnStatusChanged(string status)
    {
        var state = MonitorService.State;
        ConnectionBar.IsOpen = state is ConnectionState.Reconnecting or ConnectionState.Failed;
        ConnectionBar.Severity = state == ConnectionState.Failed ? InfoBarSeverity.Error : InfoBarSeverity.Informational;
        ConnectionBar.Message = state == ConnectionState.Failed ? $"{status}. Увійдіть знову." : status;
    }

    private void LoadSettings()
    {
        _loading = true;

        var credentials = App.Repository.Credentials;
        if (credentials != null)
        {
            EmailText.Text = $"Email: {credentials.Email}";
            AccessKeyBox.Text = credentials.AccessKey ?? string.Empty;
            // Секретний ключ не показуємо: поле лишається порожнім, поки користувач не введе новий
        }
        ClearKeysButton.Visibility = Ui.VisibleIf(credentials?.HasDeveloperKeys == true);

        var settings = App.Repository.Settings;

        ThemeButtons.SelectedIndex = settings.Theme switch
        {
            ThemeMode.Dark => 0,
            ThemeMode.Light => 1,
            _ => 2
        };

        LayoutButtons.SelectedIndex = settings.Layout == HomeLayout.Dashboard ? 1 : 0;
        CloseToTrayToggle.IsOn = settings.CloseToTray;
        AutostartToggle.IsOn = settings.StartWithWindows;

        NotificationsToggle.IsOn = settings.NotificationsEnabled;
        PowerLossToggle.IsOn = settings.NotifyOnPowerLoss;
        LowBatteryToggle.IsOn = settings.NotifyOnLowBattery;
        FullChargeToggle.IsOn = settings.NotifyOnFullCharge;
        OfflineToggle.IsOn = settings.NotifyOnOffline;
        WeakGridSlider.Value = settings.WeakGridVolt;
        LowBatterySlider.Value = settings.LowBatteryThreshold;
        UpdateSliderLabels();

        VersionText.Text = $"Omniroute {typeof(App).Assembly.GetName().Version?.ToString(3)} — Windows-клієнт для станцій EcoFlow";
        UpdateDiagText();
        LoadHidden();

        _loading = false;
    }

    private void UpdateSliderLabels()
    {
        WeakGridLabel.Text = $"Слабка мережа — нижче {(int)WeakGridSlider.Value} В (станція не заряджається)";
        LowBatteryLabel.Text = $"Поріг: {(int)LowBatterySlider.Value}%";
        LowBatteryLabel.Visibility = LowBatterySlider.Visibility = Ui.VisibleIf(LowBatteryToggle.IsOn);
    }

    private void ThemeButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading)
            return;

        var theme = ThemeButtons.SelectedIndex switch
        {
            0 => ThemeMode.Dark,
            1 => ThemeMode.Light,
            _ => ThemeMode.System
        };
        App.Repository.UpdateSettings(s => s.Theme = theme);
        App.MainWindow?.ApplyTheme(theme);
    }

    private void LayoutButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loading && LayoutButtons.SelectedIndex >= 0)
            App.Repository.UpdateSettings(s => s.Layout = LayoutButtons.SelectedIndex == 1 ? HomeLayout.Dashboard : HomeLayout.Compact);
    }

    private void CloseToTrayToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loading)
            App.Repository.UpdateSettings(s => s.CloseToTray = CloseToTrayToggle.IsOn);
    }

    private void AutostartToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_loading)
            return;
        App.Repository.UpdateSettings(s => s.StartWithWindows = AutostartToggle.IsOn);
        Autostart.Apply(AutostartToggle.IsOn);
    }

    /// <summary>
    /// Усі перемикачі сповіщень зберігаються разом
    /// </summary>
    private void NotificationToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_loading)
            return;

        App.Repository.UpdateSettings(s =>
        {
            s.NotificationsEnabled = NotificationsToggle.IsOn;
            s.NotifyOnPowerLoss = PowerLossToggle.IsOn;
            s.NotifyOnLowBattery = LowBatteryToggle.IsOn;
            s.NotifyOnFullCharge = FullChargeToggle.IsOn;
            s.NotifyOnOffline = OfflineToggle.IsOn;
        });
        UpdateSliderLabels();
    }

    private void WeakGridSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_loading)
            return;
        App.Repository.UpdateSettings(s => s.WeakGridVolt = (int)Math.Round(e.NewValue / 5) * 5);
        UpdateSliderLabels();
    }

    private void LowBatterySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_loading)
            return;
        App.Repository.UpdateSettings(s => s.LowBatteryThreshold = (int)Math.Round(e.NewValue));
        UpdateSliderLabels();
    }

    #region Ключі й станції

    private void ShowKeysMessage(string text, bool error)
    {
        KeysMessage.Text = text;
        KeysMessage.Foreground = error ? Ui.ErrorBrush : Ui.SuccessBrush;
        KeysMessage.Visibility = Visibility.Visible;
    }

    private async void SaveKeysButton_Click(object sender, RoutedEventArgs e)
    {
        var accessKey = AccessKeyBox.Text.Trim();
        var secretKey = SecretKeyBox.Password.Trim();

        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
        {
            ShowKeysMessage("Введіть обидва ключі", error: true);
            return;
        }

        SaveKeysButton.IsEnabled = false;
        KeysProgress.IsActive = true;
        KeysMessage.Visibility = Visibility.Collapsed;

        try
        {
            // Ключі зберігаються (зашифрованими) лише якщо з ними вдалося отримати список станцій
            var result = await App.Repository.SaveDeveloperKeysAsync(accessKey, secretKey);
            SecretKeyBox.Password = string.Empty;
            ClearKeysButton.Visibility = Visibility.Visible;
            ShowKeysMessage(Ui.DescribeSync(result), error: false);
        }
        catch (Exception ex)
        {
            ShowKeysMessage($"Не вдалося: {ex.Message}", error: true);
        }
        finally
        {
            SaveKeysButton.IsEnabled = true;
            KeysProgress.IsActive = false;
        }
    }

    private void ClearKeysButton_Click(object sender, RoutedEventArgs e)
    {
        App.Repository.ClearDeveloperKeys();
        AccessKeyBox.Text = string.Empty;
        SecretKeyBox.Password = string.Empty;
        ClearKeysButton.Visibility = Visibility.Collapsed;
        ShowKeysMessage("Ключі видалено. Станції лишилися в списку.", error: false);
    }

    /// <summary>
    /// Приховані станції з акаунта з кнопкою «Повернути»
    /// </summary>
    private void LoadHidden()
    {
        HiddenList.Children.Clear();
        var hidden = App.Repository.HiddenDevices;
        HiddenCard.Visibility = Ui.VisibleIf(hidden.Count > 0);

        foreach (var device in hidden)
        {
            var row = new Grid { ColumnSpacing = 8 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var info = new StackPanel();
            info.Children.Add(new TextBlock { Text = device.Name });
            info.Children.Add(new TextBlock { Text = device.SerialNumber, FontSize = 12, Opacity = 0.6 });
            row.Children.Add(info);

            var restore = new Button { Content = "Повернути", VerticalAlignment = VerticalAlignment.Center };
            var sn = device.SerialNumber;
            restore.Click += async (_, _) =>
            {
                restore.IsEnabled = false;
                try
                {
                    await App.Repository.RestoreDeviceAsync(sn);
                }
                catch (Exception ex)
                {
                    DiagLog.Log("sync", "restore sync failed", ex);
                }
                LoadHidden();
            };
            Grid.SetColumn(restore, 1);
            row.Children.Add(restore);

            HiddenList.Children.Add(row);
        }
    }

    #endregion

    #region Діагностика

    private void UpdateDiagText()
    {
        DiagText.Text = $"Журнал підключень, збоїв розбору даних і падінь застосунку ({DiagLog.LineCount()} рядків). " +
                        "Паролів, токенів і ключів у ньому немає. Надішліть його, якщо щось працює не так.";
    }

    private void OpenLogButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!File.Exists(DiagLog.FilePath))
                DiagLog.Log("app", "журнал створено");
            Process.Start(new ProcessStartInfo(DiagLog.FilePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            DiagLog.Log("app", "open log failed", ex);
        }
    }

    private void CopyLogButton_Click(object sender, RoutedEventArgs e)
    {
        var package = new DataPackage();
        package.SetText(DiagLog.ExportText());
        Clipboard.SetContent(package);
        DiagText.Text = "Журнал скопійовано в буфер обміну.";
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        DiagLog.Clear();
        UpdateDiagText();
    }

    #endregion

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

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        MonitorService.Stop();
        App.Repository.Logout();
        NotificationService.Reset();

        // Перейти до екрану входу без можливості повернутися назад
        Frame.Navigate(typeof(LoginPage));
        Frame.BackStack.Clear();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack();
    }
}
