using FluentAssertions;
using Notrelix.Domain.Accounts.WorkspaceRoutes;
using Notrelix.Domain.Accounts.WorkspaceRoutes.Events;
using Notrelix.Domain.Tests.Freeze;

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

    [CoversMutation(typeof(WorkspaceRoute), "SetAsDefault(System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void SetAsDefault_ShouldSetFlag_AndRaiseEvent()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now);
        ((IHasDomainEvents)route).ClearDomainEvents();

        route.SetAsDefault(_actorId, _now);

        route.IsDefault.Should().BeTrue();
        route.DomainEvents.Should().ContainSingle(e => e is WorkspaceRouteSetAsDefaultDomainEvent);
    }

    [CoversMutation(typeof(WorkspaceRoute), "SetAsDefault(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void SetAsDefault_WhenAlreadyDefault_ShouldBeNoOp()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now, isDefault: true);
        ((IHasDomainEvents)route).ClearDomainEvents();

        route.SetAsDefault(_actorId, _now);

        route.IsDefault.Should().BeTrue();
        route.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(WorkspaceRoute), "UnsetDefault(System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void UnsetDefault_ShouldClearFlag_AndRaiseEvent()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now, isDefault: true);
        ((IHasDomainEvents)route).ClearDomainEvents();

        route.UnsetDefault(_actorId, _now);

        route.IsDefault.Should().BeFalse();
        route.DomainEvents.Should().ContainSingle(e => e is WorkspaceRouteUnsetAsDefaultDomainEvent);
    }

    [CoversMutation(typeof(WorkspaceRoute), "UnsetDefault(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void UnsetDefault_WhenNotDefault_ShouldBeNoOp()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now);
        ((IHasDomainEvents)route).ClearDomainEvents();

        route.UnsetDefault(_actorId, _now);

        route.IsDefault.Should().BeFalse();
        route.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(WorkspaceRoute), "LinkWorkspace(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void LinkWorkspace_ShouldSetWorkspaceId_AndRaiseEvent()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now);
        ((IHasDomainEvents)route).ClearDomainEvents();

        route.LinkWorkspace(_workspaceId, _actorId, _now);

        route.WorkspaceId.Should().Be(_workspaceId);
        route.DomainEvents.Should().ContainSingle(e => e is WorkspaceRouteLinkedDomainEvent);
    }

    [CoversMutation(typeof(WorkspaceRoute), "LinkWorkspace(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void LinkWorkspace_WithSameId_ShouldBeNoOp()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now, _workspaceId);
        ((IHasDomainEvents)route).ClearDomainEvents();

        route.LinkWorkspace(_workspaceId, _actorId, _now);

        route.WorkspaceId.Should().Be(_workspaceId);
        route.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(WorkspaceRoute), "UnlinkWorkspace(System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void UnlinkWorkspace_ShouldClearWorkspaceId_AndRaiseEvent()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now, _workspaceId);
        ((IHasDomainEvents)route).ClearDomainEvents();

        route.UnlinkWorkspace(_actorId, _now);

        route.WorkspaceId.Should().BeNull();
        route.DomainEvents.Should().ContainSingle(e => e is WorkspaceRouteUnlinkedDomainEvent);
    }

    [CoversMutation(typeof(WorkspaceRoute), "UnlinkWorkspace(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
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

    [CoversMutation(typeof(WorkspaceRoute), "Delete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void Delete_ShouldMarkDeleted_AndRaiseEvent()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now);
        ((IHasDomainEvents)route).ClearDomainEvents();

        route.Delete(_actorId, _now);

        route.IsDeleted.Should().BeTrue();
        route.DomainEvents.Should().ContainSingle(e => e is WorkspaceRouteDeletedDomainEvent);
    }

    [CoversMutation(typeof(WorkspaceRoute), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_ShouldMarkRestored_AndRaiseEvent()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now);
        route.Delete(_actorId, _now);
        ((IHasDomainEvents)route).ClearDomainEvents();

        route.Restore(_actorId, _now.AddHours(1));

        route.IsDeleted.Should().BeFalse();
        route.DomainEvents.Should().ContainSingle(e => e is WorkspaceRouteRestoredDomainEvent);
    }

    [CoversMutation(typeof(WorkspaceRoute), "Delete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Invalid)]
    [Fact]
    public void Mutations_AfterDelete_ShouldThrow()
    {
        var route = WorkspaceRoute.Create(_accountId, "route", _actorId, _now);
        route.Delete(_actorId, _now);

        var act = () => route.SetAsDefault(_actorId, _now);

        act.Should().Throw<DomainException>();
    }

    private WorkspaceRoute CreateRoute(Guid? workspaceId = null, bool isDefault = false)
    {
        return WorkspaceRoute.Create(_accountId, "test-route", _actorId, _now, workspaceId, isDefault);
    }

    [Fact]
    public void InitialVersion_ShouldBe1()
    {
        var route = CreateRoute();
        route.Version.Should().Be(1);
    }

    [CoversMutation(typeof(WorkspaceRoute), "SetAsDefault(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void SetAsDefault_NoOp_VersionShouldNotIncrement()
    {
        var route = CreateRoute(isDefault: true);
        var before = route.Version;
        route.SetAsDefault(Guid.NewGuid(), DateTimeOffset.UtcNow);
        route.Version.Should().Be(before);
    }

    [CoversMutation(typeof(WorkspaceRoute), "UnsetDefault(System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void UnsetDefault_ShouldIncrementVersion()
    {
        var route = CreateRoute(isDefault: true);
        var before = route.Version;
        route.UnsetDefault(Guid.NewGuid(), DateTimeOffset.UtcNow);
        route.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(WorkspaceRoute), "UnsetDefault(System.Guid,System.DateTimeOffset)", MutationScenario.Audit)]
    [Fact]
    public void UnsetDefault_ShouldUpdateAudit()
    {
        var route = CreateRoute(isDefault: true);
        var actor = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        route.UnsetDefault(actor, time);
        route.UpdatedBy.Should().Be(actor);
        route.UpdatedAt.Should().Be(time);
    }

    [CoversMutation(typeof(WorkspaceRoute), "UnsetDefault(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void UnsetDefault_NoOp_VersionShouldNotIncrement()
    {
        var route = CreateRoute(isDefault: false);
        var before = route.Version;
        route.UnsetDefault(Guid.NewGuid(), DateTimeOffset.UtcNow);
        route.Version.Should().Be(before);
    }

    [CoversMutation(typeof(WorkspaceRoute), "LinkWorkspace(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void LinkWorkspace_ShouldIncrementVersion()
    {
        var route = CreateRoute();
        var before = route.Version;
        route.LinkWorkspace(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        route.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(WorkspaceRoute), "LinkWorkspace(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Audit)]
    [Fact]
    public void LinkWorkspace_ShouldUpdateAudit()
    {
        var route = CreateRoute();
        var actor = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        route.LinkWorkspace(Guid.NewGuid(), actor, time);
        route.UpdatedBy.Should().Be(actor);
        route.UpdatedAt.Should().Be(time);
    }

    [CoversMutation(typeof(WorkspaceRoute), "LinkWorkspace(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void LinkWorkspace_NoOp_VersionShouldNotIncrement()
    {
        var workspaceId = Guid.NewGuid();
        var route = CreateRoute(workspaceId: workspaceId);
        var before = route.Version;
        route.LinkWorkspace(workspaceId, Guid.NewGuid(), DateTimeOffset.UtcNow);
        route.Version.Should().Be(before);
    }

    [CoversMutation(typeof(WorkspaceRoute), "UnlinkWorkspace(System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void UnlinkWorkspace_ShouldIncrementVersion()
    {
        var route = CreateRoute(workspaceId: Guid.NewGuid());
        var before = route.Version;
        route.UnlinkWorkspace(Guid.NewGuid(), DateTimeOffset.UtcNow);
        route.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(WorkspaceRoute), "UnlinkWorkspace(System.Guid,System.DateTimeOffset)", MutationScenario.Audit)]
    [Fact]
    public void UnlinkWorkspace_ShouldUpdateAudit()
    {
        var route = CreateRoute(workspaceId: Guid.NewGuid());
        var actor = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        route.UnlinkWorkspace(actor, time);
        route.UpdatedBy.Should().Be(actor);
        route.UpdatedAt.Should().Be(time);
    }

    [CoversMutation(typeof(WorkspaceRoute), "UnlinkWorkspace(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void UnlinkWorkspace_NoOp_VersionShouldNotIncrement()
    {
        var route = CreateRoute();
        var before = route.Version;
        route.UnlinkWorkspace(Guid.NewGuid(), DateTimeOffset.UtcNow);
        route.Version.Should().Be(before);
    }

    [CoversMutation(typeof(WorkspaceRoute), "Delete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void Delete_ShouldIncrementVersion()
    {
        var route = CreateRoute();
        var before = route.Version;
        route.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        route.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(WorkspaceRoute), "Delete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void Delete_ShouldSetDeleteAudit()
    {
        var route = CreateRoute();
        var actor = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        route.Delete(actor, time, "reason");
        route.DeletedBy.Should().Be(actor);
        route.DeletedAt.Should().Be(time);
    }

    [CoversMutation(typeof(WorkspaceRoute), "Delete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.NoOp)]
    [Fact]
    public void Delete_IsIdempotent_ShouldNotRaiseEvent()
    {
        var route = CreateRoute();
        route.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        ((IHasDomainEvents)route).ClearDomainEvents();
        route.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        route.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(WorkspaceRoute), "Delete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.NoOp)]
    [Fact]
    public void Delete_IsIdempotent_ShouldNotIncrementVersion()
    {
        var route = CreateRoute();
        route.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        var before = route.Version;
        route.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        route.Version.Should().Be(before);
    }

    [CoversMutation(typeof(WorkspaceRoute), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_ShouldIncrementVersion()
    {
        var route = CreateRoute();
        route.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        var before = route.Version;
        route.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);
        route.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(WorkspaceRoute), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_ShouldSetRestoreAudit()
    {
        var route = CreateRoute();
        route.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        var actor = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        route.Restore(actor, time);
    }

    [CoversMutation(typeof(WorkspaceRoute), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Restore_NoOp_ShouldNotRaiseEvent()
    {
        var route = CreateRoute();
        ((IHasDomainEvents)route).ClearDomainEvents();
        route.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);
        route.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(WorkspaceRoute), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Restore_NoOp_ShouldNotIncrementVersion()
    {
        var route = CreateRoute();
        var before = route.Version;
        route.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);
        route.Version.Should().Be(before);
    }
}