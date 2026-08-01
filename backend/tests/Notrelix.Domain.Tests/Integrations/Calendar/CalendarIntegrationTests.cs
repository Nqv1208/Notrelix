using FluentAssertions;
using Notrelix.Domain.Integrations;
using Notrelix.Domain.Integrations.Calendar;

namespace Notrelix.Domain.Tests.Integrations.Calendar;

public class CalendarIntegrationTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid ConnectionId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldSetProperties_AndRaiseEvent()
    {
        var integration = CalendarIntegration.Create(
            AccountId, WorkspaceId, ConnectionId,
            CalendarProvider.Google, CalendarSyncDirection.Push, Actor, Now);

        integration.AccountId.Should().Be(AccountId);
        integration.WorkspaceId.Should().Be(WorkspaceId);
        integration.ConnectionId.Should().Be(ConnectionId);
        integration.Provider.Should().Be(CalendarProvider.Google);
        integration.SyncDirection.Should().Be(CalendarSyncDirection.Push);
        integration.IsActive.Should().BeTrue();
        integration.EventLinks.Should().BeEmpty();
        integration.DomainEvents.Should().ContainSingle(e => e is CalendarIntegrationConnectedDomainEvent);
    }

    [Fact]
    public void Create_WithEmptyAccountId_ShouldThrow()
    {
        var act = () => CalendarIntegration.Create(
            Guid.Empty, WorkspaceId, ConnectionId,
            CalendarProvider.Google, CalendarSyncDirection.Push, Actor, Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => CalendarIntegration.Create(
            AccountId, Guid.Empty, ConnectionId,
            CalendarProvider.Google, CalendarSyncDirection.Push, Actor, Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyConnectionId_ShouldThrow()
    {
        var act = () => CalendarIntegration.Create(
            AccountId, WorkspaceId, Guid.Empty,
            CalendarProvider.Google, CalendarSyncDirection.Push, Actor, Now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Activate_WhenInactive_ShouldSetIsActive_AndRaiseEvent()
    {
        var integration = CreateIntegration();
        integration.Deactivate(Actor, Now);
        ((IHasDomainEvents)integration).ClearDomainEvents();

        integration.Activate(Actor, Now);

        integration.IsActive.Should().BeTrue();
        integration.DomainEvents.Should().ContainSingle(e => e is CalendarIntegrationActivatedDomainEvent);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldBeNoOp()
    {
        var integration = CreateIntegration();
        ((IHasDomainEvents)integration).ClearDomainEvents();

        integration.Activate(Actor, Now);

        integration.IsActive.Should().BeTrue();
        integration.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldSetIsActiveFalse_AndRaiseEvent()
    {
        var integration = CreateIntegration();
        ((IHasDomainEvents)integration).ClearDomainEvents();

        integration.Deactivate(Actor, Now);

        integration.IsActive.Should().BeFalse();
        integration.DomainEvents.Should().ContainSingle(e => e is CalendarIntegrationDeactivatedDomainEvent);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldBeNoOp()
    {
        var integration = CreateIntegration();
        integration.Deactivate(Actor, Now);
        ((IHasDomainEvents)integration).ClearDomainEvents();

        integration.Deactivate(Actor, Now);

        integration.IsActive.Should().BeFalse();
        integration.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeSyncDirection_WhenActive_ShouldUpdate_AndRaiseEvent()
    {
        var integration = CreateIntegration(syncDirection: CalendarSyncDirection.Push);
        ((IHasDomainEvents)integration).ClearDomainEvents();

        integration.ChangeSyncDirection(CalendarSyncDirection.Pull, Actor, Now);

        integration.SyncDirection.Should().Be(CalendarSyncDirection.Pull);
        integration.DomainEvents.Should().ContainSingle(e => e is CalendarIntegrationSyncDirectionChangedDomainEvent);
    }

    [Fact]
    public void ChangeSyncDirection_WhenSame_ShouldBeNoOp()
    {
        var integration = CreateIntegration(syncDirection: CalendarSyncDirection.Both);
        ((IHasDomainEvents)integration).ClearDomainEvents();

        integration.ChangeSyncDirection(CalendarSyncDirection.Both, Actor, Now);

        integration.SyncDirection.Should().Be(CalendarSyncDirection.Both);
        integration.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeSyncDirection_WhenDeactivated_ShouldThrow()
    {
        var integration = CreateIntegration();
        integration.Deactivate(Actor, Now);

        var act = () => integration.ChangeSyncDirection(CalendarSyncDirection.Pull, Actor, Now);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*deactivated*");
    }

    [Fact]
    public void LinkEvent_WhenActive_ShouldAddLink()
    {
        var integration = CreateIntegration();
        var internalEventId = Guid.NewGuid();

        integration.LinkEvent(internalEventId, "ext-event-123", "etag-1");

        integration.EventLinks.Should().ContainSingle();
        integration.EventLinks.Single().InternalEventId.Should().Be(internalEventId);
        integration.EventLinks.Single().ExternalEventId.Should().Be("ext-event-123");
        integration.EventLinks.Single().ETag.Should().Be("etag-1");
    }

    [Fact]
    public void LinkEvent_WhenDeactivated_ShouldThrow()
    {
        var integration = CreateIntegration();
        integration.Deactivate(Actor, Now);

        var act = () => integration.LinkEvent(Guid.NewGuid(), "ext-123");

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*deactivated*");
    }

    [Fact]
    public void LinkEvent_DuplicateInternalId_ShouldThrow()
    {
        var integration = CreateIntegration();
        var internalId = Guid.NewGuid();
        integration.LinkEvent(internalId, "ext-1");

        var act = () => integration.LinkEvent(internalId, "ext-2");

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public void LinkEvent_DuplicateExternalId_ShouldThrow()
    {
        var integration = CreateIntegration();
        integration.LinkEvent(Guid.NewGuid(), "ext-1");

        var act = () => integration.LinkEvent(Guid.NewGuid(), "ext-1");

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public void UpdateEventLinkETag_ShouldUpdate()
    {
        var integration = CreateIntegration();
        var internalId = Guid.NewGuid();
        integration.LinkEvent(internalId, "ext-1", "old-etag");

        integration.UpdateEventLinkETag(internalId, "new-etag");

        integration.EventLinks.Single(l => l.InternalEventId == internalId).ETag.Should().Be("new-etag");
    }

    [Fact]
    public void UpdateEventLinkETag_LinkNotFound_ShouldThrow()
    {
        var integration = CreateIntegration();

        var act = () => integration.UpdateEventLinkETag(Guid.NewGuid(), "etag");

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*No event link found*");
    }

    [Fact]
    public void Delete_ShouldDeactivate_AndMarkDeleted()
    {
        var integration = CreateIntegration();
        ((IHasDomainEvents)integration).ClearDomainEvents();

        integration.Delete(Actor, Now);

        integration.IsActive.Should().BeFalse();
        integration.IsDeleted.Should().BeTrue();
        integration.DomainEvents.Should().Contain(e => e is CalendarIntegrationDeactivatedDomainEvent);
    }

    [Fact]
    public void Restore_ShouldRestoreState()
    {
        var integration = CreateIntegration();
        integration.Delete(Actor, Now);
        ((IHasDomainEvents)integration).ClearDomainEvents();

        integration.Restore(Actor, Now.AddHours(1));

        integration.IsDeleted.Should().BeFalse();
        integration.DomainEvents.Should().Contain(e => e is CalendarIntegrationActivatedDomainEvent);
    }

    [Fact]
    public void Mutations_ShouldIncrementVersion()
    {
        var integration = CreateIntegration();
        var versionBefore = integration.Version;

        integration.Deactivate(Actor, Now);

        integration.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void Mutations_ShouldUpdateAudit()
    {
        var integration = CreateIntegration();
        var later = Now.AddHours(1);

        integration.Deactivate(Actor, later);

        integration.UpdatedBy.Should().Be(Actor);
        integration.UpdatedAt.Should().Be(later);
    }

    private static CalendarIntegration CreateIntegration(
        CalendarSyncDirection syncDirection = CalendarSyncDirection.Push)
    {
        return CalendarIntegration.Create(
            AccountId, WorkspaceId, ConnectionId,
            CalendarProvider.Google, syncDirection, Actor, Now);
    }
}
