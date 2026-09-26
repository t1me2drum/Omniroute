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

    private void LoadDevice()
    {
        if (string.IsNullOrEmpty(_serialNumber))
            return;

        _device = App.Repository.GetDevice(_serialNumber);

        if (_device == null)
        {
            // Пристрій не знайдено
            Frame.GoBack();
            return;
        }

        // Оновити UI
        DeviceNameText.Text = _device.Name;
        DeviceModelText.Text = DeviceModelExtensions.GetDisplayName(_device.Model);
        SerialNumberText.Text = _device.SerialNumber;

        UpdateDeviceState();
    }

    private void UpdateDeviceState()
    {
        if (_device == null)
            return;

        // Онлайн статус
        OnlineIndicator.Visibility = _device.IsOnline ? Visibility.Visible : Visibility.Collapsed;

        // Батарея
        BatteryLevelText.Text = _device.BatteryLevel > 0 ? $"{_device.BatteryLevel}%" : "--";
        BatteryProgress.Value = _device.BatteryLevel;

        // Потужності
        InputText.Text = _device.InputWatts.HasValue ? $"{_device.InputWatts} Вт" : "-- Вт";
        OutputText.Text = _device.OutputWatts.HasValue ? $"{_device.OutputWatts} Вт" : "-- Вт";
        SolarText.Text = "-- Вт"; // TODO: додати сонячну потужність

        // Інфо
        TemperatureText.Text = _device.Temperature.HasValue ? $"{_device.Temperature}°C" : "--";
        CyclesText.Text = "--"; // TODO: додати цикли
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        Frame.GoBack();
    }
}
