using Notrelix.Domain.Common;

namespace Notrelix.Domain.Analytics.Widgets;

public sealed class WidgetPosition : ValueObject
{
    public int X { get; }
    public int Y { get; }
    public int W { get; }
    public int H { get; }

    private WidgetPosition(int x, int y, int w, int h)
    {
        X = x;
        Y = y;
        W = w;
        H = h;
    }

    public static WidgetPosition Create(int x, int y, int w, int h)
    {
        return new WidgetPosition(x, y, w, h);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
        yield return W;
        yield return H;
    }
}
