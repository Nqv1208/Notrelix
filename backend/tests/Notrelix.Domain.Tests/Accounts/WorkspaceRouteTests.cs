using FluentAssertions;
using Notrelix.Domain.Accounts.WorkspaceRoutes;
using Notrelix.Domain.Accounts.WorkspaceRoutes.Events;

namespace Notrelix.Domain.Tests.Accounts;

public class WorkspaceRouteTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var route = WorkspaceRoute.Create(_accountId, "My-Route", _actorId, _now, _workspaceId, isDefault: true);

        route.AccountId.Should().Be(_accountId);
        route.RouteSlug.Should().Be("my-route");
        route.WorkspaceId.Should().Be(_workspaceId);
        route.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldNormalizeSlugToLowercase()
    {
        var route = WorkspaceRoute.Create(_accountId, "UPPERCASE", _actorId, _now);

        route.RouteSlug.Should().Be("uppercase");
    }

    [Fact]
    public void Create_ShouldSetCreationAudit()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now);

        route.CreatedBy.Should().Be(_actorId);
        route.CreatedAt.Should().Be(_now);
    }

    [Fact]
    public void Create_ShouldRaiseCreationEvent()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now, _workspaceId);

        route.DomainEvents.Should().ContainSingle(e => e is WorkspaceRouteCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithEmptyAccountId_ShouldThrow()
    {
        var act = () => WorkspaceRoute.Create(Guid.Empty, "route", _actorId, _now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptySlug_ShouldThrow()
    {
        var act = () => WorkspaceRoute.Create(_accountId, "  ", _actorId, _now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => WorkspaceRoute.Create(_accountId, "route", _actorId, _now, Guid.Empty);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*empty GUID*");
    }

    [Fact]
    public void SetAsDefault_ShouldSetFlag_AndRaiseEvent()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now);
        ((IHasDomainEvents)route).ClearDomainEvents();

        route.SetAsDefault(_actorId, _now);

        route.IsDefault.Should().BeTrue();
        route.DomainEvents.Should().ContainSingle(e => e is WorkspaceRouteSetAsDefaultDomainEvent);
    }

    [Fact]
    public void SetAsDefault_WhenAlreadyDefault_ShouldBeNoOp()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now, isDefault: true);
        ((IHasDomainEvents)route).ClearDomainEvents();

        route.SetAsDefault(_actorId, _now);

        route.IsDefault.Should().BeTrue();
        route.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UnsetDefault_ShouldClearFlag_AndRaiseEvent()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now, isDefault: true);
        ((IHasDomainEvents)route).ClearDomainEvents();

        route.UnsetDefault(_actorId, _now);

        route.IsDefault.Should().BeFalse();
        route.DomainEvents.Should().ContainSingle(e => e is WorkspaceRouteUnsetAsDefaultDomainEvent);
    }

    [Fact]
    public void UnsetDefault_WhenNotDefault_ShouldBeNoOp()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now);
        ((IHasDomainEvents)route).ClearDomainEvents();

        route.UnsetDefault(_actorId, _now);

        route.IsDefault.Should().BeFalse();
        route.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void LinkWorkspace_ShouldSetWorkspaceId_AndRaiseEvent()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now);
        ((IHasDomainEvents)route).ClearDomainEvents();

        route.LinkWorkspace(_workspaceId, _actorId, _now);

        route.WorkspaceId.Should().Be(_workspaceId);
        route.DomainEvents.Should().ContainSingle(e => e is WorkspaceRouteLinkedDomainEvent);
    }

    [Fact]
    public void LinkWorkspace_WithSameId_ShouldBeNoOp()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now, _workspaceId);
        ((IHasDomainEvents)route).ClearDomainEvents();

        route.LinkWorkspace(_workspaceId, _actorId, _now);

        route.WorkspaceId.Should().Be(_workspaceId);
        route.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UnlinkWorkspace_ShouldClearWorkspaceId_AndRaiseEvent()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now, _workspaceId);
        ((IHasDomainEvents)route).ClearDomainEvents();

        route.UnlinkWorkspace(_actorId, _now);

        route.WorkspaceId.Should().BeNull();
        route.DomainEvents.Should().ContainSingle(e => e is WorkspaceRouteUnlinkedDomainEvent);
    }

    [Fact]
    public void UnlinkWorkspace_WhenAlreadyNull_ShouldBeNoOp()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now);
        ((IHasDomainEvents)route).ClearDomainEvents();

        route.UnlinkWorkspace(_actorId, _now);

        route.WorkspaceId.Should().BeNull();
        route.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Mutations_ShouldIncrementVersion()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now);
        var versionBefore = route.Version;

        route.SetAsDefault(_actorId, _now);

        route.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void Mutations_ShouldUpdateAudit()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now);
        var later = _now.AddHours(1);

        route.SetAsDefault(_actorId, later);

        route.UpdatedBy.Should().Be(_actorId);
        route.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void SoftDelete_ShouldMarkDeleted_AndRaiseEvent()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now);
        ((IHasDomainEvents)route).ClearDomainEvents();

        route.SoftDelete(_actorId, _now);

        route.IsDeleted.Should().BeTrue();
        route.DomainEvents.Should().ContainSingle(e => e is WorkspaceRouteSoftDeletedDomainEvent);
    }

    [Fact]
    public void Restore_ShouldMarkRestored_AndRaiseEvent()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now);
        route.SoftDelete(_actorId, _now);
        ((IHasDomainEvents)route).ClearDomainEvents();

        route.Restore(_actorId, _now.AddHours(1));

        route.IsDeleted.Should().BeFalse();
        route.DomainEvents.Should().ContainSingle(e => e is WorkspaceRouteRestoredDomainEvent);
    }

    [Fact]
    public void Mutations_AfterSoftDelete_ShouldThrow()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now);
        route.SoftDelete(_actorId, _now);

        var act = () => route.SetAsDefault(_actorId, _now);

        act.Should().Throw<DomainException>();
    }
}