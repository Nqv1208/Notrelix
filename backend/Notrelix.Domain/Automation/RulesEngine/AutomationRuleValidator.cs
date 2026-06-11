using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Automation.RulesEngine;

public static class AutomationRuleValidator
{
    public static void Validate(Rules.AutomationRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.Name))
            throw new BusinessRuleException("Automation rule name cannot be empty.");
    }
}
