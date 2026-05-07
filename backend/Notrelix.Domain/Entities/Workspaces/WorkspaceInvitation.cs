using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Entities.Workspaces;

// Entity đại diện cho lời mời tham gia workspace
public class WorkspaceInvitation : BaseEntity
{
    public Guid WorkspaceId { get; private set; }
    public Guid InvitedBy { get; private set; }
    public string Email { get; private set; } = null!;
    public WorkspaceRole Role { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Workspace Workspace { get; private set; } = null!;

    private WorkspaceInvitation() : base() { }

    public static WorkspaceInvitation Create(
        Guid workspaceId,
        Guid invitedBy,
        string email,
        WorkspaceRole role = WorkspaceRole.Member,
        TimeSpan? expiration = null)
    {
        return new WorkspaceInvitation
        {
            WorkspaceId = workspaceId,
            InvitedBy = invitedBy,
            Email = email.Trim().ToLowerInvariant(),
            Role = role,
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromDays(7)),
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsExpired => ExpiresAt < DateTime.UtcNow;
    public bool IsAccepted => AcceptedAt.HasValue;

    public void Accept()
    {
        if (IsExpired)
            throw new InvalidOperationException("Lời mời đã hết hạn");

        if (IsAccepted)
            throw new InvalidOperationException("Lời mời đã được chấp nhận");

        AcceptedAt = DateTime.UtcNow;
    }
}
