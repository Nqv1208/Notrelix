using MassTransit;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Events.Workspaces;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Members.Services;
using Notrelix.Infrastructure.Messaging.Consumers.Chat;

namespace Notrelix.Infrastructure.Tests.Messaging.Consumers;

public class ChatWorkspaceMemberRemovedConsumerTests
{
    [Fact]
    public async Task A_fact_without_a_workspace_id_is_ignored_and_touches_no_channel()
    {
        var db = new Mock<IChatDbContext>();
        var access = new Mock<IAccessChangeSignal>();
        var consumer = new ChatWorkspaceMemberRemovedConsumer(new ChatWorkspaceExit(db.Object, access.Object));
        var message = new WorkspaceMemberRemovedIntegrationEvent(
            Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), Guid.NewGuid(), null, null, DateTimeOffset.UtcNow);
        var context = new Mock<ConsumeContext<WorkspaceMemberRemovedIntegrationEvent>>();
        context.SetupGet(c => c.Message).Returns(message);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(context.Object);

        db.VerifyGet(d => d.ChatMemberships, Times.Never);
        access.VerifyNoOtherCalls();
    }
}
