using Microsoft.EntityFrameworkCore;
using Notrelix.Domain.Entities.Identity;
using Notrelix.Domain.Entities.Workspace;
using Notrelix.Domain.Entities.Document;
using Notrelix.Domain.Entities.Board;
using Notrelix.Domain.Entities.Calendar;
using Notrelix.Domain.Entities.Shared;

namespace Notrelix.Application.Common.Interfaces;

// Interface cho DbContext - Application layer tương tác với DB thông qua interface này
public interface IApplicationDbContext
{
    // Identity
    DbSet<User> Users { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<Session> Sessions { get; }
    DbSet<OAuthAccount> OAuthAccounts { get; }

    // Workspace
    DbSet<Workspace> Workspaces { get; }
    DbSet<WorkspaceMember> WorkspaceMembers { get; }
    DbSet<WorkspaceInvitation> WorkspaceInvitations { get; }

    // Document
    DbSet<Page> Pages { get; }
    DbSet<Block> Blocks { get; }

    // Board
    DbSet<Board> Boards { get; }
    DbSet<BoardMember> BoardMembers { get; }
    DbSet<BoardView> BoardViews { get; }
    DbSet<BoardList> BoardLists { get; }
    DbSet<Label> Labels { get; }
    DbSet<Card> Cards { get; }
    DbSet<CardMember> CardMembers { get; }
    DbSet<CardLabel> CardLabels { get; }
    DbSet<CardLink> CardLinks { get; }
    DbSet<Checklist> Checklists { get; }
    DbSet<ChecklistItem> ChecklistItems { get; }

    // Calendar
    DbSet<CalendarIntegration> CalendarIntegrations { get; }
    DbSet<CalendarEvent> CalendarEvents { get; }

    // Shared
    DbSet<Permission> Permissions { get; }
    DbSet<Comment> Comments { get; }
    DbSet<PageMention> PageMentions { get; }
    DbSet<Attachment> Attachments { get; }
    DbSet<Reaction> Reactions { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<ActivityLog> ActivityLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
