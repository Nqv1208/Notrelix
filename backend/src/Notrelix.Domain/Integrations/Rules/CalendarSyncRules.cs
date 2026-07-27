namespace Notrelix.Domain.Integrations.Rules;

public static class CalendarSyncRules
{
    public static void EnsureConnectionActive(bool isConnectionActive)
    {
        if (!isConnectionActive)
            throw new BusinessRuleException(IntegrationRuleCodes.Integrations_Calendar_ConnectionMustBeActive, "Calendar integration connection must be active.");
    }

    public static void EnsureNoCircularSync(Guid internalEventId, Guid? externalEventId)
    {
        if (internalEventId == externalEventId && externalEventId.HasValue)
            throw new BusinessRuleException(IntegrationRuleCodes.Integrations_Calendar_CannotLinkEventToSelf, "Cannot link an event to itself.");
    }
}
