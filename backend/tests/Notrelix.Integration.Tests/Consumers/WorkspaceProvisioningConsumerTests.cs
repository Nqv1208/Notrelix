using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data.Outbox;
using Notrelix.Infrastructure.Messaging.Consumers.Identity;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration.Factories;

namespace Notrelix.Integration.Tests.Consumers;

public class WorkspaceProvisioningConsumerTests
{
    private readonly Mock<IProcessedEventStore> _processedEventsMock;
    private readonly DateTimeOffset _occurredAt;

    public WorkspaceProvisioningConsumerTests()
    {
        _processedEventsMock = new Mock<IProcessedEventStore>();
        _occurredAt = DateTimeOffset.UtcNow;
    }

    [Fact]
    public async Task ProvisionPersonalWorkspace_WhenNewUser_ShouldCreateWorkspaceAndOwnerMember()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        using var _ = currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var email = "newuser@example.com";

        _processedEventsMock
            .Setup(e => e.IsProcessedAsync(eventId, nameof(WorkspaceProvisioningConsumer), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new WorkspaceProvisioningService(context, context, _processedEventsMock.Object);

        await service.ProvisionPersonalWorkspace(userId, email, eventId, _occurredAt, CancellationToken.None);

        var workspace = await context.Workspaces.FirstOrDefaultAsync(w => w.CreatedBy == userId);
        workspace.Should().NotBeNull();
        workspace!.Name.Should().Be($"{email}'s Workspace");

        var member = await context.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == workspace.Id && m.UserId == userId);
        member.Should().NotBeNull();
        member!.Role.Should().Be(WorkspaceRole.Owner);

        _processedEventsMock.Verify(e =>
            e.MarkProcessedAsync(It.Is<ProcessedEvent>(pe =>
                pe.EventId == eventId &&
                pe.ConsumerName == nameof(WorkspaceProvisioningConsumer)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProvisionPersonalWorkspace_WhenEventAlreadyProcessed_ShouldSkipProcessing()
    {
        using var context = TestDbContextFactory.CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _processedEventsMock
            .Setup(e => e.IsProcessedAsync(eventId, nameof(WorkspaceProvisioningConsumer), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new WorkspaceProvisioningService(context, context, _processedEventsMock.Object);

        await service.ProvisionPersonalWorkspace(userId, "skip@example.com", eventId, _occurredAt, CancellationToken.None);

        context.Workspaces.Should().BeEmpty();
        context.WorkspaceMembers.Should().BeEmpty();
        _processedEventsMock.Verify(e =>
            e.MarkProcessedAsync(It.IsAny<ProcessedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProvisionPersonalWorkspace_WhenPersonalWorkspaceExists_ShouldSkipCreation()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        using var _ = currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var existingWorkspace = Domain.Workspaces.Workspaces.Workspace.Create(
            userId, "Existing Workspace", "existing-workspace", _occurredAt, isPersonal: true);
        context.Workspaces.Add(existingWorkspace);
        await context.SaveChangesAsync();

        _processedEventsMock
            .Setup(e => e.IsProcessedAsync(eventId, nameof(WorkspaceProvisioningConsumer), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new WorkspaceProvisioningService(context, context, _processedEventsMock.Object);

        await service.ProvisionPersonalWorkspace(userId, "dup@example.com", eventId, _occurredAt, CancellationToken.None);

        (await context.Workspaces.CountAsync(w => w.IsPersonal)).Should().Be(1);
        context.WorkspaceMembers.Should().BeEmpty();
        _processedEventsMock.Verify(e =>
            e.MarkProcessedAsync(It.IsAny<ProcessedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
