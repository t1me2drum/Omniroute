using System;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace Omniroute.Views;

/// <summary>
/// Кільце заряду батареї (як SocRing в Android-версії). Коли станція заряджається від мережі,
/// по заповненій дузі біжить світлий відрізок, а над відсотками пульсує блискавка; на 100% лишається лише блискавка.
/// </summary>
public sealed class SocRing : Grid
{
    private const double StartAngle = 135;
    private const double TotalSweep = 270;
    private const double HighlightLength = 40;
    private static readonly TimeSpan SweepPeriod = TimeSpan.FromMilliseconds(1800);

    private readonly Path _track = new();
    private readonly Path _fill = new();
    private readonly Path _highlight = new();
    private readonly FontIcon _bolt = new() { Glyph = "", HorizontalAlignment = HorizontalAlignment.Center };
    private readonly TextBlock _percent = new() { FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center };
    private readonly TextBlock _offline = new() { Text = "офлайн", HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.6 };
    private readonly Storyboard _pulse = new();
    private readonly DispatcherQueueTimer _timer;
    private DateTime _sweepStart = DateTime.Now;
    private bool _pulsing;

    public static readonly DependencyProperty SocProperty = DependencyProperty.Register(
        nameof(Soc), typeof(object), typeof(SocRing), new PropertyMetadata(null, (d, _) => ((SocRing)d).Redraw()));

    public static readonly DependencyProperty IsOnlineProperty = DependencyProperty.Register(
        nameof(IsOnline), typeof(bool), typeof(SocRing), new PropertyMetadata(false, (d, _) => ((SocRing)d).Redraw()));

    public static readonly DependencyProperty IsChargingProperty = DependencyProperty.Register(
        nameof(IsCharging), typeof(bool), typeof(SocRing), new PropertyMetadata(false, (d, _) => ((SocRing)d).Redraw()));

    public static readonly DependencyProperty RingSizeProperty = DependencyProperty.Register(
        nameof(RingSize), typeof(double), typeof(SocRing), new PropertyMetadata(56.0, (d, _) => ((SocRing)d).Redraw()));

    public static readonly DependencyProperty StrokeWidthProperty = DependencyProperty.Register(
        nameof(StrokeWidth), typeof(double), typeof(SocRing), new PropertyMetadata(6.0, (d, _) => ((SocRing)d).Redraw()));

    /// <summary>Заряд 0–100 або null (int? не можна оголосити типом DependencyProperty)</summary>
    public object? Soc
    {
        get => GetValue(SocProperty);
        set => SetValue(SocProperty, value);
    }

    public bool IsOnline
    {
        get => (bool)GetValue(IsOnlineProperty);
        set => SetValue(IsOnlineProperty, value);
    }

    public bool IsCharging
    {
        get => (bool)GetValue(IsChargingProperty);
        set => SetValue(IsChargingProperty, value);
    }

    public double RingSize
    {
        get => (double)GetValue(RingSizeProperty);
        set => SetValue(RingSizeProperty, value);
    }

    public double StrokeWidth
    {
        get => (double)GetValue(StrokeWidthProperty);
        set => SetValue(StrokeWidthProperty, value);
    }

