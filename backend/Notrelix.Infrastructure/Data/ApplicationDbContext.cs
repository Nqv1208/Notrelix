using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Identity.Profiles;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Views;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Labels;
using Notrelix.Domain.WorkManagement.Checklists;
using Notrelix.Domain.Integrations.Calendar;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Domain.Integrations.Webhooks;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Domain.Governance.Policies;
using Notrelix.Domain.Governance.Security;
using Notrelix.Domain.Governance.Audit;
using Notrelix.Domain.Governance.ShareLinks;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Collaboration.Mentions;
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Domain.Collaboration.Reactions;
using Notrelix.Domain.Collaboration.Notifications;
using Notrelix.Domain.Collaboration.Activity;

namespace Notrelix.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserSession> Sessions => Set<UserSession>();
    public DbSet<OAuthAccount> OAuthAccounts => Set<OAuthAccount>();

    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<WorkspaceInvitation> WorkspaceInvitations => Set<WorkspaceInvitation>();
    public DbSet<Space> Spaces => Set<Space>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    public DbSet<Page> Pages => Set<Page>();
    public DbSet<Block> Blocks => Set<Block>();

    public DbSet<Board> Boards => Set<Board>();
    public DbSet<BoardGroup> BoardGroups => Set<BoardGroup>();
    public DbSet<BoardField> BoardFields => Set<BoardField>();
    public DbSet<BoardView> BoardViews => Set<BoardView>();
    public DbSet<BoardMember> BoardMembers => Set<BoardMember>();
    public DbSet<BoardItem> BoardItems => Set<BoardItem>();
    public DbSet<BoardItemValue> BoardItemValues => Set<BoardItemValue>();
    public DbSet<BoardItemMember> BoardItemMembers => Set<BoardItemMember>();
    public DbSet<BoardItemLabel> BoardItemLabels => Set<BoardItemLabel>();
    public DbSet<BoardItemLink> BoardItemLinks => Set<BoardItemLink>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<Checklist> Checklists => Set<Checklist>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();

    public DbSet<CalendarIntegration> CalendarIntegrations => Set<CalendarIntegration>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();

    public DbSet<IntegrationConnection> IntegrationConnections => Set<IntegrationConnection>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<AutomationRule> AutomationRules => Set<AutomationRule>();
    public DbSet<AutomationExecution> AutomationExecutions => Set<AutomationExecution>();

    public DbSet<ResourcePermission> ResourcePermissions => Set<ResourcePermission>();
    public DbSet<CustomRole> CustomRoles => Set<CustomRole>();
    public DbSet<ShareLink> ShareLinks => Set<ShareLink>();
    public DbSet<WorkspacePolicy> WorkspacePolicies => Set<WorkspacePolicy>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Mention> PageMentions => Set<Mention>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Ignore<DomainEvent>();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
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