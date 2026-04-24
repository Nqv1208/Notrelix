using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Events.Workspace;

public class MemberInvitedEvent : BaseEvent
{
    public Guid WorkspaceId { get; }
    public string Email { get; }
    public WorkspaceRole Role { get; }
    public Guid InvitedBy { get; }

    public MemberInvitedEvent(Guid workspaceId, string email, WorkspaceRole role, Guid invitedBy)
    {
        WorkspaceId = workspaceId;
        Email = email;
        Role = role;
        InvitedBy = invitedBy;
    }
}
