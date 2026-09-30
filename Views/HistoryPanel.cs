using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Omniroute.Models;
using Windows.Foundation;

namespace Omniroute.Views;

/// <summary>
/// Графіки історії (як HistoryPane в Android-версії): заряд і потужність за 6 год, 24 год, 7 і 30 днів,
/// енергія за період і час без мережі. Дані перечитуються раз на хвилину, поки вкладка відкрита.
/// </summary>
public sealed class HistoryPanel : StackPanel
{
    private static readonly (string Label, int Hours)[] Ranges =
    {
        ("6 год", 6), ("24 год", 24), ("7 днів", 24 * 7), ("30 днів", 24 * 30)
    };

    /// <summary>Розрив лінії, якщо між точками більше (моніторинг не працював або станція була офлайн)</summary>
    private static readonly TimeSpan GapThreshold = TimeSpan.FromMinutes(5);

    /// <summary>Більше точок на лінію не малюємо: сусідні точки усереднюються</summary>
    private const int MaxPoints = 720;

    private const double ChartHeight = 180;

    private static readonly SolidColorBrush InputBrush = new(ColorHelper.FromArgb(0xFF, 0x0E, 0x9F, 0x82));
    private static readonly SolidColorBrush OutputBrush = new(ColorHelper.FromArgb(0xFF, 0x5B, 0x8D, 0xEF));
    private static readonly SolidColorBrush SolarBrush = new(ColorHelper.FromArgb(0xFF, 0xF2, 0xA9, 0x00));
    private static readonly SolidColorBrush GridLineBrush = new(ColorHelper.FromArgb(0x50, 0x80, 0x80, 0x80));

    private sealed record Series(string Name, Brush Brush, Func<HistoryEntry, int?> Value);

    private readonly RadioButtons _range = new() { MaxColumns = 4 };
    private readonly StackPanel _content = new() { Spacing = 12 };
    private readonly DispatcherQueueTimer _timer;
    private string? _serialNumber;
    private bool _loading;

    public HistoryPanel()
    {
        Spacing = 12;
        foreach (var (label, _) in Ranges)
            _range.Items.Add(label);
        _range.SelectedIndex = 1;
        _range.SelectionChanged += (_, _) => Reload();
        Children.Add(_range);
        Children.Add(_content);

        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.Interval = TimeSpan.FromMinutes(1);
        _timer.Tick += (_, _) => Reload();
    }

    public void Start(string serialNumber)
    {
        _serialNumber = serialNumber;
        _timer.Start();
        Reload();
    }

    public void Stop() => _timer.Stop();

    private async void Reload()
    {
        if (_serialNumber == null || _loading || _range.SelectedIndex < 0)
            return;

        _loading = true;
        try
        {
            var to = DateTime.Now;
            var from = to.AddHours(-Ranges[_range.SelectedIndex].Hours);
            var samples = await App.Repository.GetHistoryAsync(_serialNumber, from, to);
            Render(samples);
        }
        catch (Exception ex)
        {
            Services.DiagLog.Log("history", "query failed", ex);
        }
        finally
        {
            _loading = false;
        }
    }

    private void Render(List<HistoryEntry> samples)
    {
        _content.Children.Clear();

        if (samples.Count < 2)
        {
            _content.Children.Add(new TextBlock
            {
                Text = "Історія ще збирається. Точки записуються щохвилини, поки працює моніторинг (зокрема з трею).",
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.7
            });
            return;
        }

        _content.Children.Add(ChartCard("Рівень заряду, %", samples,
            new[] { new Series("Заряд", InputBrush, s => s.BatteryLevel) }, fixedMax: 100));
        _content.Children.Add(ChartCard("Потужність, Вт", samples, new[]
        {
            new Series("Вхід", InputBrush, s => s.InputWatts),
            new Series("Вихід", OutputBrush, s => s.OutputWatts),
            new Series("Сонце", SolarBrush, s => s.SolarWatts)
        }));

        // Кожна точка — одна хвилина, тож Вт / 60 = Вт·год
        var inWh = samples.Sum(s => (double)(s.InputWatts ?? 0)) / 60;
        var outWh = samples.Sum(s => (double)(s.OutputWatts ?? 0)) / 60;
        var solarWh = samples.Sum(s => (double)(s.SolarWatts ?? 0)) / 60;
        var outageMin = samples.Count(s => s.Grid == false);

        var energy = new StackPanel { Spacing = 8 };
        energy.Children.Add(Title("Енергія за період"));
        energy.Children.Add(Row("Отримано", Format.WattHours(inWh)));
        energy.Children.Add(Row("Спожито", Format.WattHours(outWh)));
        energy.Children.Add(Row("З сонця", Format.WattHours(solarWh)));
        energy.Children.Add(Row("Без мережі", Format.Minutes(outageMin)));
        _content.Children.Add(Card(energy));
    }

