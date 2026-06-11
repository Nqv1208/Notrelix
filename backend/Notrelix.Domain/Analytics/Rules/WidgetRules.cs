using Notrelix.Domain.Common;

namespace Notrelix.Domain.Analytics.Rules;

public static class WidgetRules
{
    public static void ValidateTitle(string title)
    {
        Guard.NotNullOrWhiteSpace(title);
    }
}
