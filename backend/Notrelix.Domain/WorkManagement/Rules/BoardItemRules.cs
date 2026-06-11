using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Rules;

public static class BoardItemRules
{
    public static void ValidateTitle(string title)
    {
        Guard.NotNullOrWhiteSpace(title);
    }
}
