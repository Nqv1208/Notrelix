using System.Reflection;
using FluentAssertions;
using Notrelix.Domain.WorkManagement.Boards;

namespace Notrelix.Domain.Tests.Common;

public class DomainEventCompletenessTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void DomainEvents_ShouldNotUseGuidEmptyActor()
    {
        var aggregateCases = new[]
        {
            (object)Board.Create(Guid.NewGuid(), Guid.NewGuid(), _actorId, "Test", null, _now)
        };

        foreach (var aggregate in aggregateCases)
        {
            if (aggregate is Entity entity)
            {
                foreach (var evt in entity.DomainEvents)
                {
                    var actorProp = evt.GetType().GetProperty("ActorUserId")
                        ?? evt.GetType().GetProperty("DeletedBy")
                        ?? evt.GetType().GetProperty("RestoredBy")
                        ?? evt.GetType().GetProperty("UpdatedBy")
                        ?? evt.GetType().GetProperty("CreatedBy");

                    if (actorProp != null && actorProp.GetValue(evt) is Guid guid)
                    {
                        guid.Should().NotBe(Guid.Empty, $"DomainEvent {evt.GetType().Name} should not have Guid.Empty actor");
                    }
                }
            }
        }
    }

    [Fact]
    public void DomainEvent_ShouldHaveNonDefaultEventId()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), _actorId, "Test", null, _now);
        foreach (var evt in board.DomainEvents)
        {
            evt.EventId.Should().NotBe(default(Guid));
        }
    }

    [Fact]
    public void DomainEvent_ShouldHaveNonDefaultOccurredAt()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), _actorId, "Test", null, _now);
        foreach (var evt in board.DomainEvents)
        {
            evt.OccurredAt.Should().NotBe(default(DateTimeOffset));
        }
    }

    [Theory]
    [InlineData("User")]
    [InlineData("UserSession")]
    [InlineData("UserMfaMethod")]
    [InlineData("UserSecuritySettings")]
    [InlineData("ApiToken")]
    [InlineData("Board")]
    [InlineData("BoardField")]
    [InlineData("BoardItem")]
    [InlineData("BoardRelation")]
    [InlineData("BoardView")]
    [InlineData("SavedFilter")]
    [InlineData("Label")]
    [InlineData("Checklist")]
    [InlineData("ApprovalRequest")]
    [InlineData("Form")]
    [InlineData("ResourceLink")]
    [InlineData("Comment")]
    [InlineData("Reaction")]
    [InlineData("Notification")]
    [InlineData("IntegrationConnection")]
    [InlineData("CalendarIntegration")]
    [InlineData("WebhookSubscription")]
    [InlineData("DashboardSource")]
    [InlineData("Subscription")]
    [InlineData("Entitlement")]
    [InlineData("WorkspaceFeatureUsage")]
    [InlineData("UsageMetric")]
    [InlineData("Invoice")]
    [InlineData("PaymentMethod")]
    [InlineData("PermissionRule")]
    [InlineData("ResourcePermission")]
    [InlineData("ShareLink")]
    [InlineData("CustomRole")]
    [InlineData("ScheduledJob")]
    [InlineData("Dashboard")]
    public void AllAggregateRoots_ShouldBeListed(string aggregateName)
    {
        var ns = "Notrelix.Domain";
        var type = Assembly.Load(ns).GetType($"{ns}.{GetTypePath(aggregateName)}.{aggregateName}")
            ?? Assembly.Load(ns).GetTypes().FirstOrDefault(t => t.Name == aggregateName && !t.IsAbstract);

        type.Should().NotBeNull($"AggregateRoot '{aggregateName}' should exist");
        var baseType = type!;
        while (baseType != null && baseType != typeof(object))
        {
            if (baseType == typeof(AggregateRoot)) return;
            baseType = baseType.BaseType!;
        }

        Assert.Fail($"'{aggregateName}' does not inherit from AggregateRoot");
    }

    private static string GetTypePath(string name) => name switch
    {
        "User" => "Identity.Users",
        "UserSession" => "Identity.Sessions",
        "UserMfaMethod" => "Identity.Mfa",
        "UserSecuritySettings" => "Identity.Security",
        "ApiToken" => "Identity.Tokens",
        "UserProfile" => "Identity.Profiles",
        "Board" => "WorkManagement.Boards",
        "BoardField" => "WorkManagement.Fields",
        "BoardItem" => "WorkManagement.Items",
        "BoardGroup" => "WorkManagement.BoardGroups",
        "BoardRelation" => "WorkManagement.Relations",
        "BoardView" => "WorkManagement.Views",
        "SavedFilter" => "WorkManagement.Views",
        "Label" => "WorkManagement.Labels",
        "Checklist" => "WorkManagement.Checklists",
        "ApprovalRequest" => "WorkManagement.Approvals",
        "Form" => "WorkManagement.Forms",
        "Comment" => "Collaboration.Comments",
        "Reaction" => "Collaboration.Reactions",
        "Notification" => "Collaboration.Notifications",
        "ResourceLink" => "Documents.ResourceLinks",
        "ShareLink" => "Governance.ShareLinks",
        "CustomRole" => "Governance.Roles",
        "PermissionRule" => "Governance.Permissions",
        "ResourcePermission" => "Governance.Permissions",
        "Plan" => "Billing.Plans",
        "Subscription" => "Billing.Subscriptions",
        "Entitlement" => "Billing.Entitlements",
        "WorkspaceFeatureUsage" => "Billing.Usage",
        "UsageMetric" => "Billing.Usage",
        "Invoice" => "Billing.Payments",
        "PaymentMethod" => "Billing.Payments",
        "IntegrationConnection" => "Integrations.Connections",
        "CalendarIntegration" => "Integrations.Calendar",
        "WebhookSubscription" => "Integrations.Webhooks",
        "WebhookDelivery" => "Integrations.Webhooks",
        "Dashboard" => "Analytics.Dashboards",
        "DashboardSource" => "Analytics.Dashboards",
        "ScheduledJob" => "Automation.Scheduled",
        "AutomationRule" => "Automation.Rules",
        "AutomationExecution" => "Automation.Executions",
        "AutomationTemplate" => "Automation.Templates",
        "AiAgent" => "Automation.Agents",
        "AiAgentRun" => "Automation.Agents",
        "Page" => "Documents.Pages",
        "Block" => "Documents.Blocks",
        "Workspace" => "Workspaces.Workspaces",
        "WorkspaceMember" => "Workspaces.Members",
        "WorkspaceInvitation" => "Workspaces.Invitations",
        "Space" => "Workspaces.Spaces",
        "Team" => "Workspaces.Teams",
        _ => throw new ArgumentException($"Unknown aggregate: {name}")
    };
}