    private FrameworkElement ChartCard(string title, List<HistoryEntry> samples, Series[] series, double? fixedMax = null)
    {
        var t0 = samples[0].Timestamp;
        var t1 = samples[^1].Timestamp;
        var observedMax = samples.Max(s => series.Max(x => x.Value(s) ?? 0));
        var maxValue = fixedMax ?? Math.Max(observedMax * 1.1, 10);
        var timeFormat = t1 - t0 > TimeSpan.FromHours(36) ? "dd.MM" : "HH:mm";

        var panel = new StackPanel { Spacing = 8 };

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(Title(title));
        var max = new TextBlock { Text = $"макс {(int)maxValue}", FontSize = 12, Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(max, 1);
        header.Children.Add(max);
        panel.Children.Add(header);

        var canvas = new Canvas { Height = ChartHeight, HorizontalAlignment = HorizontalAlignment.Stretch };
        canvas.SizeChanged += (_, e) => DrawChart(canvas, e.NewSize.Width, samples, series, maxValue, t0, t1);
        panel.Children.Add(canvas);

        var axis = new Grid();
        axis.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        axis.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        axis.Children.Add(new TextBlock { Text = t0.ToString(timeFormat), FontSize = 12, Opacity = 0.7 });
        var end = new TextBlock { Text = t1.ToString(timeFormat), FontSize = 12, Opacity = 0.7 };
        Grid.SetColumn(end, 1);
        axis.Children.Add(end);
        panel.Children.Add(axis);

        if (series.Length > 1)
        {
            var legend = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
            foreach (var s in series)
            {
                var item = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                item.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = s.Brush, VerticalAlignment = VerticalAlignment.Center });
                item.Children.Add(new TextBlock { Text = s.Name, FontSize = 12 });
                legend.Children.Add(item);
            }
            panel.Children.Add(legend);
        }

        return Card(panel);
    }

    private static void DrawChart(Canvas canvas, double width, List<HistoryEntry> samples, Series[] series,
        double maxValue, DateTime t0, DateTime t1)
    {
        canvas.Children.Clear();
        if (width <= 0)
            return;

        var h = ChartHeight;
        for (var i = 0; i <= 4; i++)
        {
            var y = h * i / 4;
            canvas.Children.Add(new Line
            {
                X1 = 0, Y1 = y, X2 = width, Y2 = y,
                Stroke = GridLineBrush, StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 3, 3 }
            });
        }

        var span = Math.Max((t1 - t0).TotalMilliseconds, 1);
        var bucket = Math.Max(1, (int)Math.Ceiling(samples.Count / (double)MaxPoints));
        var gap = GapThreshold * bucket;

        foreach (var s in series)
        {
            Polyline? line = null;
            var prev = DateTime.MinValue;

            foreach (var (ts, value) in Downsample(samples, s.Value, bucket))
            {
                // Лінія рветься на пропусках (моніторинг не працював або станція була офлайн)
                if (value == null || ts - prev > gap)
                    line = null;
                prev = ts;
                if (value == null)
                    continue;

                if (line == null)
                {
                    line = new Polyline { Stroke = s.Brush, StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round };
                    canvas.Children.Add(line);
                }
                var x = (ts - t0).TotalMilliseconds / span * width;
                var y = h - Math.Clamp(value.Value / maxValue, 0, 1) * h;
                line.Points.Add(new Point(x, y));
            }
        }
    }

    /// <summary>
    /// Усереднити сусідні точки групами по bucket; група без значень дає null
    /// </summary>
    private static IEnumerable<(DateTime Ts, double? Value)> Downsample(List<HistoryEntry> samples, Func<HistoryEntry, int?> value, int bucket)
    {
        for (var i = 0; i < samples.Count; i += bucket)
        {
            var group = samples.GetRange(i, Math.Min(bucket, samples.Count - i));
            var values = group.Select(value).Where(v => v.HasValue).Select(v => (double)v!.Value).ToList();
            yield return (group[0].Timestamp, values.Count > 0 ? values.Average() : null);
        }
    }

    private static TextBlock Title(string text) => new() { Text = text, FontSize = 15, FontWeight = FontWeights.SemiBold };

    private static Grid Row(string label, string value)
    {
        var grid = new Grid();
        grid.Children.Add(new TextBlock { Text = label, Opacity = 0.7 });
        grid.Children.Add(new TextBlock { Text = value, HorizontalAlignment = HorizontalAlignment.Right });
        return grid;
    }

    private static Border Card(UIElement child) => new()
    {
        BorderBrush = GridLineBrush,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(16),
        Child = child
    };
}
