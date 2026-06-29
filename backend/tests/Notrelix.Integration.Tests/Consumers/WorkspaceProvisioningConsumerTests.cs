using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Messaging;
using Notrelix.Application.Features.Workspaces.Provisioning.Commands.ProvisionPersonalWorkspace;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data.Outbox;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration.Factories;

namespace Notrelix.Integration.Tests.Consumers;

public class WorkspaceProvisioningConsumerTests
{
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly DateTimeOffset _occurredAt;

    public WorkspaceProvisioningConsumerTests()
    {
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _occurredAt = DateTimeOffset.UtcNow;
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task ProvisionPersonalWorkspace_WhenNewUser_ShouldCreateWorkspaceAndOwnerMemberAndProcessedEvent()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        using var _ = currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
        var userId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var email = "newuser@example.com";

        var deduplicationStore = new MessageDeduplicationStore(context);
        var handler = new ProvisionPersonalWorkspaceCommandHandler(
            context, deduplicationStore, _dateTimeProviderMock.Object);

        var command = new ProvisionPersonalWorkspaceCommand(
            UserId: userId,
            Email: email,
            MessageId: messageId,
            SourceEventId: messageId,
            SourceMessageName: "identity.user-registered",
            SourceMessageVersion: 1,
            CorrelationId: null,
            CausationId: null,
            OccurredAt: _occurredAt);

        var result = await handler.Handle(command, CancellationToken.None);

        // SaveChanges to commit workspace + processed event together
        await context.SaveChangesAsync();

        result.AlreadyExisted.Should().BeFalse();
        result.WorkspaceId.Should().NotBeEmpty();

        // Assert workspace created
        var workspace = await context.Workspaces.FirstOrDefaultAsync(w => w.CreatedBy == userId);
        workspace.Should().NotBeNull();
        workspace!.Name.Should().Be($"{email}'s Workspace");

        // Assert owner member created
        var member = await context.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == workspace.Id && m.UserId == userId);
        member.Should().NotBeNull();
        member!.Role.Should().Be(WorkspaceRole.Owner);

        // Assert processed event recorded IN SAME TRANSACTION
        var processedEvent = await context.ProcessedEvents
            .FirstOrDefaultAsync(pe => pe.EventId == messageId && pe.ConsumerName == ConsumerNames.PersonalWorkspaceProvisioning);
        processedEvent.Should().NotBeNull();
        processedEvent!.MessageName.Should().Be("identity.user-registered");
    }

    [Fact]
    public async Task ProvisionPersonalWorkspace_WhenEventAlreadyProcessed_ShouldSkipProcessing()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        using var _ = currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
        var userId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        // Create the personal workspace that was supposedly created before
        var existingWorkspace = Domain.Workspaces.Workspaces.Workspace.Create(
            userId, "Existing Workspace", "existing-workspace", _occurredAt, isPersonal: true);
        context.Workspaces.Add(existingWorkspace);

        // Seed a processed event to simulate previous successful provisioning
        context.ProcessedEvents.Add(ProcessedEvent.Create(messageId, ConsumerNames.PersonalWorkspaceProvisioning,
            "identity.user-registered", 1, null, null, _occurredAt));
        await context.SaveChangesAsync();

        var deduplicationStore = new MessageDeduplicationStore(context);
        var handler = new ProvisionPersonalWorkspaceCommandHandler(
            context, deduplicationStore, _dateTimeProviderMock.Object);

        var command = new ProvisionPersonalWorkspaceCommand(
            UserId: userId,
            Email: "skip@example.com",
            MessageId: messageId,
            SourceEventId: messageId,
            SourceMessageName: "identity.user-registered",
            SourceMessageVersion: 1,
            CorrelationId: null,
            CausationId: null,
            OccurredAt: _occurredAt);

        var result = await handler.Handle(command, CancellationToken.None);

        // Assert no workspace/member created since event already processed
        (await context.Workspaces.CountAsync(w => w.IsPersonal)).Should().Be(1);
        context.WorkspaceMembers.Should().BeEmpty();

        // Assert exactly one processed event still (no duplicate)
        (await context.ProcessedEvents.CountAsync()).Should().Be(1);

        result.AlreadyExisted.Should().BeTrue();
        result.WorkspaceId.Should().Be(existingWorkspace.Id);
    }

    [Fact]
    public async Task ProvisionPersonalWorkspace_WhenPersonalWorkspaceExists_ShouldSkipCreationButMarkProcessed()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        using var _ = currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
        var userId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        // Create existing personal workspace
        var existingWorkspace = Domain.Workspaces.Workspaces.Workspace.Create(
            userId, "Existing Workspace", "existing-workspace", _occurredAt, isPersonal: true);
        context.Workspaces.Add(existingWorkspace);
        await context.SaveChangesAsync();

        var deduplicationStore = new MessageDeduplicationStore(context);
        var handler = new ProvisionPersonalWorkspaceCommandHandler(
            context, deduplicationStore, _dateTimeProviderMock.Object);

        var command = new ProvisionPersonalWorkspaceCommand(
            UserId: userId,
            Email: "dup@example.com",
            MessageId: messageId,
            SourceEventId: messageId,
            SourceMessageName: "identity.user-registered",
            SourceMessageVersion: 1,
            CorrelationId: null,
            CausationId: null,
            OccurredAt: _occurredAt);

        var result = await handler.Handle(command, CancellationToken.None);

        await context.SaveChangesAsync();

        // Assert no new workspace created
        (await context.Workspaces.CountAsync(w => w.IsPersonal)).Should().Be(1);

        // Assert processed event IS recorded (crash recovery: must mark processed)
        var processedEvent = await context.ProcessedEvents
            .FirstOrDefaultAsync(pe => pe.EventId == messageId && pe.ConsumerName == ConsumerNames.PersonalWorkspaceProvisioning);
        processedEvent.Should().NotBeNull();

        result.AlreadyExisted.Should().BeTrue();
    }

    [Fact]
    public async Task ProvisionPersonalWorkspace_WhenProcessedButWorkspaceMissing_ShouldThrowConsistencyError()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        using var _ = currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
        var userId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        // Seed processed event WITHOUT workspace (simulates crash after processed but before workspace)
        context.ProcessedEvents.Add(ProcessedEvent.Create(messageId, ConsumerNames.PersonalWorkspaceProvisioning,
            "identity.user-registered", 1, null, null, _occurredAt));
        await context.SaveChangesAsync();

        var deduplicationStore = new MessageDeduplicationStore(context);
        var handler = new ProvisionPersonalWorkspaceCommandHandler(
            context, deduplicationStore, _dateTimeProviderMock.Object);

        var command = new ProvisionPersonalWorkspaceCommand(
            UserId: userId,
            Email: "missing@example.com",
            MessageId: messageId,
            SourceEventId: messageId,
            SourceMessageName: "identity.user-registered",
            SourceMessageVersion: 1,
            CorrelationId: null,
            CausationId: null,
            OccurredAt: _occurredAt);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Data consistency violation*");
    }
}
