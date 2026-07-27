namespace Notrelix.Domain.Workspaces.Rules;

public static class WorkspaceInvitationRules
{
    public static void EnsureNotDuplicate(string email, IEnumerable<string> existingPendingEmails)
    {
        if (existingPendingEmails.Contains(email, StringComparer.OrdinalIgnoreCase))
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Invitation_PendingAlreadyExists, "A pending invitation already exists for this email address.");
    }
}
