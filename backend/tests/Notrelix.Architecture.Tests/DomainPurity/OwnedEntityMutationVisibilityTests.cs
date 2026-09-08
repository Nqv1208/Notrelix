using System.Reflection;
using Notrelix.Domain.Common;

namespace Notrelix.Architecture.Tests;

public class OwnedEntityMutationVisibilityTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;

    private static readonly HashSet<string> PublicMutationExceptions =
    [
        // ── Billing ──
        "Notrelix.Domain.Billing.Subscriptions.SubscriptionItem",
        "Notrelix.Domain.Billing.Plans.PlanPrice",
        "Notrelix.Domain.Billing.Plans.PlanLimit",

        // ── Analytics ──
        "Notrelix.Domain.Analytics.Dashboards.DashboardWidget",
        "Notrelix.Domain.Analytics.Placements.WorkspaceWorkItemPlacementProjection",

        // ── Automation ──
        "Notrelix.Domain.Automation.Executions.AutomationExecutionStep",

        // ── Integration ──
        "Notrelix.Domain.Integrations.Sync.IntegrationSyncCursor",
        "Notrelix.Domain.Integrations.Calendar.CalendarEvent",
        "Notrelix.Domain.Integrations.Calendar.CalendarEventLink",

        // ── Governance ──
        "Notrelix.Domain.Governance.Permissions.FieldPermission",
        "Notrelix.Domain.Governance.Policies.WorkspacePolicy",

        // ── WorkManagement ──
        "Notrelix.Domain.WorkManagement.Relations.MirrorValueSnapshot",
        "Notrelix.Domain.WorkManagement.Checklists.ChecklistItem",
        "Notrelix.Domain.WorkManagement.Relations.BoardItemConnection",
        "Notrelix.Domain.WorkManagement.Boards.BoardMember",
        "Notrelix.Domain.WorkManagement.Boards.BoardSubscriber",
        "Notrelix.Domain.WorkManagement.Approvals.ApprovalStep",
        "Notrelix.Domain.WorkManagement.Forms.FormQuestion",
        "Notrelix.Domain.WorkManagement.Fields.FieldOption",
        "Notrelix.Domain.WorkManagement.Forms.FormSubmission",
        "Notrelix.Domain.WorkManagement.Items.BoardItemValue",

        // ── Collaboration ──
        "Notrelix.Domain.Collaboration.ReadStates.ResourceReadState",
        "Notrelix.Domain.Collaboration.Presence.PresenceSession",

        // ── Workspaces ──
        "Notrelix.Domain.Workspaces.Teams.TeamMember",

        // ── Accounts ──
        "Notrelix.Domain.Accounts.Regions.AccountRegion",
        "Notrelix.Domain.Accounts.Scim.ScimSyncRun",
    ];

    [Fact]
    public void OwnedEntity_ShouldNotHavePublicMutationMethods()
    {
        var violations = new List<string>();

        foreach (var type in DomainAssembly.GetTypes())
        {
            if (!IsOwnedEntity(type)) continue;

            var publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .Where(m => m.ReturnType == typeof(void))
                .Where(m => !ExcludedMethods.Contains(m.Name))
                .ToList();

            if (publicMethods.Count == 0) continue;

            if (type.FullName is not null && PublicMutationExceptions.Contains(type.FullName))
                continue;

            foreach (var method in publicMethods)
            {
                var paramStr = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                violations.Add($"{type.FullName}.{method.Name}({paramStr})");
            }
        }

        violations.Should().BeEmpty(
            $"Owned entities ({violations.Count} found) should not expose public mutation methods. " +
            "Use internal or private methods that are invoked through the aggregate root. " +
            "Add genuinely independent owned entities to PublicMutationExceptions.");
    }

    private static bool IsOwnedEntity(Type type)
    {
        if (type is { IsClass: false, IsAbstract: true }) return false;
        if (type.Namespace is null) return false;
        if (!type.Namespace.StartsWith("Notrelix.Domain", StringComparison.Ordinal)) return false;

        if (typeof(AggregateRoot).IsAssignableFrom(type)) return false;
        if (typeof(ValueObject).IsAssignableFrom(type)) return false;

        return typeof(Entity).IsAssignableFrom(type);
    }

    private static readonly HashSet<string> ExcludedMethods =
    [
        "ToString",
        "Equals",
        "GetHashCode",
        "GetType",
    ];
}
