namespace Notrelix.Domain.Governance.Roles;

/// <summary>
/// Append-only audit fact of one custom-role change: a definition created, changed or archived, or a
/// person given or released from a role. It is never updated or deleted. The workspace is set only when
/// the change concerns one workspace (an assignment).
/// </summary>
public sealed class CustomRoleChange : Entity, IAccountScoped
{
    public Guid AccountId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public Guid RoleId { get; private set; }
    public string Kind { get; private set; } = null!;
    public Guid? ActorUserId { get; private set; }
    public Guid? SubjectUserId { get; private set; }
    public string Summary { get; private set; } = null!;
    public DateTimeOffset OccurredAt { get; private set; }

    private CustomRoleChange() : base() { }

    public static CustomRoleChange Record(
        Guid accountId, Guid? workspaceId, Guid roleId, string kind, Guid? actorUserId, Guid? subjectUserId,
        string summary, DateTimeOffset occurredAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(roleId);
        Guard.NotNullOrWhiteSpace(kind);

        return new CustomRoleChange
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            RoleId = roleId,
            Kind = kind,
            ActorUserId = actorUserId,
            SubjectUserId = subjectUserId,
            Summary = summary,
            OccurredAt = occurredAt,
        };
    }
}
