using TodoApp.Domain.Common;

namespace TodoApp.Domain.Entities;

public class WorkspaceInvitation : BaseEntity
{
    public Guid WorkspaceId { get; private set; }
    public Guid InvitedBy { get; private set; }
    public string Email { get; private set; } = null!;
    public string Role { get; private set; } = "member";
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Workspace Workspace { get; private set; } = null!;

    private WorkspaceInvitation() : base() { }

    public static WorkspaceInvitation Create(
        Guid workspaceId,
        Guid invitedBy,
        string email,
        string role,
        string token,
        DateTime expiresAt)
    {
        return new WorkspaceInvitation
        {
            WorkspaceId = workspaceId,
            InvitedBy = invitedBy,
            Email = email.Trim().ToLowerInvariant(),
            Role = role,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAccepted()
    {
        AcceptedAt = DateTime.UtcNow;
    }
}
