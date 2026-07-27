using FluentAssertions;
using Moq;
using Notrelix.Platform.Messaging.Contracts;
using Notrelix.Platform.Messaging.Runtime;
using Notrelix.Platform.Messaging.Transport;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Transport;

public sealed class DefaultTransportPolicyTests
{
    [Fact]
    public void ResolveTopic_ShouldDelegateToTopicResolver()
    {
        var resolverMock = new Mock<ITopicResolver>();
        resolverMock.Setup(r => r.ResolveTopic(It.IsAny<EventDescriptor>()))
            .Returns("board.created.v1");

        var sut = new DefaultTransportPolicy(resolverMock.Object);
        var descriptor = new EventDescriptor
        {
            Name = "board.created",
            Version = 1,
            EventType = typeof(object),
        };
        var context = new PublishContext
        {
            CorrelationId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
        };

        var topic = sut.ResolveTopic(descriptor, context);

        topic.Should().Be("board.created.v1");
    }
}
