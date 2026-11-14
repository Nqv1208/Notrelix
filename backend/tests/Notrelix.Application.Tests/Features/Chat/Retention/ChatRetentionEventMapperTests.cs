using Notrelix.Application.EventMappers.Chat;
using Notrelix.Domain.Chat.Spaces.Events;

namespace Notrelix.Application.Tests.Features.Chat.Retention;

/// <summary>UC-CHT-H05: a retention batch reaches the channel as one bounded chat.space.changed frame with no message and no actor.</summary>
public class ChatRetentionEventMapperTests
{
    [Fact]
    public void TheFactBecomesASpaceChangedWithTheRetentionChange_NoMessageNoActor()
    {
        var ws = Guid.NewGuid();
        var space = Guid.NewGuid();
        var evt = new ChannelRetentionAppliedDomainEvent(Guid.NewGuid(), ws, space, DateTimeOffset.UtcNow);

        var mapped = new ChatRetentionEventMapper().Map(evt)!;

        mapped.ChangeKind.Should().Be("retention");
        mapped.SpaceId.Should().Be(space);
        mapped.WorkspaceId.Should().Be(ws);
        mapped.MessageId.Should().Be(Guid.Empty);
        mapped.ActorUserId.Should().BeNull();
    }
}
