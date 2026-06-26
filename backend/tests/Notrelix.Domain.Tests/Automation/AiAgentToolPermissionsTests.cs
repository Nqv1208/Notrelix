using FluentAssertions;
using Notrelix.Domain.Automation.Agents;

namespace Notrelix.Domain.Tests.Automation;

public class AiAgentToolPermissionsTests
{
    [Fact]
    public void Create_WithDefaults_ShouldSetOnlyReadBoards()
    {
        var permissions = AiAgentToolPermissions.Create();

        permissions.CanReadBoards.Should().BeTrue();
        permissions.CanUpdateItems.Should().BeFalse();
        permissions.CanCreateItems.Should().BeFalse();
        permissions.CanSendNotifications.Should().BeFalse();
        permissions.CanAccessDocuments.Should().BeFalse();
        permissions.CanExecuteAutomations.Should().BeFalse();
        permissions.AllowedWorkflows.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithCustomPermissions_ShouldSetThem()
    {
        var permissions = AiAgentToolPermissions.Create(
            canUpdateItems: true,
            canSendNotifications: true,
            allowedWorkflows: new[] { "review", "approve" });

        permissions.CanReadBoards.Should().BeTrue();
        permissions.CanUpdateItems.Should().BeTrue();
        permissions.CanSendNotifications.Should().BeTrue();
        permissions.AllowedWorkflows.Should().BeEquivalentTo(new[] { "review", "approve" });
    }

    [Fact]
    public void FromJson_ShouldParse()
    {
        var json = "{\"canReadBoards\":true,\"canUpdateItems\":true,\"allowedWorkflows\":[\"wf1\",\"wf2\"]}";

        var permissions = AiAgentToolPermissions.FromJson(json);

        permissions.CanReadBoards.Should().BeTrue();
        permissions.CanUpdateItems.Should().BeTrue();
        permissions.AllowedWorkflows.Should().BeEquivalentTo(new[] { "wf1", "wf2" });
    }

    [Fact]
    public void FromJson_AllFalse_ShouldParse()
    {
        var permissions = AiAgentToolPermissions.FromJson("{}");

        permissions.CanReadBoards.Should().BeFalse();
        permissions.AllowedWorkflows.Should().BeEmpty();
    }

    [Fact]
    public void FromJson_WithInvalidJson_ShouldThrow()
    {
        var act = () => AiAgentToolPermissions.FromJson("{bad}");
        act.Should().Throw<BusinessRuleException>().WithMessage("*JSON*");
    }

    [Fact]
    public void ToJson_ShouldRoundTrip()
    {
        var permissions = AiAgentToolPermissions.Create(true, true, false, true, false, true, new[] { "wf1" });

        var json = permissions.ToJson();
        var parsed = AiAgentToolPermissions.FromJson(json);

        parsed.Should().Be(permissions);
    }

    [Fact]
    public void ToJson_WithEmptyWorkflows_ShouldOmitField()
    {
        var permissions = AiAgentToolPermissions.Create();

        var json = permissions.ToJson();

        json.Should().NotContain("allowedWorkflows");
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var p1 = AiAgentToolPermissions.Create(true, false, false, false, false, false);
        var p2 = AiAgentToolPermissions.Create(true, false, false, false, false, false);

        p1.Should().Be(p2);
    }
}
