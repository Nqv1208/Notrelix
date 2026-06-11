using Microsoft.EntityFrameworkCore;
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
using Notrelix.Domain.Governance.Audit;
using Notrelix.Domain.Governance.Policies;
using Notrelix.Domain.Governance.Security;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Collaboration.Mentions;
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Domain.Collaboration.Reactions;
using Notrelix.Domain.Collaboration.Notifications;
using Notrelix.Domain.Governance.ShareLinks;
using Notrelix.Domain.Collaboration.Activity;

namespace Notrelix.Application.Common.Abstractions;

public interface IApplicationDbContext
{
    // Identity
    DbSet<User> Users { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<UserSession> Sessions { get; }
    DbSet<OAuthAccount> OAuthAccounts { get; }

    // Workspace
    DbSet<Workspace> Workspaces { get; }
    DbSet<WorkspaceMember> WorkspaceMembers { get; }
    DbSet<WorkspaceInvitation> WorkspaceInvitations { get; }
    DbSet<Space> Spaces { get; }
    DbSet<Team> Teams { get; }
    DbSet<TeamMember> TeamMembers { get; }

    // Document
    DbSet<Page> Pages { get; }
    DbSet<Block> Blocks { get; }

    // Board / WorkManagement
    DbSet<Board> Boards { get; }
    DbSet<BoardGroup> BoardGroups { get; }
    DbSet<BoardField> BoardFields { get; }
    DbSet<BoardView> BoardViews { get; }
    DbSet<BoardMember> BoardMembers { get; }
    DbSet<BoardItem> BoardItems { get; }
    DbSet<BoardItemValue> BoardItemValues { get; }
    DbSet<BoardItemMember> BoardItemMembers { get; }
    DbSet<BoardItemLabel> BoardItemLabels { get; }
    DbSet<BoardItemLink> BoardItemLinks { get; }
    DbSet<Label> Labels { get; }
    DbSet<Checklist> Checklists { get; }
    DbSet<ChecklistItem> ChecklistItems { get; }

    // Calendar
    DbSet<CalendarIntegration> CalendarIntegrations { get; }
    DbSet<Notrelix.Domain.Integrations.Calendar.CalendarEvent> CalendarEvents { get; }

    // Extensibility / Integrations / Automation
    DbSet<IntegrationConnection> IntegrationConnections { get; }
    DbSet<WebhookSubscription> WebhookSubscriptions { get; }
    DbSet<AutomationRule> AutomationRules { get; }
    DbSet<AutomationExecution> AutomationExecutions { get; }

    // Shared / Governance / Collaboration
    DbSet<ResourcePermission> ResourcePermissions { get; }
    DbSet<CustomRole> CustomRoles { get; }
    DbSet<ShareLink> ShareLinks { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Comment> Comments { get; }
    DbSet<Mention> PageMentions { get; }
    DbSet<Attachment> Attachments { get; }
    DbSet<Reaction> Reactions { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<ActivityLog> ActivityLogs { get; }
    DbSet<WorkspacePolicy> WorkspacePolicies { get; }
    DbSet<SecurityEvent> SecurityEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
