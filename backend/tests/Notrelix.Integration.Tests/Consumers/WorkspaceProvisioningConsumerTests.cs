using Notrelix.Application.Common.Abstractions;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data.Outbox;
using Notrelix.Infrastructure.Messaging.Consumers.Identity;
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
        var eventId = Guid.NewGuid();
        var email = "newuser@example.com";

        var service = new WorkspaceProvisioningService(context, context, _dateTimeProviderMock.Object);

        await service.ProvisionPersonalWorkspace(userId, email, eventId, _occurredAt, CancellationToken.None);

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
            .FirstOrDefaultAsync(pe => pe.EventId == eventId && pe.ConsumerName == nameof(WorkspaceProvisioningConsumer));
        processedEvent.Should().NotBeNull();
        processedEvent!.MessageName.Should().Be("user.registered");
    }

    [Fact]
    public async Task ProvisionPersonalWorkspace_WhenEventAlreadyProcessed_ShouldSkipProcessing()
    {
        using var context = TestDbContextFactory.CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // Seed a processed event to simulate previous successful provisioning
        context.ProcessedEvents.Add(ProcessedEvent.Create(eventId, nameof(WorkspaceProvisioningConsumer),
            "user.registered", 1, null, null, _occurredAt));
        await context.SaveChangesAsync();

        var service = new WorkspaceProvisioningService(context, context, _dateTimeProviderMock.Object);

        await service.ProvisionPersonalWorkspace(userId, "skip@example.com", eventId, _occurredAt, CancellationToken.None);

        // Assert no workspace/member created since event already processed
        context.Workspaces.Should().BeEmpty();
        context.WorkspaceMembers.Should().BeEmpty();

        // Assert exactly one processed event still (no duplicate)
        (await context.ProcessedEvents.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ProvisionPersonalWorkspace_WhenPersonalWorkspaceExists_ShouldSkipCreation()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        using var _ = currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // Create existing personal workspace
        var existingWorkspace = Domain.Workspaces.Workspaces.Workspace.Create(
            userId, "Existing Workspace", "existing-workspace", _occurredAt, isPersonal: true);
        context.Workspaces.Add(existingWorkspace);
        await context.SaveChangesAsync();

        var service = new WorkspaceProvisioningService(context, context, _dateTimeProviderMock.Object);

        await service.ProvisionPersonalWorkspace(userId, "dup@example.com", eventId, _occurredAt, CancellationToken.None);

        // Assert no new workspace created
        (await context.Workspaces.CountAsync(w => w.IsPersonal)).Should().Be(1);
        context.WorkspaceMembers.Should().BeEmpty();

        // Assert no processed event recorded (since we short-circuited on existing workspace)
        (await context.ProcessedEvents.CountAsync()).Should().Be(0);
    }
}
