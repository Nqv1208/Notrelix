using Notrelix.Application.Features.Analytics.Dashboards.Commands.CreateDashboard;
using Notrelix.Application.Features.Analytics.Dashboards.Commands.RenameDashboard;

namespace Notrelix.Application.Tests.Features.Analytics;

public class RenameDashboardCommandTests
{
    private static readonly Guid WorkspaceId = Guid.Parse("0196a1b2-0000-7000-8000-000000000002");
    private static readonly Guid DashboardId = Guid.Parse("0196a1b2-0000-7000-8000-000000000003");

    [Theory]
    [InlineData("Weekly")]
    [InlineData("  Weekly  ")]
    [InlineData("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx")]
    public void A_name_of_one_to_one_hundred_twenty_characters_is_accepted(string name)
    {
        var result = new RenameDashboardCommandValidator().Validate(new RenameDashboardCommand(WorkspaceId, DashboardId, name, 1));

        Assert.True(result.IsValid, string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_name_is_refused(string name)
    {
        Assert.False(new RenameDashboardCommandValidator().Validate(new RenameDashboardCommand(WorkspaceId, DashboardId, name, 1)).IsValid);
    }

    [Fact]
    public void A_name_over_one_hundred_twenty_characters_is_refused()
    {
        var name = new string('x', CreateDashboardCommandValidator.MaxNameLength + 1);

        Assert.False(new RenameDashboardCommandValidator().Validate(new RenameDashboardCommand(WorkspaceId, DashboardId, name, 1)).IsValid);
    }

    [Fact]
    public void A_rename_carries_the_expected_version_on_the_dashboard()
    {
        var command = new RenameDashboardCommand(WorkspaceId, DashboardId, "Weekly", 4);

        var versioned = (IExpectedVersionRequest)command;
        versioned.Resource.Kind.Value.Should().Be("analytics.dashboard");
        versioned.Resource.ResourceId.Should().Be(DashboardId);
        versioned.ExpectedVersion.Should().Be(4);
    }

    [Fact]
    public void A_version_below_one_is_refused()
    {
        Assert.False(new RenameDashboardCommandValidator().Validate(new RenameDashboardCommand(WorkspaceId, DashboardId, "Weekly", 0)).IsValid);
    }
}
