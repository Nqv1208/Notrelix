namespace Notrelix.Domain.Collaboration.Rules;

public static class NotificationRules
{
    public static void EnsureNotSelfNotification(Guid actorUserId, Guid targetUserId)
    {
        if (actorUserId == targetUserId)
            throw new BusinessRuleException(BusinessRuleCodes.Collaboration_Notification_CannotNotifySelf, "Cannot create a notification for the same user performing the action.");
    }
}
