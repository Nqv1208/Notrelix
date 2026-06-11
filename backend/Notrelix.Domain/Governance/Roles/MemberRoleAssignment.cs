using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Roles;

public class MemberRoleAssignment : Entity
{
    public Guid WorkspaceId { get; private set; }
    public Guid MemberId { get; private set; }
    public Guid CustomRoleId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }

    private MemberRoleAssignment() : base() { }

    public static MemberRoleAssignment Create(Guid workspaceId, Guid memberId, Guid customRoleId, DateTimeOffset assignedAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(memberId);
        Guard.NotEmpty(customRoleId);

        return new MemberRoleAssignment
        {
            WorkspaceId = workspaceId,
            MemberId = memberId,
            CustomRoleId = customRoleId,
            AssignedAt = assignedAt
        };
    }
}
