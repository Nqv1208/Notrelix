using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.WorkManagement.Views;

public static class BoardViewRules
{
    public static void EnsureCanDeleteView(bool isDefault, int availableViewCount)
    {
        if (isDefault && availableViewCount <= 1)
        {
            throw new BusinessRuleException("Cannot delete the default view. Set another view as default first.");
        }
    }
}
