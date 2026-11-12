using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Domain.Chat.Permissions;

/// <summary>
/// The effective channel permission of one person (UC-CHT-H01): the Workspace role's default, narrowed by the channel's overrides.
/// Pure: it reads the overrides it is given and nothing else, so every handler and the space view ask the same question.
/// </summary>
/// <remarks>
/// Rules: a Workspace Owner or Admin keeps every action (no override reaches them); the person matches one subject, the channel
/// manager first, then the Guest, then the Member; a deny of that subject for the action refuses it; an allow changes nothing,
/// because an override never exceeds what the Workspace role may do and a deny always beats an allow. No overrides gives the
/// default, so a channel without overrides behaves as before.
/// </remarks>
public static class ChannelPermissionResolver
{
    /// <summary>The subject an override addresses for this person, or null for a Workspace Owner or Admin, whom no override reaches.</summary>
    public static ChannelPermissionSubject? SubjectOf(bool isWorkspaceAdmin, bool isChannelManager, bool isGuest)
    {
        if (isWorkspaceAdmin) return null;
        if (isChannelManager) return ChannelPermissionSubject.Manager;
        return isGuest ? ChannelPermissionSubject.Guest : ChannelPermissionSubject.Member;
    }

    /// <summary>Whether the channel's overrides leave the action to this person. The Workspace role's own default is decided by Governance.</summary>
    public static bool IsAllowed(
        IEnumerable<ChannelPermissionEntry> overrides,
        ChannelAction action,
        bool isWorkspaceAdmin,
        bool isChannelManager,
        bool isGuest)
    {
        var subject = SubjectOf(isWorkspaceAdmin, isChannelManager, isGuest);
        if (subject is null) return true;

        return !overrides.Any(o => o.Subject == subject && o.Action == action && o.Effect == ChannelPermissionEffect.Deny);
    }

    /// <summary>
    /// Normalizes a requested set: one entry per (subject, action), where a deny beats an allow given for the same pair, in a stable
    /// order. Refuses an override that would lock a channel manager out of inviting (422 permission-lockout), because a manager who
    /// cannot add people can no longer run the channel.
    /// </summary>
    public static IReadOnlyList<ChannelPermissionEntry> Normalize(IEnumerable<ChannelPermissionEntry> requested)
    {
        var byKey = new Dictionary<(ChannelPermissionSubject, ChannelAction), ChannelPermissionEffect>();
        foreach (var entry in requested)
        {
            var key = (entry.Subject, entry.Action);
            if (!byKey.ContainsKey(key) || entry.Effect == ChannelPermissionEffect.Deny)
                byKey[key] = entry.Effect;
        }

        var result = byKey
            .Select(kv => new ChannelPermissionEntry(kv.Key.Item1, kv.Key.Item2, kv.Value))
            .OrderBy(e => e.Subject).ThenBy(e => e.Action)
            .ToList();

        if (result.Any(e => e.Subject == ChannelPermissionSubject.Manager && e.Action == ChannelAction.Invite && e.Effect == ChannelPermissionEffect.Deny))
            throw new BusinessRuleException(ChatRuleCodes.PermissionLockout, "A channel manager cannot be denied inviting people.");

        return result;
    }
}
