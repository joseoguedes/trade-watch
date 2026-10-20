using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Tradewatch.Controls
{
    // A piece of an exchange's trading day, as fractions (0..1) of the viewer's local day.
    // Extended = pre-market / after-hours.
    public readonly record struct TimelineSegment(double Start, double End, bool Extended);

    // Thin 24-hour bar: trading segments, 6-hour ticks and a "now" line.
    // Drawn directly in OnRender so a grid full of them stays cheap.
    public class TimelineBar : FrameworkElement
    {
        private static readonly Brush OpenBrush = Freeze(new SolidColorBrush(Colors.LimeGreen));
        private static readonly Brush ExtendedBrush = Freeze(new SolidColorBrush(Color.FromArgb(0xB0, 0x64, 0x95, 0xED)));

        public static readonly DependencyProperty SegmentsProperty = DependencyProperty.Register(
            nameof(Segments), typeof(IReadOnlyList<TimelineSegment>), typeof(TimelineBar),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty NowFractionProperty = DependencyProperty.Register(
            nameof(NowFraction), typeof(double), typeof(TimelineBar),
            new FrameworkPropertyMetadata(-1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
            nameof(TrackBrush), typeof(Brush), typeof(TimelineBar),
            new FrameworkPropertyMetadata(Brushes.DimGray, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TickBrushProperty = DependencyProperty.Register(
            nameof(TickBrush), typeof(Brush), typeof(TimelineBar),
            new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty NowBrushProperty = DependencyProperty.Register(
            nameof(NowBrush), typeof(Brush), typeof(TimelineBar),
            new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

        public IReadOnlyList<TimelineSegment>? Segments
        {
            get => (IReadOnlyList<TimelineSegment>?)GetValue(SegmentsProperty);
            set => SetValue(SegmentsProperty, value);
        }
        public double NowFraction
        {
            get => (double)GetValue(NowFractionProperty);
            set => SetValue(NowFractionProperty, value);
        }
        public Brush TrackBrush { get => (Brush)GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }
        public Brush TickBrush { get => (Brush)GetValue(TickBrushProperty); set => SetValue(TickBrushProperty, value); }
        public Brush NowBrush { get => (Brush)GetValue(NowBrushProperty); set => SetValue(NowBrushProperty, value); }

        protected override Size MeasureOverride(Size availableSize) => new Size(0, 10);

        protected override void OnRender(DrawingContext dc)
        {
            double w = ActualWidth, h = ActualHeight;
            if (w <= 0 || h <= 0) return;
            double barH = Math.Min(10, h), y = (h - barH) / 2;

            dc.DrawRoundedRectangle(TrackBrush, null, new Rect(0, y, w, barH), 2, 2);

            if (Segments != null)
                foreach (var s in Segments)
                {
                    var brush = s.Extended ? ExtendedBrush : OpenBrush;
                    double sy = s.Extended ? y + barH * 0.25 : y, sh = s.Extended ? barH * 0.5 : barH;
                    dc.DrawRectangle(brush, null, new Rect(s.Start * w, sy, Math.Max(1, (s.End - s.Start) * w), sh));
                }

            var tickPen = new Pen(TickBrush, 1);
            for (int i = 1; i < 4; i++)
            {
                double x = Math.Round(w * i / 4) + 0.5;
                dc.DrawLine(tickPen, new Point(x, y), new Point(x, y + barH));
            }

            if (NowFraction >= 0 && NowFraction <= 1)
            {
                double x = Math.Round(NowFraction * w) + 0.5;
                dc.DrawLine(new Pen(NowBrush, 2), new Point(x, y - 2), new Point(x, y + barH + 2));
            }
        }

        private static Brush Freeze(Brush b) { b.Freeze(); return b; }
    }
}
