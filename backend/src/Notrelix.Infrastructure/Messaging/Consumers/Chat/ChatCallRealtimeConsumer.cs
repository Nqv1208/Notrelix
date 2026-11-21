using Notrelix.Application.Events.Chat;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Realtime;
using Notrelix.Domain.Chat.Calls;

namespace Notrelix.Infrastructure.Messaging.Consumers.Chat;

/// <remarks>
/// Call changes share the message endpoint and its partitioner (by space), so the changes of one call reach the channel stream in the
/// order the outbox committed them. The frame goes to the channel scope of the space; while a direct or group call rings, it also goes
/// to the stream of each other active member (bounded by <see cref="ChatRealtime.MaxUnreadRecipients"/>), so a person with no channel
/// open still rings. A frame is an advisory: REST is the source of truth and a reader refetches the call by its version.
/// </remarks>
[OrderingNotApplicable("Call changes of one space are delivered in outbox commit order on the shared chat message endpoint (partitioned by space); each frame carries the call version and readers keep the highest.")]
public sealed class ChatCallRealtimeConsumer : IConsumer<ChatCallChangedIntegrationEvent>
{
    private readonly IRealtimePublisher _publisher;
    private readonly IChatDbContext _db;

    public ChatCallRealtimeConsumer(IRealtimePublisher publisher, IChatDbContext db)
    {
        _publisher = publisher;
        _db = db;
    }

    public async Task Consume(ConsumeContext<ChatCallChangedIntegrationEvent> context)
    {
        var message = context.Message;
        var now = DateTimeOffset.UtcNow;
        var frames = new List<RealtimeResourceChangedV1> { ChatRealtime.ForCallChanged(message, now) };

        if (message.RingsMembers && message.State == CallWire.ToWire(CallState.Ringing))
        {
            var invited = await _db.ChatMemberships.AsNoTracking()
                .Where(m => m.SpaceId == message.SpaceId && m.LeftAt == null && m.UserId != message.InitiatorId)
                .OrderBy(m => m.UserId)
                .Take(ChatRealtime.MaxUnreadRecipients)
                .Select(m => m.UserId)
                .ToListAsync(context.CancellationToken);
            frames.AddRange(invited.Select(userId => ChatRealtime.CallRing(message, userId, now)));
        }

        await _publisher.PublishManyAsync(frames, context.CancellationToken);
    }
}

public sealed class ChatCallRealtimeConsumerDefinition : ConsumerDefinition<ChatCallRealtimeConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ChatCallRealtimeConsumer> consumerConfigurator,
        IRegistrationContext context) =>
        consumerConfigurator.Message<ChatCallChangedIntegrationEvent>(m =>
            m.UsePartitioner(ChatMessageRealtimePartitioning.PartitionCount, c => ChatMessageRealtimePartitioning.Key(c.Message.SpaceId)));
}
