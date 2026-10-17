using Notrelix.Domain.Workspaces.Navigation;

namespace Notrelix.Domain.Workspaces.Rules;

/// <summary>
/// Rules for a Workspace Member's personal navigation state (WX-DEC-011).
/// </summary>
public static class WorkspaceNavigationRules
{
    /// <summary>Recents keep the 30 most recent entries per member per Workspace.</summary>
    public const int RecentRetentionLimit = 30;

    /// <summary>Maximum personal pins per member per Workspace.</summary>
    public const int PinLimit = 50;

    public static void EnsureSupportedKind(string? resourceKind)
    {
        if (!NavigationResourceKinds.IsSupported(resourceKind))
        {
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Navigation_UnsupportedResourceKind,
                $"Resource kind '{resourceKind}' cannot be kept in workspace navigation.");
        }
    }

    public static void EnsureValidTimestamp(DateTimeOffset occurredAt)
    {
        if (occurredAt == default || occurredAt == DateTimeOffset.MinValue)
        {
            throw new BusinessRuleException(
                CommonRuleCodes.Common_Audit_InvalidTimestamp,
                "Navigation timestamp must be a valid date.");
        }
    }

    /// <summary>
    /// <paramref name="currentPinCount"/> is the member's pin count in the Workspace
    /// before this pin, read by the caller under the member's navigation lock.
    /// </summary>
    public static void EnsureCanPin(int currentPinCount)
    {
        if (currentPinCount >= PinLimit)
        {
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Navigation_PinLimitReached,
                $"A member can pin at most {PinLimit} resources in a workspace.");
        }
    }

    /// <summary>
    /// Returns the entries that fall outside the retention window: everything after
    /// the newest <see cref="RecentRetentionLimit"/> by <c>LastOpenedAt</c>
    /// (ties broken by id so the selection is deterministic).
    /// </summary>
    public static IReadOnlyList<RecentResource> SelectRecentsToEvict(IEnumerable<RecentResource> entries)
    {
        Guard.NotNull(entries);

        return entries
            .OrderByDescending(e => e.LastOpenedAt)
            .ThenByDescending(e => e.Id)
            .Skip(RecentRetentionLimit)
            .ToList();
    }
}
