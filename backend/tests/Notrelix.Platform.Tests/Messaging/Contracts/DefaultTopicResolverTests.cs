using FluentAssertions;
using Notrelix.Application.Common.Events;
using Notrelix.Platform.Messaging.Contracts;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Contracts;

public sealed class DefaultTopicResolverTests
{
    private readonly DefaultTopicResolver _sut = new();

    [Fact]
    public void ResolveTopic_ShouldUseDomainAndName()
    {
        var descriptor = new EventDescriptor
        {
            Name = "board.created",
            Version = 1,
            EventType = typeof(object),
            Classification = EventClassification.Business,
        };

        var topic = _sut.ResolveTopic(descriptor);

        topic.Should().Be("board.created.v1");
    }

    [Fact]
    public void ResolveTopic_ShouldHandleSingleSegment()
    {
        var descriptor = new EventDescriptor
        {
            Name = "ping",
            Version = 1,
            EventType = typeof(object),
            Classification = EventClassification.Business,
        };

        var topic = _sut.ResolveTopic(descriptor);

        topic.Should().Be("ping.v1");
    }

    [Fact]
    public void ResolveTopic_ShouldIncludeVersion()
    {
        var descriptor = new EventDescriptor
        {
            Name = "workspace.member.added",
            Version = 2,
            EventType = typeof(object),
            Classification = EventClassification.Business,
        };

        var topic = _sut.ResolveTopic(descriptor);

        topic.Should().Be("workspace.added.v2");
    }

    [Fact]
    public void ResolveTopic_ShouldHandleMultiSegmentName()
    {
        var descriptor = new EventDescriptor
        {
            Name = "board.item.field_value.changed",
            Version = 1,
            EventType = typeof(object),
            Classification = EventClassification.Business,
        };

        var topic = _sut.ResolveTopic(descriptor);

        topic.Should().Be("board.changed.v1");
    }
}
