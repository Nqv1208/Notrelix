namespace Notrelix.Application.Common.Context;

public interface ICorrelationContext
{
    Guid CorrelationId { get; }
    Guid? CausationId { get; }

    /// <summary>
    /// AUT-020: the Automation rule acting in this scope, so facts committed on
    /// its behalf can name it. Null for ordinary user requests.
    /// </summary>
    Guid? AutomationRuleId { get; }

    void SetAutomationRule(Guid ruleId);

    void Set(Guid correlationId, Guid? causationId = null);
    void Clear();
}
