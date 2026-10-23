using Notrelix.Application.EventMappers.Workspaces;
using Notrelix.Domain.Workspaces.Members.Events;

namespace Notrelix.Application.Tests.EventMappers;

/// <summary>AUT-020: Workspaces publishes the member standing facts Automation reacts to.</summary>
public class WorkspaceMemberStandingMapperTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(WorkspaceRole.Admin, WorkspaceRole.Member, true)]
    [InlineData(WorkspaceRole.Owner, WorkspaceRole.Guest, true)]
    [InlineData(WorkspaceRole.Member, WorkspaceRole.Admin, false)]
    public void RoleChanged_FactCarriesTheWorkspaceRankedDemotionFlag(WorkspaceRole from, WorkspaceRole to, bool demotion)
    {
        var userId = Guid.NewGuid();
        var domainEvent = new WorkspaceMemberRoleChangedDomainEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), userId, from, to, Guid.NewGuid(), Now);

        var fact = new WorkspaceEventMapper().Map(domainEvent)!;

        fact.UserId.Should().Be(userId);
        fact.OldRole.Should().Be(from.ToString());
        fact.NewRole.Should().Be(to.ToString());
        fact.IsDemotion.Should().Be(demotion);
        fact.WorkspaceId.Should().Be(domainEvent.WorkspaceId);
    }

    [Fact]
    public void Suspended_FactNamesTheSuspendedUserInTheWorkspaceScope()
    {
        var domainEvent = new WorkspaceMemberSuspendedDomainEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now);

        var fact = new WorkspaceEventMapper().Map(domainEvent)!;

        fact.UserId.Should().Be(domainEvent.UserId);
        fact.AccountId.Should().Be(domainEvent.AccountId);
        fact.WorkspaceId.Should().Be(domainEvent.WorkspaceId);
    }
}
