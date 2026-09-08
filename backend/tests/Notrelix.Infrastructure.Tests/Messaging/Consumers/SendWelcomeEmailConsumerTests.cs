using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Email;
using Notrelix.Application.Events.Identity;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Infrastructure.Messaging.Consumers.Identity.RegistrationCompleted;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Infrastructure.Tests.Messaging.Consumers;

public class SendWelcomeEmailConsumerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RegistrationCompleted_QueuesWelcomeOnceUnderAccountTenant()
    {
        var accountId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var integrationEvent = new IdentityRegistrationCompletedIntegrationEventV1(
            EventId: Guid.CreateVersion7(),
            UserId: userId,
            AccountId: accountId,
            Email: "welcome@example.com",
            DisplayName: "Welcome User",
            AccountName: "Welcome User's Account",
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

        var emailOutboxWriter = new Mock<IEmailOutboxWriter>();
        var consumer = new SendWelcomeEmailConsumer(
            emailOutboxWriter.Object,
            NullLogger<SendWelcomeEmailConsumer>.Instance);

        var context = new Mock<ConsumeContext<IdentityRegistrationCompletedIntegrationEventV1>>();
        context.SetupGet(c => c.Message).Returns(integrationEvent);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        QueueRenderedEmailRequest? request = null;
        Guid? observedAccount = null;
        bool observedSystemContext = true;

        emailOutboxWriter
            .Setup(w => w.QueueRenderedEmailAsync(It.IsAny<QueueRenderedEmailRequest>(), It.IsAny<CancellationToken>()))
            .Callback<QueueRenderedEmailRequest, CancellationToken>((r, _) => request = r)
            .Returns(Task.CompletedTask);

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
        request.Should().NotBeNull();
        request!.DeduplicationKey.Should().Be($"welcome-email:{userId}");
        request.RecipientEmail.Should().Be("welcome@example.com");
        request.RecipientName.Should().Be("Welcome User");
        request.WorkspaceId.Should().BeNull();
        request.RecipientUserId.Should().Be(userId);
        request.SourceContext.Should().Be("identity");
        request.TemplateKey.Should().Be("welcome-email");
        emailOutboxWriter.Verify(
            w => w.QueueRenderedEmailAsync(It.IsAny<QueueRenderedEmailRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
