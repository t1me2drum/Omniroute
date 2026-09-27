using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Omniroute.Models;

namespace Omniroute.Views;

public sealed partial class DeviceDetailsPage : Page
{
    private string? _serialNumber;
    private Device? _device;

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
    }

    private void Device_PropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateDeviceState();

    private void UpdateDeviceState()
    {
        if (_device == null)
            return;

        // Онлайн статус
        OnlineIndicator.Visibility = _device.IsOnline ? Visibility.Visible : Visibility.Collapsed;

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
}
