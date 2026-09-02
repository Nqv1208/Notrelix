using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Workspaces.Provisioning.Commands.ProvisionPersonalWorkspace;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Infrastructure.Messaging.Consumers.Identity.RegistrationCompleted;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Infrastructure.Tests.Messaging.Consumers;

public class WorkspaceProvisioningConsumerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RegistrationCompleted_ProvisionsPersonalWorkspaceUnderAccountTenant()
    {
        var accountId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var integrationEvent = new IdentityRegistrationCompletedIntegrationEventV1(
            EventId: Guid.CreateVersion7(),
            UserId: userId,
            AccountId: accountId,
            Email: "registration@example.com",
            DisplayName: "Registration User",
            AccountName: "Registration User's Account",
            CorrelationId: Guid.CreateVersion7(),
            ActorUserId: userId,
            SourceEventId: null,
            CausationId: null,
            OccurredAt: Now);

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        var filter = new TenantContextConsumeFilter<IdentityRegistrationCompletedIntegrationEventV1>(
            tenant,
            NullLogger<TenantContextConsumeFilter<IdentityRegistrationCompletedIntegrationEventV1>>.Instance);

        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(
                It.IsAny<ProvisionPersonalWorkspaceCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProvisionPersonalWorkspaceResult(workspaceId, AlreadyExisted: false));

        var executionContextWriter = new Mock<IIdempotencyExecutionContextWriter>();
        var consumer = new WorkspaceProvisioningConsumer(
            sender.Object,
            executionContextWriter.Object,
            NullLogger<WorkspaceProvisioningConsumer>.Instance);

        var context = new Mock<ConsumeContext<IdentityRegistrationCompletedIntegrationEventV1>>();
        context.SetupGet(c => c.Message).Returns(integrationEvent);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        Guid? observedAccount = null;
        bool observedSystemContext = true;

        var pipe = new Mock<IPipe<ConsumeContext<IdentityRegistrationCompletedIntegrationEventV1>>>();
        pipe.Setup(p => p.Send(It.IsAny<ConsumeContext<IdentityRegistrationCompletedIntegrationEventV1>>()))
            .Callback(async () =>
            {
                observedAccount = tenant.AccountId;
                observedSystemContext = tenant.IsSystemContext;
                await consumer.Consume(context.Object);
            })
            .Returns(Task.CompletedTask);

        await filter.Send(context.Object, pipe.Object);

        observedAccount.Should().Be(accountId);
        observedSystemContext.Should().BeFalse();
        executionContextWriter.Verify(w => w.Set(integrationEvent.EventId.ToString("N"), IdempotencyExecutionSource.Message), Times.Once);
        sender.Verify(s => s.Send(
            It.Is<ProvisionPersonalWorkspaceCommand>(c =>
                c.AccountId == accountId &&
                c.UserId == userId &&
                c.MessageId == integrationEvent.EventId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
