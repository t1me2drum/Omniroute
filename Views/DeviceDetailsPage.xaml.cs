using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using Omniroute.Models;
using Omniroute.Protocol;
using Omniroute.Services;

namespace Omniroute.Views;

public sealed partial class DeviceDetailsPage : Page
{
    /// <summary>
    /// Слайдер надсилає команду, коли значення не змінювалось стільки часу
    /// (замінює onValueChangeFinished з Android: працює і для миші, і для клавіатури)
    /// </summary>
    private static readonly TimeSpan SliderCommitDelay = TimeSpan.FromMilliseconds(700);

    private string? _serialNumber;
    private Device? _device;

    /// <summary>
    /// Оновлювачі елементів керування: переносять значення з параметрів станції в UI
    /// </summary>
    private readonly List<Action<DeviceParams>> _controlRefreshers = new();

    /// <summary>
    /// Значення змінюється програмно — обробники подій не мають надсилати команди
    /// </summary>
    private bool _refreshing;

    public DeviceDetailsPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string serialNumber)
        {
            _serialNumber = serialNumber;
            LoadDevice();
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        if (_device != null)
        {
            _device.PropertyChanged -= Device_PropertyChanged;
        }
        MonitorService.ParamsUpdated -= RefreshControls;
    }

    private void LoadDevice()
    {
        if (string.IsNullOrEmpty(_serialNumber))
            return;

        _device = App.Repository.GetDevice(_serialNumber);

        if (_device == null)
        {
            // Пристрій не знайдено
            if (Frame.CanGoBack)
                Frame.GoBack();
            return;
        }

        // Оновити UI
        DeviceNameText.Text = _device.Name;
        DeviceModelText.Text = _device.Model.GetDisplayName();
        SerialNumberText.Text = _device.SerialNumber;

        // Телеметрія оновлюється MonitorService в UI-потоці
        _device.PropertyChanged += Device_PropertyChanged;
        UpdateDeviceState();

        BuildControls(_device);
        MonitorService.ParamsUpdated += RefreshControls;
        RefreshControls();
    }

    private void Device_PropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateDeviceState();

    private void UpdateDeviceState()
    {
        if (_device == null)
            return;

        // Онлайн статус
        OnlineIndicator.Visibility = _device.IsOnline ? Visibility.Visible : Visibility.Collapsed;
        OfflineWarningText.Visibility = _device.IsOnline ? Visibility.Collapsed : Visibility.Visible;

        // Батарея
        BatteryLevelText.Text = _device.BatteryText;
        BatteryProgress.Value = _device.BatteryPercent;

        // Потужності
        InputText.Text = _device.InputText;
        OutputText.Text = _device.OutputText;
        SolarText.Text = Device.FormatWatts(_device.SolarWatts);

        // Інфо
        TemperatureText.Text = _device.Temperature.HasValue ? $"{_device.Temperature}°C" : "--";
        CyclesText.Text = _device.Cycles?.ToString() ?? "--";
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack();
    }

    #region Керування

    private static string SectionTitle(ControlSection section) => section switch
    {
        ControlSection.Outputs => "Виходи",
        ControlSection.Charging => "Заряджання",
        ControlSection.Backup => "Резерв",
        ControlSection.System => "Система",
        _ => section.ToString()
    };

    /// <summary>
    /// Картка на кожну секцію, як Controls() в Android-версії
    /// </summary>
    private void BuildControls(Device device)
    {
        ControlsPanel.Children.Clear();
        _controlRefreshers.Clear();

        var controls = Protocols.For(device.Model).GetControls(device.SerialNumber);
        foreach (var group in controls.GroupBy(c => c.Section))
        {
            var rows = new StackPanel { Spacing = 12 };
            rows.Children.Add(new TextBlock
            {
                Text = SectionTitle(group.Key),
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });

            foreach (var control in group)
            {
                FrameworkElement? row = control switch
                {
                    ToggleControl t => ToggleRow(device, t),
                    SliderControl s => SliderRow(device, s),
                    ChoiceControl c => ChoiceRow(device, c),
                    _ => null
                };
                if (row != null)
                    rows.Children.Add(row);
            }

            ControlsPanel.Children.Add(new Border
            {
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Child = rows
            });
        }
    }

    private void RefreshControls()
    {
        if (_device == null)
            return;

        var parameters = MonitorService.GetParams(_device.SerialNumber);
        _refreshing = true;
        try
        {
            foreach (var refresh in _controlRefreshers)
            {
                refresh(parameters);
            }
        }
        finally
        {
            _refreshing = false;
        }
    }

    /// <summary>
    /// Показати помилку і повернути елементи керування до фактичного стану
    /// </summary>
    private async void Report(Task<bool> send)
    {
        bool ok;
        try
        {
            ok = await send;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeviceDetailsPage: command failed - {ex.Message}");
            ok = false;
        }

        CommandErrorBar.IsOpen = !ok;
        if (!ok)
            RefreshControls();
    }

    private static TextBlock Label(string text) => new()
    {
        Text = text,
        VerticalAlignment = VerticalAlignment.Center,
        TextWrapping = TextWrapping.Wrap
    };

    private static TextBlock ValueText() => new()
    {
        VerticalAlignment = VerticalAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Right,
        Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"]
    };

    private FrameworkElement ToggleRow(Device device, ToggleControl control)
    {
        var grid = new Grid { ColumnSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var label = Label(control.Label);
        var unknown = ValueText();
        unknown.Text = "—";
        var toggle = new ToggleSwitch { OnContent = "", OffContent = "", MinWidth = 0 };

        Grid.SetColumn(unknown, 1);
        Grid.SetColumn(toggle, 2);
        grid.Children.Add(label);
        grid.Children.Add(unknown);
        grid.Children.Add(toggle);

        toggle.Toggled += (_, _) =>
        {
            if (!_refreshing)
                Report(MonitorService.ToggleAsync(device, control, toggle.IsOn));
        };

        _controlRefreshers.Add(p =>
        {
            var value = control.Read(p);
            unknown.Visibility = value.HasValue ? Visibility.Collapsed : Visibility.Visible;
            toggle.IsOn = value == true;
        });

        return grid;
    }

    private FrameworkElement SliderRow(Device device, SliderControl control)
    {
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var valueText = ValueText();
        Grid.SetColumn(valueText, 1);
        header.Children.Add(Label(control.Label));
        header.Children.Add(valueText);

        var slider = new Slider
        {
            Minimum = control.Min,
            Maximum = control.Max,
            StepFrequency = control.Step,
            SnapsTo = SliderSnapsTo.StepValues,
            TickFrequency = control.Step,
            TickPlacement = TickPlacement.None,
            IsThumbToolTipEnabled = true
        };

        // Поки користувач рухає повзунок, значення від станції його не перебивають
        var timer = DispatcherQueue.CreateTimer();
        timer.Interval = SliderCommitDelay;
        timer.IsRepeating = false;
        timer.Tick += (_, _) =>
        {
            var snapped = SnapToStep(slider.Value, control);
            Report(MonitorService.SetValueAsync(device, control, snapped));
        };

        slider.ValueChanged += (_, e) =>
        {
            if (_refreshing)
                return;
            valueText.Text = $"{SnapToStep(e.NewValue, control)} {control.Unit}";
            timer.Stop();
            timer.Start();
        };

        _controlRefreshers.Add(p =>
        {
            if (timer.IsRunning)
                return;
            var value = control.Read(p);
            valueText.Text = value.HasValue ? $"{value} {control.Unit}" : "—";
            slider.Value = Math.Clamp(value ?? control.Min, control.Min, control.Max);
        });

        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(header);
        panel.Children.Add(slider);
        return panel;
    }

    private static int SnapToStep(double value, SliderControl control)
    {
        var step = Math.Max(control.Step, 1);
        var snapped = control.Min + (int)Math.Round((value - control.Min) / step) * step;
        return Math.Clamp(snapped, control.Min, control.Max);
    }

    private FrameworkElement ChoiceRow(Device device, ChoiceControl control)
    {
        var grid = new Grid { ColumnSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var combo = new ComboBox { MinWidth = 140, PlaceholderText = "—" };
        foreach (var (label, _) in control.Options)
        {
            combo.Items.Add(label);
        }
        Grid.SetColumn(combo, 1);
        grid.Children.Add(Label(control.Label));
        grid.Children.Add(combo);

        combo.SelectionChanged += (_, _) =>
        {
            if (_refreshing || combo.SelectedIndex < 0)
                return;
            Report(MonitorService.ChooseAsync(device, control, control.Options[combo.SelectedIndex].Value));
        };

        _controlRefreshers.Add(p =>
        {
            var value = control.Read(p);
            combo.SelectedIndex = value.HasValue ? control.Options.FindIndex(o => o.Value == value.Value) : -1;
        });

        return grid;
    }

    #endregion
}
