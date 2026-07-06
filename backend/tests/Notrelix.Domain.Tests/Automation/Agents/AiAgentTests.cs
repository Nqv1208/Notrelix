using FluentAssertions;
using Notrelix.Domain.Automation.Agents;
using Notrelix.Domain.Automation.Agents.Events;

namespace Notrelix.Domain.Tests.Automation;

public class AiAgentTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var agent = CreateAgent();

        agent.Name.Should().Be("Test Agent");
        agent.Status.Should().Be(AiAgentStatus.Draft);
        agent.ScopeType.Should().Be(AiAgentScopeType.Workspace);
        agent.DomainEvents.Should().ContainSingle(e => e is AiAgentCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithBoardScope_RequiresResourceId()
    {
        var act = () => AiAgent.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Agent", null, AiAgentScopeType.Board, null,
            JsonValue.EmptyObject(), JsonValue.EmptyObject(), JsonValue.EmptyObject(),
            Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Update_ShouldUpdate_AndRaiseEvent()
    {
        var agent = CreateAgent();
        agent.ClearDomainEvents();

        agent.Update("Updated", "Desc", JsonValue.EmptyObject(), JsonValue.EmptyObject(), JsonValue.EmptyObject(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        agent.Name.Should().Be("Updated");
        agent.Description.Should().Be("Desc");
        agent.DomainEvents.Should().ContainSingle(e => e is AiAgentUpdatedDomainEvent);
    }

    [Fact]
    public void Update_WhenDeleted_ShouldThrow()
    {
        var agent = CreateAgent();
        agent.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => agent.Update("Name", null, JsonValue.EmptyObject(), JsonValue.EmptyObject(), JsonValue.EmptyObject(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void ChangeStatus_ShouldUpdateStatus_AndRaiseEvent()
    {
        var agent = CreateAgent();
        agent.ClearDomainEvents();

        agent.ChangeStatus(AiAgentStatus.Enabled, Guid.NewGuid(), DateTimeOffset.UtcNow);

        agent.Status.Should().Be(AiAgentStatus.Enabled);
        agent.DomainEvents.Should().ContainSingle(e => e is AiAgentStatusChangedDomainEvent);
    }

    [Fact]
    public void ChangeStatus_WhenSameStatus_ShouldBeNoOp()
    {
        var agent = CreateAgent();
        agent.ClearDomainEvents();

        agent.ChangeStatus(AiAgentStatus.Draft, Guid.NewGuid(), DateTimeOffset.UtcNow);

        agent.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeStatus_ToDeleted_ShouldCallSoftDelete()
    {
        var agent = CreateAgent();
        agent.ClearDomainEvents();

        agent.ChangeStatus(AiAgentStatus.Deleted, Guid.NewGuid(), DateTimeOffset.UtcNow);

        agent.IsDeleted.Should().BeTrue();
        agent.Status.Should().Be(AiAgentStatus.Deleted);
        agent.DomainEvents.Should().ContainSingle(e => e is AiAgentStatusChangedDomainEvent);
    }

    [Fact]
    public void ChangeStatus_WhenDeleted_ShouldThrow()
    {
        var agent = CreateAgent();
        agent.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => agent.ChangeStatus(AiAgentStatus.Enabled, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void SoftDelete_ShouldSetDeleted_AndRaiseEvent()
    {
        var agent = CreateAgent();
        agent.ClearDomainEvents();

        agent.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        agent.IsDeleted.Should().BeTrue();
        agent.Status.Should().Be(AiAgentStatus.Deleted);
        agent.DomainEvents.Should().ContainSingle(e => e is AiAgentStatusChangedDomainEvent);
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var agent = CreateAgent();
        agent.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        agent.ClearDomainEvents();

        agent.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        agent.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_ShouldSetDraft_AndRaiseEvent()
    {
        var agent = CreateAgent();
        agent.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        agent.ClearDomainEvents();

        agent.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        agent.IsDeleted.Should().BeFalse();
        agent.Status.Should().Be(AiAgentStatus.Draft);
        agent.DomainEvents.Should().ContainSingle(e => e is AiAgentStatusChangedDomainEvent);
    }

    [Fact]
    public void Restore_WhenNotDeleted_ShouldBeNoOp()
    {
        var agent = CreateAgent();
        agent.ClearDomainEvents();

        agent.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        agent.DomainEvents.Should().BeEmpty();
    }

    private static AiAgent CreateAgent()
    {
        return AiAgent.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Test Agent", null, AiAgentScopeType.Workspace, null,
            JsonValue.EmptyObject(), JsonValue.EmptyObject(), JsonValue.EmptyObject(),
            Guid.NewGuid(), DateTimeOffset.UtcNow);
    }
}
