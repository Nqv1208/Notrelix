namespace Notrelix.Domain.WorkManagement.Views;

public static class BoardViewPreferenceRules
{
    public static void EnsureValidFilterRules(IReadOnlyCollection<FilterRule> rules)
    {
        if (rules.GroupBy(r => r.FieldId).Any(g => g.Count() > 1))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_View_DuplicateFilterRules, "Duplicate filter rules for the same field are not allowed.");
    }

    public static void EnsureValidSortRules(IReadOnlyCollection<SortRule> rules)
    {
        if (rules.GroupBy(r => r.FieldId).Any(g => g.Count() > 1))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_View_DuplicateSortRules, "Duplicate sort rules for the same field are not allowed.");
    }

    public static void EnsureValidGroupRule(GroupRule rule)
    {
        Guard.NotNull(rule);
    }
}
