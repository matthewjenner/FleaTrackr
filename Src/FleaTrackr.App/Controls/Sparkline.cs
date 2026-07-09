using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace FleaTrackr.App.Controls;

/// <summary>
/// A minimal price-history sparkline: draws the sequence in <see cref="Points"/> as a single
/// polyline scaled to fill the control, with the last point marked. Purely presentational - no
/// axes or labels - for the Search detail pane. Renders nothing when there are fewer than two
/// points (a single price cannot show a trend).
/// </summary>
public sealed class Sparkline : Control
{
    /// <summary>The price series to plot, oldest first. Null or short series render blank.</summary>
    public static readonly StyledProperty<IReadOnlyList<int>?> PointsProperty =
        AvaloniaProperty.Register<Sparkline, IReadOnlyList<int>?>(nameof(Points));

    /// <summary>Line colour.</summary>
    public static readonly StyledProperty<IBrush> StrokeProperty =
        AvaloniaProperty.Register<Sparkline, IBrush>(nameof(Stroke), Brushes.SteelBlue);

    /// <summary>Line thickness in device-independent pixels.</summary>
    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<Sparkline, double>(nameof(StrokeThickness), 1.5);

    /// <summary>Optional fill under the line (a soft tint reads better than a bare stroke).</summary>
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<Sparkline, IBrush?>(nameof(Fill));

    static Sparkline()
    {
        // Any of these changing must trigger a repaint.
        AffectsRender<Sparkline>(PointsProperty, StrokeProperty, StrokeThicknessProperty, FillProperty);
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public IReadOnlyList<int>? Points
    {
        get => GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public IBrush Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        IReadOnlyList<int>? points = Points;
        if (points is null || points.Count < 2) return;

        Rect bounds = Bounds;
        double pad = StrokeThickness + 1;
        double width = bounds.Width - 2 * pad;
        double height = bounds.Height - 2 * pad;
        if (width <= 0 || height <= 0) return;

        int min = points[0], max = points[0];
        foreach (int p in points)
        {
            if (p < min) min = p;
            if (p > max) max = p;
        }
        double range = max - min;

        // Map index -> x across the width, value -> y (inverted, high price near the top). A flat
        // series (range 0) draws along the vertical middle.
        Point At(int i)
        {
            double x = pad + width * i / (points.Count - 1);
            double norm = range == 0 ? 0.5 : (points[i] - min) / range;
            double y = pad + height * (1 - norm);
            return new Point(x, y);
        }

        // Optional filled area under the line, closed down to the baseline.
        if (Fill is { } fill)
        {
            var area = new StreamGeometry();
            using (StreamGeometryContext ctx = area.Open())
            {
                double baseline = bounds.Height - pad;
                ctx.BeginFigure(new Point(At(0).X, baseline), isFilled: true);
                for (int i = 0; i < points.Count; i++)
                    ctx.LineTo(At(i));
                ctx.LineTo(new Point(At(points.Count - 1).X, baseline));
                ctx.EndFigure(isClosed: true);
            }
            context.DrawGeometry(fill, null, area);
        }

        var geometry = new StreamGeometry();
        using (StreamGeometryContext ctx = geometry.Open())
        {
            ctx.BeginFigure(At(0), isFilled: false);
            for (int i = 1; i < points.Count; i++)
                ctx.LineTo(At(i));
            ctx.EndFigure(isClosed: false);
        }

        var pen = new Pen(Stroke, StrokeThickness, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        context.DrawGeometry(null, pen, geometry);

        // Mark the latest price.
        Point last = At(points.Count - 1);
        double r = StrokeThickness + 1;
        context.DrawEllipse(Stroke, null, last, r, r);
    }
}
