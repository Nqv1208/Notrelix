namespace Notrelix.Domain.Governance.Roles;

/// <summary>
/// The one custom role a person holds in one workspace. <see cref="MemberId"/> is the person's user id.
/// The role replaces the person's built-in workspace role while it is assigned; the stored built-in role is
/// untouched, so removing the assignment returns the person to it.
/// </summary>
public class MemberRoleAssignment : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid MemberId { get; private set; }
    public Guid CustomRoleId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }

    private MemberRoleAssignment() : base() { }

    public static MemberRoleAssignment Create(Guid accountId, Guid workspaceId, Guid memberId, Guid customRoleId, DateTimeOffset assignedAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(memberId);
        Guard.NotEmpty(customRoleId);
        Guard.NotEmpty(accountId);

        return new MemberRoleAssignment
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            MemberId = memberId,
            CustomRoleId = customRoleId,
            AssignedAt = assignedAt
        };
    }

    /// <summary>Points the assignment at another role (the person keeps exactly one role per workspace).</summary>
    public void MoveTo(Guid customRoleId, DateTimeOffset at)
    {
        Guard.NotEmpty(customRoleId);
        CustomRoleId = customRoleId;
        AssignedAt = at;
    }
}
