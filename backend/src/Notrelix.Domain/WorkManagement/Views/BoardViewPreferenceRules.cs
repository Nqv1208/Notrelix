using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.WorkManagement.Views;

public static class BoardViewPreferenceRules
{
    public static void EnsureValidFilterRules(IReadOnlyCollection<FilterRule> rules)
    {
        if (rules.GroupBy(r => r.FieldId).Any(g => g.Count() > 1))
            throw new BusinessRuleException("Duplicate filter rules for the same field are not allowed.");
    }

    public static void EnsureValidSortRules(IReadOnlyCollection<SortRule> rules)
    {
        if (rules.GroupBy(r => r.FieldId).Any(g => g.Count() > 1))
            throw new BusinessRuleException("Duplicate sort rules for the same field are not allowed.");
    }

    public static void EnsureValidGroupRule(GroupRule rule)
    {
        Guard.NotNull(rule);
    }
}
