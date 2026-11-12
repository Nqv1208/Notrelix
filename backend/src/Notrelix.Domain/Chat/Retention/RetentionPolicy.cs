using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Domain.Chat.Retention;

/// <summary>
/// The retention rule of one Workspace's Chat (UC-CHT-H05, CHT-021): how many days a message is kept and what happens to it after that.
/// There is one policy per Workspace and its id is the Workspace id, so the version a client reads belongs to the Workspace's rule. A
/// Workspace that never set one has no retention: nothing is removed. Only the Workspace Owner changes it (the handler decides that).
/// </summary>
/// <remarks>
/// Invariants held here: the window is null (no retention) or between <see cref="ChatLimits.RetentionMinDays"/> and
/// <see cref="ChatLimits.RetentionMaxDays"/> whole days; the mode is a known mode. Turning retention off keeps the mode the Owner had
/// chosen, so turning it on again starts from the same choice.
/// </remarks>
public sealed class RetentionPolicy : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }

    /// <summary>The age in days after which a message expires. Null means no retention.</summary>
    public int? RetentionDays { get; private set; }

    public RetentionMode Mode { get; private set; }

    public bool IsEnabled => RetentionDays is not null;

    private RetentionPolicy() : base() { }

    private RetentionPolicy(Guid workspaceId) : base(workspaceId) { }

    /// <summary>The rule of a Workspace that has not set one: no retention, tombstone when one is set later.</summary>
    public static RetentionPolicy CreateDefault(Guid accountId, Guid workspaceId, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        var policy = new RetentionPolicy(workspaceId)
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            RetentionDays = null,
            Mode = RetentionMode.Tombstone,
        };
        policy.SetAuditOnCreate(null, createdAt);
        return policy;
    }

    /// <summary>Checks a window without changing anything: null, or whole days within the limits.</summary>
    public static bool IsValidWindow(int? days) =>
        days is null || days is >= ChatLimits.RetentionMinDays and <= ChatLimits.RetentionMaxDays;

    /// <summary>
    /// Sets the window and the mode. A null window turns retention off and keeps the stored mode. Returns false and changes nothing when
    /// the rule already is this one.
    /// </summary>
    public bool Change(int? days, RetentionMode mode, Guid changedBy, DateTimeOffset changedAt)
    {
        Guard.NotEmpty(changedBy);
        if (!IsValidWindow(days))
            throw new BusinessRuleException(
                ChatRuleCodes.RetentionInvalid,
                $"A retention window is {ChatLimits.RetentionMinDays} to {ChatLimits.RetentionMaxDays} days, or none.");
        if (!Enum.IsDefined(mode))
            throw new BusinessRuleException(ChatRuleCodes.RetentionInvalid, "The retention mode is tombstone or purge.");

        var effectiveMode = days is null ? Mode : mode;
        if (days == RetentionDays && effectiveMode == Mode) return false;

        var pending = PrepareAuditUpdate(changedBy, changedAt);
        RetentionDays = days;
        Mode = effectiveMode;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        return true;
    }

    /// <summary>
    /// The instant before which a message has expired at <paramref name="now"/>: a message created strictly before it is past the window.
    /// Null when there is no retention.
    /// </summary>
    public DateTimeOffset? CutoffAt(DateTimeOffset now) =>
        RetentionDays is { } days ? now - TimeSpan.FromDays(days) : null;
}
