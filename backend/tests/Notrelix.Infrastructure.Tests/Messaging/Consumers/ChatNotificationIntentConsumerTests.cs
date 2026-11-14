using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Events.Chat;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Notifications.Services;
using Notrelix.Application.Features.Collaboration.Public.Notifications;
using Notrelix.Infrastructure.Messaging.Consumers.Chat;

namespace Notrelix.Infrastructure.Tests.Messaging.Consumers;

public class ChatNotificationIntentConsumerTests
{
    [Fact]
    public async Task A_fact_without_an_account_and_workspace_envelope_is_ignored_and_reads_nothing()
    {
        var db = new Mock<IChatDbContext>();
        var intents = new Mock<IChatNotificationIntents>();
        var consumer = new ChatNotificationIntentConsumer(
            new ChatNotificationHandoff(db.Object, intents.Object, NullLogger<ChatNotificationHandoff>.Instance),
            NullLogger<ChatNotificationIntentConsumer>.Instance);
        var message = new ChatMessageCreatedIntegrationEvent(
            Guid.NewGuid(), null, null, Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), "user", "hi", null, 1, Guid.NewGuid());
        var context = new Mock<ConsumeContext<ChatMessageCreatedIntegrationEvent>>();
        context.SetupGet(c => c.Message).Returns(message);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(context.Object);

        db.VerifyGet(d => d.ChatMessages, Times.Never);
        intents.VerifyNoOtherCalls();
    }
}