    public SocRing()
    {
        _track.Stroke = new SolidColorBrush(ColorHelper.FromArgb(0x40, 0x80, 0x80, 0x80));
        _highlight.Stroke = new SolidColorBrush(ColorHelper.FromArgb(0x8C, 0xFF, 0xFF, 0xFF));
        foreach (var path in new[] { _track, _fill, _highlight })
        {
            path.StrokeStartLineCap = PenLineCap.Round;
            path.StrokeEndLineCap = PenLineCap.Round;
            Children.Add(path);
        }

        var center = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        center.Children.Add(_bolt);
        center.Children.Add(_percent);
        center.Children.Add(_offline);
        Children.Add(center);

        var fade = new DoubleAnimation
        {
            From = 0.35,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(900),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTarget(fade, _bolt);
        Storyboard.SetTargetProperty(fade, "Opacity");
        _pulse.Children.Add(fade);

        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(33);
        _timer.Tick += (_, _) => DrawHighlight();

        Loaded += (_, _) => Redraw();
        Unloaded += (_, _) => StopAnimation();
        Redraw();
    }

    private int? SocValue => Soc switch
    {
        int i => i,
        double d => (int)Math.Round(d),
        _ => null
    };

    private bool Animate => IsCharging && IsOnline && SocValue.HasValue;

    private void Redraw()
    {
        var size = RingSize;
        var stroke = StrokeWidth;
        Width = size;
        Height = size;

        var soc = SocValue;
        var color = !IsOnline
            ? ColorHelper.FromArgb(0xFF, 0x80, 0x80, 0x80)
            : (soc ?? 0) <= 20 ? ColorHelper.FromArgb(0xFF, 0xD1, 0x34, 0x38) : ColorHelper.FromArgb(0xFF, 0x0E, 0x9F, 0x82);
        var brush = new SolidColorBrush(color);

        _track.StrokeThickness = _fill.StrokeThickness = _highlight.StrokeThickness = stroke;
        _track.Data = Arc(size, stroke, 0, TotalSweep);
        _fill.Stroke = brush;
        _fill.Data = soc.HasValue ? Arc(size, stroke, 0, TotalSweep * Math.Clamp(soc.Value, 0, 100) / 100) : null;

        _percent.Text = soc.HasValue ? $"{soc}%" : "—";
        _percent.FontSize = Math.Max(size / 4.2, 10);
        _offline.Visibility = IsOnline ? Visibility.Collapsed : Visibility.Visible;
        _offline.FontSize = Math.Max(size / 12, 9);

        _bolt.Foreground = brush;
        _bolt.FontSize = size * 0.2;
        _bolt.Visibility = Animate ? Visibility.Visible : Visibility.Collapsed;

        if (Animate && IsLoaded)
            StartAnimation(soc!.Value);
        else
            StopAnimation();
    }

    private void StartAnimation(int soc)
    {
        if (soc < 100)
        {
            if (!_timer.IsRunning)
            {
                _sweepStart = DateTime.Now;
                _timer.Start();
            }
            if (!_pulsing)
            {
                _pulsing = true;
                _pulse.Begin();
            }
        }
        else
        {
            // На 100% блискавка світиться рівно, без руху
            _timer.Stop();
            _highlight.Data = null;
            _pulse.Stop();
            _pulsing = false;
            _bolt.Opacity = 1;
        }
    }

    private void StopAnimation()
    {
        _timer.Stop();
        _pulse.Stop();
        _pulsing = false;
        _highlight.Data = null;
    }

    /// <summary>
    /// Короткий світлий відрізок біжить від порожнього кінця до поточного рівня
    /// </summary>
    private void DrawHighlight()
    {
        var soc = SocValue;
        if (!Animate || soc is null or >= 100)
        {
            StopAnimation();
            return;
        }

        var filled = TotalSweep * Math.Clamp(soc.Value, 0, 100) / 100;
        var len = Math.Min(HighlightLength, filled);
        var t = (DateTime.Now - _sweepStart).TotalMilliseconds % SweepPeriod.TotalMilliseconds / SweepPeriod.TotalMilliseconds;
        var head = (filled + len) * t;
        var from = Math.Max(head - len, 0);
        var to = Math.Min(head, filled);
        _highlight.Data = to > from ? Arc(RingSize, StrokeWidth, from, to - from) : null;
    }

    /// <summary>
    /// Дуга від StartAngle+offset довжиною sweep градусів (за годинниковою стрілкою)
    /// </summary>
    private static Geometry? Arc(double size, double stroke, double offset, double sweep)
    {
        if (sweep <= 0.01)
            return null;

        var r = (size - stroke) / 2;
        var c = size / 2;
        Point At(double deg)
        {
            var rad = deg * Math.PI / 180;
            return new Point(c + r * Math.Cos(rad), c + r * Math.Sin(rad));
        }

        var figure = new PathFigure { StartPoint = At(StartAngle + offset), IsClosed = false, IsFilled = false };
        figure.Segments.Add(new ArcSegment
        {
            Point = At(StartAngle + offset + sweep),
            Size = new Size(r, r),
            IsLargeArc = sweep > 180,
            SweepDirection = SweepDirection.Clockwise
        });
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }
}
