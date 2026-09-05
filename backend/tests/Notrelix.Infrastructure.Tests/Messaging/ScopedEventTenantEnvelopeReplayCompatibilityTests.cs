using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using MassTransit;
using Notrelix.Application.Events.Documents;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Infrastructure.Tests.Messaging;

public class ScopedEventTenantEnvelopeReplayCompatibilityTests
{
    private static readonly JsonSerializerOptions PayloadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void Catalog_ResolvesWorkspaceEventVersion_WithoutUpcaster()
    {
        var catalog = new IntegrationEventCatalog([typeof(PageCreatedIntegrationEvent)]);

        catalog.Resolve(new EventContractKey("page.created", 1))
            .Should().Be(typeof(PageCreatedIntegrationEvent));
    }

    [Fact]
    public async Task ReplayedWorkspaceEvent_WithNullAccountId_FailsClosedBeforeConsumer()
    {
        var workspaceId = Guid.CreateVersion7();
        var json = JsonSerializer.Serialize(new
        {
            eventId = Guid.CreateVersion7(),
            accountId = (Guid?)null,
            pageId = Guid.CreateVersion7(),
            workspaceId,
            title = "Legacy page",
            parentId = (Guid?)null,
            correlationId = Guid.CreateVersion7(),
            actorUserId = (Guid?)null,
            causationId = (Guid?)null,
            occurredAt = DateTimeOffset.UtcNow,
        }, PayloadOptions);

        var replayed = JsonSerializer.Deserialize<PageCreatedIntegrationEvent>(json, PayloadOptions);
        replayed.Should().NotBeNull();

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        var filter = new TenantContextConsumeFilter<PageCreatedIntegrationEvent>(
            tenant,
            NullLogger<TenantContextConsumeFilter<PageCreatedIntegrationEvent>>.Instance);

        var context = new Mock<ConsumeContext<PageCreatedIntegrationEvent>>();
        context.SetupGet(c => c.Message).Returns(replayed!);

        var pipe = new Mock<IPipe<ConsumeContext<PageCreatedIntegrationEvent>>>();

        var act = () => filter.Send(context.Object, pipe.Object);

        await act.Should().ThrowAsync<IntegrationEventTenantEnvelopeException>();
        pipe.Verify(p => p.Send(It.IsAny<ConsumeContext<PageCreatedIntegrationEvent>>()), Times.Never);
        tenant.IsSystemContext.Should().BeFalse("a rejected scoped event must not leave a System tenant active");
    }

    [Fact]
    public async Task ReplayedWorkspaceEvent_WithValidAccountId_RestoresWorkspaceTenant()
    {
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var json = JsonSerializer.Serialize(new
        {
            eventId = Guid.CreateVersion7(),
            accountId,
            pageId = Guid.CreateVersion7(),
            workspaceId,
            title = "Replayed page",
            parentId = (Guid?)null,
            correlationId = Guid.CreateVersion7(),
            actorUserId = (Guid?)null,
            causationId = (Guid?)null,
            occurredAt = DateTimeOffset.UtcNow,
        }, PayloadOptions);

        var replayed = JsonSerializer.Deserialize<PageCreatedIntegrationEvent>(json, PayloadOptions);
        replayed.Should().NotBeNull();

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        var filter = new TenantContextConsumeFilter<PageCreatedIntegrationEvent>(
            tenant,
            NullLogger<TenantContextConsumeFilter<PageCreatedIntegrationEvent>>.Instance);

        var context = new Mock<ConsumeContext<PageCreatedIntegrationEvent>>();
        context.SetupGet(c => c.Message).Returns(replayed!);

        Guid? observedAccount = null;
        Guid? observedWorkspace = null;
        bool observedSystemContext = true;

        var pipe = new Mock<IPipe<ConsumeContext<PageCreatedIntegrationEvent>>>();
        pipe.Setup(p => p.Send(It.IsAny<ConsumeContext<PageCreatedIntegrationEvent>>()))
            .Callback<ConsumeContext<PageCreatedIntegrationEvent>>(_ =>
            {
                observedAccount = tenant.AccountId;
                observedWorkspace = tenant.WorkspaceId;
                observedSystemContext = tenant.IsSystemContext;
            })
            .Returns(Task.CompletedTask);

        await filter.Send(context.Object, pipe.Object);

        observedAccount.Should().Be(accountId);
        observedWorkspace.Should().Be(workspaceId);
        observedSystemContext.Should().BeFalse();
    }

    [Fact]
    public void TenantEnvelopeException_IsArgumentException_ForDeterministicRetryClassification()
    {
        typeof(IntegrationEventTenantEnvelopeException).Should().BeAssignableTo<ArgumentException>();
    }
}
