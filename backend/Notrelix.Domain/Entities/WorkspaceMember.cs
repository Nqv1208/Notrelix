using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Entities;

// Entity đại diện cho thành viên của workspace
public class WorkspaceMember : BaseEntity
{
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public MemberRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }

    // Navigation
    public Workspace Workspace { get; private set; } = null!;

    private WorkspaceMember() : base() { }

    internal static WorkspaceMember Create(Guid workspaceId, Guid userId, MemberRole role)
    {
        return new WorkspaceMember
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
    }

    internal void UpdateRole(MemberRole newRole)
    {
        Role = newRole;
    }

    public bool CanEdit => Role is MemberRole.Owner or MemberRole.Admin or MemberRole.Member;
    public bool CanAdmin => Role is MemberRole.Owner or MemberRole.Admin;
    public bool IsOwner => Role == MemberRole.Owner;
}
