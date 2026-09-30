using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using PowerHub.Models;
using PowerHub.Protocol;
using PowerHub.Services;
using Windows.Foundation;

namespace PowerHub.Views;

/// <summary>
/// Плитка станції для десктопного дашборда: кільце заряду, потужності, стан мережі,
/// міні-графік заряду за 24 год і швидкі перемикачі виходів AC / DC / USB
/// </summary>
public sealed class StationTile : Grid
{
    public const double TileWidth = 340;
    public const double TileHeight = 290;

    private const double SparkHeight = 36;
    private static readonly TimeSpan SparkRange = TimeSpan.FromHours(24);
    private static readonly TimeSpan SparkRefresh = TimeSpan.FromMinutes(5);

    /// <summary>Перемикачі виходів, які показуються на плитці: id елемента керування → підпис кнопки</summary>
    private static readonly (string Id, string Label)[] QuickOutputs =
    {
        ("ac_out", "AC"), ("ac_hv", "AC"), ("ac_lv", "AC LV"), ("dc_out", "DC"), ("dc12", "DC"), ("usb_out", "USB")
    };

    private static readonly SolidColorBrush SparkBrush = new(ColorHelper.FromArgb(0xFF, 0x0E, 0x9F, 0x82));
    private static readonly SolidColorBrush OnlineBrush = new(ColorHelper.FromArgb(0xFF, 0x0E, 0x9F, 0x82));
    private static readonly SolidColorBrush OfflineBrush = new(ColorHelper.FromArgb(0xFF, 0x80, 0x80, 0x80));

