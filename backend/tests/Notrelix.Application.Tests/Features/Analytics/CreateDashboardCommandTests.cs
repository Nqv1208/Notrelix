using Notrelix.Application.Features.Analytics.Dashboards.Commands.CreateDashboard;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Analytics;

public class CreateDashboardCommandTests
{
    private static readonly Guid WorkspaceId = Guid.Parse("0196a1b2-0000-7000-8000-000000000002");

    [Theory]
    [InlineData("Weekly")]
    [InlineData("  Weekly  ")]
    [InlineData("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx")]
    public void A_name_of_one_to_one_hundred_twenty_characters_is_accepted(string name)
    {
        var result = new CreateDashboardCommandValidator().Validate(new CreateDashboardCommand(WorkspaceId, name));

        Assert.True(result.IsValid, string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_name_is_refused(string name)
    {
        Assert.False(new CreateDashboardCommandValidator().Validate(new CreateDashboardCommand(WorkspaceId, name)).IsValid);
    }

    [Fact]
    public void A_name_over_one_hundred_twenty_characters_is_refused()
    {
        var name = new string('x', CreateDashboardCommandValidator.MaxNameLength + 1);

        Assert.False(new CreateDashboardCommandValidator().Validate(new CreateDashboardCommand(WorkspaceId, name)).IsValid);
    }

    [Fact]
    public void Any_workspace_member_may_create_a_dashboard_and_the_create_is_idempotent()
    {
        var command = new CreateDashboardCommand(WorkspaceId, "Weekly");

        Assert.Equal(PermissionAction.ViewWorkspace, ((IRequirePermission)command).Action);
        Assert.IsAssignableFrom<IIdempotentRequest>(command);
    }
}
