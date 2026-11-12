using Notrelix.Domain.Chat.Permissions;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Audit.Services;

/// <summary>
/// Chat's one place that reports an administrative action to the enterprise audit trail (UC-CHT-H04, CHT-024). It writes through the
/// Governance audit contract (<see cref="IAuditService"/>), in the request transaction of the command that calls it, so the fact and
/// the action are stored together or not at all. A fact names the actor, the target, the Workspace and, where it applies, the reason
/// and the old and new state; it never carries message content. Chat does not own the audit record and never reads it.
/// </summary>
public sealed class ChatAuditFacts(IAuditService audit, ICurrentRequestContext requestContext)
{
    public const string ChannelKind = "chat.channel";
    public const string MessageKind = "chat.message";

    public const string ChannelCreated = "ChatChannelCreated";
    public const string ChannelArchived = "ChatChannelArchived";
    public const string ChannelUnarchived = "ChatChannelUnarchived";
    public const string ChannelDeleted = "ChatChannelDeleted";
    public const string MemberAdded = "ChatMemberAdded";
    public const string MemberRemoved = "ChatMemberRemoved";
    public const string PermissionsSet = "ChatPermissionsSet";
    public const string PermissionsCleared = "ChatPermissionsCleared";
    public const string MessageModerated = "ChatMessageModerated";

    public Task ChannelCreatedAsync(Space space, CancellationToken ct) =>
        RecordAsync(ChannelCreated, ChannelTarget(space), space.WorkspaceId, new AuditChange(null, null, null, new { kind = space.Kind.ToWire(), name = space.Name }), ct);

    public Task ChannelArchivedAsync(Space space, bool archived, CancellationToken ct) =>
        RecordAsync(archived ? ChannelArchived : ChannelUnarchived, ChannelTarget(space), space.WorkspaceId, null, ct);

    public Task ChannelDeletedAsync(Space space, CancellationToken ct) =>
        RecordAsync(ChannelDeleted, ChannelTarget(space), space.WorkspaceId, null, ct);

    public Task MemberAddedAsync(Space space, Guid userId, CancellationToken ct) =>
        RecordAsync(MemberAdded, ChannelTarget(space), space.WorkspaceId, new AuditChange("User", userId, null, null), ct);

    public Task MemberRemovedAsync(Space space, Guid userId, CancellationToken ct) =>
        RecordAsync(MemberRemoved, ChannelTarget(space), space.WorkspaceId, new AuditChange("User", userId, null, null), ct);

    /// <summary>The overrides after a set or a clear, with the set before, as the old and new state (roles, actions and effects only).</summary>
    public Task PermissionsChangedAsync(
        Space space, IReadOnlyList<ChannelPermissionEntry> before, IReadOnlyList<ChannelPermissionEntry> after, CancellationToken ct) =>
        RecordAsync(
            after.Count == 0 ? PermissionsCleared : PermissionsSet,
            ChannelTarget(space),
            space.WorkspaceId,
            new AuditChange(null, null, Describe(before), Describe(after)),
            ct);

    /// <summary>
    /// A moderator removed another person's message. The fact names the moderator (the actor), the message and the channel, the author and
    /// the reason. The message text is never part of it.
    /// </summary>
    public Task MessageModeratedAsync(Space space, Guid messageId, Guid authorId, string reason, CancellationToken ct) =>
        RecordAsync(
            MessageModerated,
            ResourceRef.Create(ResourceKind.Create(MessageKind), messageId, space.WorkspaceId),
            space.WorkspaceId,
            new AuditChange("User", authorId, null, new { channelId = space.Id, reason }),
            ct);

    private static ResourceRef ChannelTarget(Space space) =>
        ResourceRef.Create(ResourceKind.Create(ChannelKind), space.Id, space.WorkspaceId);

    private static object[] Describe(IEnumerable<ChannelPermissionEntry> entries) =>
        entries.Select(e => (object)new { subject = e.Subject.ToWire(), action = e.Action.ToWire(), effect = e.Effect.ToWire() }).ToArray();

    private Task RecordAsync(string action, ResourceRef target, Guid workspaceId, AuditChange? change, CancellationToken ct) =>
        audit.RecordAsync(
            workspaceId,
            requestContext.UserId,
            action,
            target,
            AuditMetadata.Create(),
            AuditSeverity.Info,
            cancellationToken: ct,
            change: change);
}
