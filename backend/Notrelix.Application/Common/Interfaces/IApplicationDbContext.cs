using Microsoft.EntityFrameworkCore;
using Notrelix.Domain.Entities;

namespace Notrelix.Application.Common.Interfaces;

// Interface cho DbContext - Application layer tương tác với DB thông qua interface này
public interface IApplicationDbContext
{
    // Identity
    DbSet<User> Users { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<Session> Sessions { get; }

    // Workspace
    DbSet<Workspace> Workspaces { get; }
    DbSet<WorkspaceMember> WorkspaceMembers { get; }
    DbSet<WorkspaceInvitation> WorkspaceInvitations { get; }

    // Content
    DbSet<Page> Pages { get; }
    DbSet<Block> Blocks { get; }
    DbSet<Board> Boards { get; }
    DbSet<BoardList> BoardLists { get; }
    DbSet<Card> Cards { get; }
    DbSet<Checklist> Checklists { get; }
    DbSet<ChecklistItem> ChecklistItems { get; }

    // Collaboration
    DbSet<Permission> Permissions { get; }
    DbSet<Comment> Comments { get; }
    DbSet<Attachment> Attachments { get; }
    DbSet<Reaction> Reactions { get; }
    DbSet<Notification> Notifications { get; }

    // Activity
    DbSet<ActivityLog> ActivityLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
