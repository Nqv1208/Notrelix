using Microsoft.Extensions.Configuration;
using Notrelix.Infrastructure.Options;

namespace Notrelix.Infrastructure.Tests.Runtime;

public class RuntimeRolesTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(pair => KeyValuePair.Create(pair.Key, pair.Value)))
            .Build();

    [Fact]
    public void AbsentKey_RunsEveryRole()
    {
        var roles = RuntimeRoles.Parse(Config());

        roles.RunsApi.Should().BeTrue();
        roles.RunsRealtime.Should().BeTrue();
        roles.RunsWorker.Should().BeTrue();
        roles.ToString().Should().Be("api,realtime,worker");
    }

    [Fact]
    public void CommaSeparatedScalar_SelectsTheNamedRoles()
    {
        var roles = RuntimeRoles.Parse(Config(("Runtime:Roles", "api, realtime")));

        roles.RunsApi.Should().BeTrue();
        roles.RunsRealtime.Should().BeTrue();
        roles.RunsWorker.Should().BeFalse();
    }

    [Fact]
    public void IndexedList_SelectsTheNamedRoles()
    {
        var roles = RuntimeRoles.Parse(Config(
            ("Runtime:Roles:0", "worker")));

        roles.RunsWorker.Should().BeTrue();
        roles.RunsApi.Should().BeFalse();
        roles.RunsRealtime.Should().BeFalse();
    }

    [Theory]
    [InlineData("API")]
    [InlineData("Realtime")]
    [InlineData(" worker ")]
    public void RoleNames_AreCaseInsensitiveAndTrimmed(string token)
    {
        var roles = RuntimeRoles.Parse(Config(("Runtime:Roles", token)));

        roles.Names.Should().ContainSingle();
    }

    [Fact]
    public void DuplicateRoles_CollapseToOneSelection()
    {
        var roles = RuntimeRoles.Parse(Config(("Runtime:Roles", "api,api")));

        roles.Names.Should().Equal("api");
    }

    [Theory]
    [InlineData("gateway")]
    [InlineData("api,queue")]
    public void UnknownRole_FailsFast(string value)
    {
        var act = () => RuntimeRoles.Parse(Config(("Runtime:Roles", value)));

        act.Should().Throw<InvalidOperationException>().WithMessage("*unknown role*Valid roles: api, realtime, worker*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("api,,worker")]
    public void EmptySelection_FailsFast(string value)
    {
        var act = () => RuntimeRoles.Parse(Config(("Runtime:Roles", value)));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Has_ReportsEachRoleIndependently()
    {
        var roles = RuntimeRoles.FromTokens(["realtime"]);

        roles.Has(RuntimeRole.Realtime).Should().BeTrue();
        roles.Has(RuntimeRole.Api).Should().BeFalse();
        roles.Has(RuntimeRole.Worker).Should().BeFalse();
    }
}
