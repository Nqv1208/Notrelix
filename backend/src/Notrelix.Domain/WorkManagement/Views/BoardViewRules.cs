namespace Notrelix.Domain.WorkManagement.Views;

public static class BoardViewRules
{
    public static void EnsureCanDeleteView(bool isDefault, int availableViewCount)
    {
        if (isDefault && availableViewCount <= 1)
        {
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_View_CannotDeleteDefault, "Cannot delete the default view. Set another view as default first.");
        }
    }
}
