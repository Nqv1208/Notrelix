using Notrelix.Domain.Governance.Permissions;
using Notrelix.Application.Features.Automation.Executions.Queries.GetAutomationExecutions;
using Notrelix.Application.Features.Automation.Rules.Commands.CreateAutomationRule;
using Notrelix.Application.Features.Automation.Rules.Commands.SetAutomationRuleEnabled;
using Notrelix.Application.Features.Automation.Rules.Queries.GetWorkspaceAutomations;

namespace Notrelix.Application.Tests.Features.Automation.Rules;

/// <summary>AUT-020: the pipeline-facing permission contract of the Automation use cases.</summary>
public class AutomationPermissionMarkerTests
{
    private static readonly Guid Workspace = Guid.NewGuid();

    [Fact]
    public void CreateWebhookRule_RequiresManageIntegrationsInAdditionToManageAutomation()
    {
        var command = new CreateAutomationRuleCommand(Workspace, "r", "ItemCreated", "Webhook", """{"webhookPath":"h"}""");

        ((IRequirePermission)command).Action.Should().Be(PermissionAction.ManageAutomation);
        ((IRequireAdditionalPermission)command).AdditionalActionNames.Should().Equal(nameof(PermissionAction.ManageIntegrations));
    }

    [Fact]
    public void CreateNonWebhookRule_NeedsNoAdditionalPermission()
    {
        var command = new CreateAutomationRuleCommand(Workspace, "r", "ItemCreated", "MoveItem", """{"targetGroupId":"x"}""");

        ((IRequireAdditionalPermission)command).AdditionalActionNames.Should().BeEmpty();
    }

    [Fact]
    public void Enabling_RequiresManageIntegrations_ButDisablingStaysAKillSwitch()
    {
        ((IRequireAdditionalPermission)new SetAutomationRuleEnabledCommand(Guid.NewGuid(), true))
            .AdditionalActionNames.Should().Equal(nameof(PermissionAction.ManageIntegrations));
        ((IRequireAdditionalPermission)new SetAutomationRuleEnabledCommand(Guid.NewGuid(), false))
            .AdditionalActionNames.Should().BeEmpty();
    }

    [Fact]
    public void ReadingRulesAndExecutions_RequiresTheManagingPermission()
    {
        ((IRequirePermission)new GetWorkspaceAutomationsQuery(Workspace)).Action
            .Should().Be(PermissionAction.ManageAutomation);
        ((IRequirePermission)new GetAutomationExecutionsQuery(Guid.NewGuid())).Action
            .Should().Be(PermissionAction.ManageAutomation);
    }
}