    private readonly TextBlock _name = new() { FontSize = 17, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock _model = new() { FontSize = 12, Opacity = 0.7 };
    private readonly Ellipse _dot = new() { Width = 8, Height = 8, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _online = new() { FontSize = 12, Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center };
    private readonly SocRing _ring = new() { RingSize = 96, StrokeWidth = 9, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _input = new() { FontSize = 20 };
    private readonly TextBlock _output = new() { FontSize = 20 };
    private readonly TextBlock _grid = new() { FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock _flow = new() { FontSize = 13, Opacity = 0.7, TextWrapping = TextWrapping.Wrap, MaxLines = 2 };
    private readonly Canvas _spark = new() { Height = SparkHeight };
    private readonly TextBlock _sparkLabel = new() { FontSize = 11, Opacity = 0.6, Text = "заряд за 24 год" };
    private readonly StackPanel _buttons = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
    private readonly List<(ToggleControl Control, ToggleButton Button)> _toggles = new();
    private readonly DispatcherQueueTimer _sparkTimer;

    private List<(DateTime Ts, int Soc)> _sparkData = new();
    private bool _refreshing;
    private bool _loading;

    public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register(
        nameof(Device), typeof(Device), typeof(StationTile), new PropertyMetadata(null, (d, e) =>
            ((StationTile)d).OnDeviceChanged(e.OldValue as Device, e.NewValue as Device)));

    public Device? Device
    {
        get => (Device?)GetValue(DeviceProperty);
        set => SetValue(DeviceProperty, value);
    }

    public StationTile()
    {
        Width = TileWidth;
        Height = TileHeight;
        Padding = new Thickness(16);
        RowSpacing = 10;
        for (var i = 0; i < 4; i++)
            RowDefinitions.Add(new RowDefinition { Height = i == 1 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });

        // Рядок 0: назва, модель, онлайн
        var header = new Grid { ColumnSpacing = 8 };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var titles = new StackPanel();
        titles.Children.Add(_name);
        titles.Children.Add(_model);
        header.Children.Add(titles);
        var status = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Top };
        status.Children.Add(_dot);
        status.Children.Add(_online);
        SetColumn(status, 1);
        header.Children.Add(status);
        Children.Add(header);

        // Рядок 1: кільце і потужності
        var body = new Grid { ColumnSpacing = 16 };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        body.Children.Add(_ring);
        var numbers = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        numbers.Children.Add(_input);
        numbers.Children.Add(_output);
        numbers.Children.Add(_grid);
        numbers.Children.Add(_flow);
        SetColumn(numbers, 1);
        body.Children.Add(numbers);
        SetRow(body, 1);
        Children.Add(body);

        // Рядок 2: міні-графік заряду
        var spark = new StackPanel { Spacing = 2 };
        spark.Children.Add(_spark);
        spark.Children.Add(_sparkLabel);
        SetRow(spark, 2);
        Children.Add(spark);
        _spark.SizeChanged += (_, _) => DrawSpark();

        // Рядок 3: швидкі перемикачі виходів
        SetRow(_buttons, 3);
        Children.Add(_buttons);

        _sparkTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _sparkTimer.Interval = SparkRefresh;
        _sparkTimer.Tick += (_, _) => LoadSpark();

        Loaded += (_, _) =>
        {
            MonitorService.ParamsUpdated += RefreshToggles;
            _sparkTimer.Start();
            LoadSpark();
            Update();
        };
        Unloaded += (_, _) =>
        {
            MonitorService.ParamsUpdated -= RefreshToggles;
            _sparkTimer.Stop();
        };
    }

    private void OnDeviceChanged(Device? oldDevice, Device? newDevice)
    {
        if (oldDevice != null)
            oldDevice.PropertyChanged -= Device_PropertyChanged;
        if (newDevice != null)
            newDevice.PropertyChanged += Device_PropertyChanged;

        BuildToggles();
        _sparkData = new List<(DateTime, int)>();
        DrawSpark();
        if (IsLoaded)
            LoadSpark();
        Update();
    }

    private void Device_PropertyChanged(object? sender, PropertyChangedEventArgs e) => Update();

    private void Update()
    {
        var d = Device;
        if (d == null)
            return;

        var s = d.State;
        _name.Text = d.Name;
        _model.Text = d.ModelName;
        _online.Text = d.IsOnline ? "онлайн" : d.HasData ? "не на зв'язку" : "очікування";
        _dot.Fill = d.IsOnline ? OnlineBrush : OfflineBrush;

        _ring.Soc = s.Soc;
        _ring.IsOnline = d.IsOnline;
        _ring.IsCharging = d.IsChargingFromGrid;

        _input.Text = $"↓ {Format.Watts(s.InputW)}";
        _output.Text = $"↑ {Format.Watts(s.OutputW)}";
        _grid.Text = d.IsOnline ? Format.GridShort(d.GridStatus, s.AcInVolt) : string.Empty;
        // Звичайний колір тексту успадковується від теми; кольором лише слабка мережа або її відсутність
        if (d.GridLevel == 0)
            _grid.ClearValue(TextBlock.ForegroundProperty);
        else
            _grid.Foreground = Ui.AlertBrush(d.GridLevel);
        _flow.Text = d.CardStatusText;
    }

    #region Перемикачі

    private void BuildToggles()
    {
        _buttons.Children.Clear();
        _toggles.Clear();
        var d = Device;
        if (d == null)
            return;

        var controls = Protocols.For(d.Model).GetControls(d.SerialNumber).OfType<ToggleControl>().ToList();
        foreach (var (id, label) in QuickOutputs)
        {
            var control = controls.FirstOrDefault(c => c.Id == id);
            if (control == null)
                continue;

            var button = new ToggleButton { Content = label, MinWidth = 64 };
            ToolTipService.SetToolTip(button, control.Label);
            button.Click += async (_, _) =>
            {
                if (_refreshing || Device is not Device device)
                    return;
                var on = button.IsChecked == true;
                bool ok;
                try
                {
                    ok = await MonitorService.ToggleAsync(device, control, on);
                }
                catch (Exception ex)
                {
                    DiagLog.Log("cmd", "quick toggle failed", ex);
                    ok = false;
                }
                if (!ok)
                    RefreshToggles();
            };
            _buttons.Children.Add(button);
            _toggles.Add((control, button));
        }
        RefreshToggles();
    }

    private void RefreshToggles()
    {
        var d = Device;
        if (d == null)
            return;

        var parameters = MonitorService.GetParams(d.SerialNumber);
        _refreshing = true;
        try
        {
            foreach (var (control, button) in _toggles)
            {
                var value = control.Read(parameters);
                button.IsChecked = value == true;
                button.Opacity = value.HasValue ? 1 : 0.6;
            }
        }
        finally
        {
            _refreshing = false;
        }
    }

    #endregion

    #region Міні-графік

    private async void LoadSpark()
    {
        var d = Device;
        if (d == null || _loading)
            return;

        _loading = true;
        try
        {
            var to = DateTime.Now;
            var samples = await App.Repository.GetHistoryAsync(d.SerialNumber, to - SparkRange, to);
            if (Device != d)
                return;
            _sparkData = samples.Select(h => (h.Timestamp, h.BatteryLevel)).ToList();
            DrawSpark();
        }
        catch (Exception ex)
        {
            DiagLog.Log("history", "sparkline query failed", ex);
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>
    /// Заряд за останні 24 год (вісь часу — повні 24 год, пропуски розривають лінію)
    /// </summary>
    private void DrawSpark()
    {
        _spark.Children.Clear();
        var width = _spark.ActualWidth;
        _sparkLabel.Text = _sparkData.Count < 2 ? "заряд за 24 год: історія ще збирається" : "заряд за 24 год";
        if (width <= 0 || _sparkData.Count < 2)
            return;

        var end = DateTime.Now;
        var start = end - SparkRange;
        var bucket = Math.Max(1, _sparkData.Count / 160);
        Polyline? line = null;
        var prev = DateTime.MinValue;

        for (var i = 0; i < _sparkData.Count; i += bucket)
        {
            var group = _sparkData.GetRange(i, Math.Min(bucket, _sparkData.Count - i));
            var ts = group[0].Ts;
            var soc = group.Average(p => p.Soc);
            if (ts - prev > TimeSpan.FromMinutes(5) * bucket)
                line = null;
            prev = ts;

            if (line == null)
            {
                line = new Polyline { Stroke = SparkBrush, StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round };
                _spark.Children.Add(line);
            }
            var x = (ts - start).TotalMilliseconds / SparkRange.TotalMilliseconds * width;
            var y = SparkHeight - Math.Clamp(soc / 100, 0, 1) * (SparkHeight - 2) - 1;
            line.Points.Add(new Point(Math.Clamp(x, 0, width), y));
        }
    }

    #endregion
}
