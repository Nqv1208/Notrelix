using Notrelix.Domain.Analytics.Widgets;

namespace Notrelix.Domain.Analytics.Rules;

public static class WidgetRules
{
    public static void ValidateTitle(string title)
    {
        Guard.NotNullOrWhiteSpace(title);
    }

    public static void ValidatePosition(WidgetPosition position)
    {
        Guard.NotNull(position);
        if (position.X < 0 || position.Y < 0)
        {
            throw new BusinessRuleException(CommonRuleCodes.Common_WidgetCoordinatesMustBeNonNegative, "Widget coordinates (X, Y) must be non-negative.");
        }
        if (position.W <= 0 || position.H <= 0)
        {
            throw new BusinessRuleException(CommonRuleCodes.Common_WidgetDimensionsMustBePositive, "Widget dimensions (W, H) must be positive.");
        }
    }
}
