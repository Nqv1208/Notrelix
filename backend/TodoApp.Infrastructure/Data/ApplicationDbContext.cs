using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Common.Interfaces;
using TodoApp.Domain.Common;
using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Data;

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

    // Workspace
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<WorkspaceInvitation> WorkspaceInvitations => Set<WorkspaceInvitation>();

    // Content
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<BoardMember> BoardMembers => Set<BoardMember>();
    public DbSet<BoardList> BoardLists => Set<BoardList>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<CardMember> CardMembers => Set<CardMember>();
    public DbSet<CardLabel> CardLabels => Set<CardLabel>();
    public DbSet<Checklist> Checklists => Set<Checklist>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();

    // Collaboration
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // Activity
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
