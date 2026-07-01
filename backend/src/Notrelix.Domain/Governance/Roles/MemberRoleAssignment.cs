namespace Notrelix.Domain.Governance.Roles;

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
}
