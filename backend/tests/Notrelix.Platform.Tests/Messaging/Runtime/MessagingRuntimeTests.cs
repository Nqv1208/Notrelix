using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Notrelix.Application.Common.Events;
using Notrelix.Domain.Common;
using Notrelix.Platform.Messaging.Contracts;
using Notrelix.Platform.Messaging.Runtime;
using Notrelix.Platform.Messaging.Runtime.Governance;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Runtime;

[EventName("pipeline.test", Version = 1)]
file sealed record PipelineTestEvent : IIntegrationEvent
{
    public Guid EventId { get; init; }
    public Guid? SourceEventId { get; init; }
    public string MessageName => "pipeline.test";
    public int SchemaVersion => 1;
    public Guid? AccountId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public Guid? ActorUserId { get; init; }
    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}

public sealed class MessagingRuntimeTests
{
    private readonly Mock<IEventDescriptorProvider> _providerMock = new();
    private readonly Mock<IEventSerializer> _serializerMock = new();
    private readonly Mock<ICanonicalizer> _canonicalizerMock = new();
    private readonly Mock<ICompatibilityEvaluator> _compatMock = new();
    private readonly MessagingRuntime _sut;

    public MessagingRuntimeTests()
    {
        _serializerMock.Setup(s => s.Serialize(It.IsAny<object>(), It.IsAny<Type>()))
            .Returns(new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes("{}")));

        _canonicalizerMock.Setup(c => c.Canonicalize(It.IsAny<ReadOnlyMemory<byte>>()))
            .Returns((ReadOnlyMemory<byte> data) => data);

        _providerMock.Setup(p => p.Get(typeof(PipelineTestEvent)))
            .Returns(new EventDescriptor
            {
                Name = "pipeline.test",
                Version = 1,
                EventType = typeof(PipelineTestEvent),
                Classification = EventClassification.Business,
            });

        _providerMock.Setup(p => p.Get("pipeline.test", 1))
            .Returns(new EventDescriptor
            {
                Name = "pipeline.test",
                Version = 1,
                EventType = typeof(PipelineTestEvent),
                Classification = EventClassification.Business,
            });

        _compatMock.Setup(c => c.Evaluate(It.IsAny<EventDescriptor>(), It.IsAny<int>()))
            .Returns(CompatibilityResult.Ok(CompatibilityLevel.Full));

        var envelopeBuilder = new EnvelopeBuilder(_providerMock.Object, _serializerMock.Object);
        var schemaValidation = new SchemaValidationRule(
            _canonicalizerMock.Object,
            _providerMock.Object,
            NullLogger<SchemaValidationRule>.Instance);
        var governance = new GovernanceEngine(
            [new AuthorizationPolicy()],
            NullLogger<GovernanceEngine>.Instance);

        _sut = new MessagingRuntime(
            _providerMock.Object,
            envelopeBuilder,
            _canonicalizerMock.Object,
            _serializerMock.Object,
            schemaValidation,
            _compatMock.Object,
            governance,
            NullLogger<MessagingRuntime>.Instance);
    }

    [Fact]
    public async Task PublishAsync_ShouldSucceed_WhenPipelinePasses()
    {
        var result = await _sut.PublishAsync(new EventPublication
        {
            Event = new PipelineTestEvent
            {
                EventId = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                OccurredAt = DateTimeOffset.UtcNow,
            },
            Context = new PublishContext
            {
                CorrelationId = Guid.NewGuid(),
                WorkspaceId = Guid.NewGuid(),
                OccurredAt = DateTimeOffset.UtcNow,
            },
        });

        result.Success.Should().BeTrue();
        result.EnvelopeId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PublishAsync_ShouldFail_WhenDescriptorNotFound()
    {
        _providerMock.Setup(p => p.Get(typeof(PipelineTestEvent)))
            .Throws(new UnknownEventDescriptorException("not found"));

        var act = () => _sut.PublishAsync(new EventPublication
        {
            Event = new PipelineTestEvent
            {
                EventId = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                OccurredAt = DateTimeOffset.UtcNow,
            },
            Context = new PublishContext
            {
                CorrelationId = Guid.NewGuid(),
                WorkspaceId = Guid.NewGuid(),
                OccurredAt = DateTimeOffset.UtcNow,
            },
        });

        await act.Should().ThrowAsync<UnknownEventDescriptorException>();
    }

    [Fact]
    public async Task PublishAsync_ShouldFail_WhenCompatibilityFails()
    {
        _compatMock.Setup(c => c.Evaluate(It.IsAny<EventDescriptor>(), It.IsAny<int>()))
            .Returns(CompatibilityResult.Fail("version mismatch"));

        var result = await _sut.PublishAsync(new EventPublication
        {
            Event = new PipelineTestEvent
            {
                EventId = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                OccurredAt = DateTimeOffset.UtcNow,
            },
            Context = new PublishContext
            {
                CorrelationId = Guid.NewGuid(),
                WorkspaceId = Guid.NewGuid(),
                OccurredAt = DateTimeOffset.UtcNow,
            },
        });

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e!.Contains("version mismatch"));
    }
}
