using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Domain.Common;
using Notrelix.Domain.Entities.Identity;
using Notrelix.Domain.Entities.Workspacess;
using Notrelix.Domain.Entities.Document;
using Notrelix.Domain.Entities.Boardss;
using Notrelix.Domain.Entities.Calendar;
using Notrelix.Domain.Entities.Shared;

namespace Notrelix.Infrastructure.Data;

// Application Database Context - Implement IApplicationDbContext
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<OAuthAccount> OAuthAccounts => Set<OAuthAccount>();

    // Workspace
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<WorkspaceInvitation> WorkspaceInvitations => Set<WorkspaceInvitation>();

    // Document
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<Block> Blocks => Set<Block>();

    // Board
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<BoardMember> BoardMembers => Set<BoardMember>();
    public DbSet<BoardView> BoardViews => Set<BoardView>();
    public DbSet<BoardList> BoardLists => Set<BoardList>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<CardMember> CardMembers => Set<CardMember>();
    public DbSet<CardLabel> CardLabels => Set<CardLabel>();
    public DbSet<CardLink> CardLinks => Set<CardLink>();
    public DbSet<Checklist> Checklists => Set<Checklist>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();

    // Calendar
    public DbSet<CalendarIntegration> CalendarIntegrations => Set<CalendarIntegration>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();

    // Shared
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<PageMention> PageMentions => Set<PageMention>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore Domain Events
        modelBuilder.Ignore<BaseEvent>();

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-update audit fields
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
