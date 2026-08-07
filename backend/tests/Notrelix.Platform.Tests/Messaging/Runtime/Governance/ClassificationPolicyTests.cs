using FluentAssertions;
using Notrelix.Application.Common.Events;
using Notrelix.Platform.Messaging.Runtime;
using Notrelix.Platform.Messaging.Runtime.Governance;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Runtime.Governance;

public sealed class ClassificationPolicyTests
{
    private readonly ClassificationPolicy _sut = new();

    [Fact]
    public async Task EvaluateAsync_ShouldAllow_ForBusinessEvents()
    {
        var envelope = CreateEnvelope(EventClassification.Business);

        var result = await _sut.EvaluateAsync(envelope);

        result.Decision.Should().Be(GovernanceDecision.Allow);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldAllow_ForLifecycleEvents()
    {
        var envelope = CreateEnvelope(EventClassification.Lifecycle);

        var result = await _sut.EvaluateAsync(envelope);

        result.Decision.Should().Be(GovernanceDecision.Allow);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldWarn_ForAuditEvents()
    {
        var envelope = CreateEnvelope(EventClassification.Audit);

        var result = await _sut.EvaluateAsync(envelope);

        result.Decision.Should().Be(GovernanceDecision.Warn);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldWarn_ForInternalEvents()
    {
        var envelope = CreateEnvelope(EventClassification.Internal);

        var result = await _sut.EvaluateAsync(envelope);

        result.Decision.Should().Be(GovernanceDecision.Warn);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldWarn_ForSystemEvents()
    {
        var envelope = CreateEnvelope(EventClassification.System);

        var result = await _sut.EvaluateAsync(envelope);

        result.Decision.Should().Be(GovernanceDecision.Allow);
    }

    private static EventEnvelope CreateEnvelope(EventClassification classification) => new()
    {
        Id = Guid.NewGuid(),
        EventName = "test.event",
        EventVersion = 1,
        CorrelationId = Guid.NewGuid(),
        OccurredAt = DateTimeOffset.UtcNow,
        Data = Array.Empty<byte>(),
        ContentType = "application/json",
        Classification = classification,
    };
}
