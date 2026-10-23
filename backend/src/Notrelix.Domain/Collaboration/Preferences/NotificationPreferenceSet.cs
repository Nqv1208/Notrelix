namespace Notrelix.Domain.Collaboration.Preferences;

/// <summary>
/// A person's notification preference matrix in one Workspace: which notification types reach them on which
/// channels (accepted 2026-10-23, owned by Collaboration). One set per person and Workspace, only for that
/// person. The set stores explicit choices only; a cell without one has its default
/// (<see cref="NotificationPreferenceDefaults"/>), so no row exists until the person changes something and a
/// later default change reaches everyone who never chose. Delivery truth stays with the notification itself:
/// a preference decides whether a channel is attempted, never whether a fact happened.
/// </summary>
public sealed class NotificationPreferenceSet : AggregateRoot, IWorkspaceScoped
{
    private readonly List<NotificationPreferenceEntry> _entries = [];

    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }

    public IReadOnlyCollection<NotificationPreferenceEntry> Entries => _entries;

    private NotificationPreferenceSet() : base() { }

    /// <summary>
    /// Creates the set with the first effective changes (version 1). Returns null, creating nothing, when every
    /// requested value already equals its default. Invalid input changes nothing.
    /// </summary>
    public static NotificationPreferenceSet? Create(
        Guid accountId, Guid workspaceId, Guid userId, IReadOnlyCollection<NotificationPreferenceChange> changes, DateTimeOffset now)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(userId);

        var set = new NotificationPreferenceSet { AccountId = accountId, WorkspaceId = workspaceId, UserId = userId };
        var effective = set.Plan(changes);
        if (effective.Count == 0)
            return null;

        set.SetAuditOnCreate(userId, now);
        set.Commit(effective, now);
        return set;
    }

    /// <summary>The effective value of a cell: the explicit choice, else the default.</summary>
    public bool IsEnabled(string type, string channel)
    {
        EnsureKnown(type, channel);
        var entry = Find(type, channel);
        return entry?.IsEnabled ?? NotificationPreferenceDefaults.IsEnabled(type, channel);
    }

    /// <summary>
    /// Applies the requested cells all-or-nothing. A request whose every value already holds is a semantic
    /// no-op: nothing changes and the version stays. Returns whether anything changed.
    /// </summary>
    public bool Apply(IReadOnlyCollection<NotificationPreferenceChange> changes, DateTimeOffset now)
    {
        var effective = Plan(changes);
        if (effective.Count == 0)
            return false;

        var audit = PrepareAuditUpdate(UserId, now);
        Commit(effective, now);
        ApplyAuditUpdate(audit);
        IncrementVersion();
        return true;
    }

    /// <summary>Validates every change and returns, per cell, the ones that differ from the current effective value.</summary>
    private List<NotificationPreferenceChange> Plan(IReadOnlyCollection<NotificationPreferenceChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        if (changes.Count == 0)
        {
            throw new BusinessRuleException(
                CollaborationRuleCodes.Collaboration_NotificationPreference_NoChanges,
                "At least one notification preference change is required.");
        }

        var requested = new Dictionary<(string, string), bool>();
        foreach (var change in changes)
        {
            EnsureKnown(change.Type, change.Channel);
            var key = (change.Type, change.Channel);
            if (requested.TryGetValue(key, out var existing) && existing != change.Enabled)
            {
                throw new BusinessRuleException(
                    CollaborationRuleCodes.Collaboration_NotificationPreference_ConflictingChange,
                    $"'{change.Type}' on '{change.Channel}' is set to two different values in one request.");
            }

            requested[key] = change.Enabled;
        }

        return requested
            .Where(pair => IsEnabled(pair.Key.Item1, pair.Key.Item2) != pair.Value)
            .Select(pair => new NotificationPreferenceChange(pair.Key.Item1, pair.Key.Item2, pair.Value))
            .ToList();
    }

    private void Commit(List<NotificationPreferenceChange> effective, DateTimeOffset now)
    {
        foreach (var change in effective)
        {
            var entry = Find(change.Type, change.Channel);
            if (entry is null)
                _entries.Add(NotificationPreferenceEntry.Create(this, change.Type, change.Channel, change.Enabled, now));
            else
                entry.Set(change.Enabled, now);
        }
    }

    private NotificationPreferenceEntry? Find(string type, string channel) =>
        _entries.FirstOrDefault(e => e.NotificationType == type && e.Channel == channel);

    private static void EnsureKnown(string type, string channel)
    {
        if (!NotificationTypes.IsKnown(type))
        {
            throw new BusinessRuleException(
                CollaborationRuleCodes.Collaboration_NotificationPreference_UnknownType,
                $"'{type}' is not a notification type.");
        }

        if (!NotificationChannels.IsKnown(channel))
        {
            throw new BusinessRuleException(
                CollaborationRuleCodes.Collaboration_NotificationPreference_UnknownChannel,
                $"'{channel}' is not a notification channel.");
        }
    }
}
