using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Events;
using Notrelix.Platform.Messaging.Runtime;
using Notrelix.Platform.Messaging.Runtime.Governance;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Runtime.Governance;

public sealed class GovernanceEngineTests
{
    private readonly EventEnvelope _envelope = new()
    {
        EventName = "test.event",
        EventVersion = 1,
        CorrelationId = Guid.NewGuid(),
        OccurredAt = DateTimeOffset.UtcNow,
        Data = Array.Empty<byte>(),
        ContentType = "application/json",
        Classification = EventClassification.Business,
    };

    [Fact]
    public async Task EvaluateAsync_ShouldAllow_WhenAllPoliciesAllow()
    {
        var policies = new IGovernancePolicy[]
        {
            new AuthorizationPolicy(),
            new ClassificationPolicy(),
            new DeliveryPolicy(),
            new RetentionPolicy(),
            new CompliancePolicy(),
        };
        var sut = new GovernanceEngine(policies, NullLogger<GovernanceEngine>.Instance);

        var results = await sut.EvaluateAsync(_envelope);

        results.Should().AllSatisfy(r => r.Decision.Should().Be(GovernanceDecision.Allow));
    }

    [Fact]
    public async Task EvaluateAsync_ShouldShortCircuit_WhenPolicyBlocks()
    {
        var blockingPolicy = new MockBlockPolicy("TestBlocker", GovernanceDecision.Block);
        var policies = new IGovernancePolicy[]
        {
            blockingPolicy,
            new AuthorizationPolicy(),
        };
        var sut = new GovernanceEngine(policies, NullLogger<GovernanceEngine>.Instance);

        var results = await sut.EvaluateAsync(_envelope);

        results.Should().HaveCount(1);
        results[0].PolicyName.Should().Be("TestBlocker");
        results[0].Decision.Should().Be(GovernanceDecision.Block);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldContinue_WhenPolicyWarns()
    {
        var warnPolicy = new MockBlockPolicy("Warner", GovernanceDecision.Warn);
        var policies = new IGovernancePolicy[]
        {
            warnPolicy,
            new AuthorizationPolicy(),
        };
        var sut = new GovernanceEngine(policies, NullLogger<GovernanceEngine>.Instance);

        var results = await sut.EvaluateAsync(_envelope);

        results.Should().HaveCount(2);
        results[0].PolicyName.Should().Be("Warner");
        results[0].Decision.Should().Be(GovernanceDecision.Warn);
        results[1].Decision.Should().Be(GovernanceDecision.Allow);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldReturnResults_ForAllPolicies()
    {
        var policies = new IGovernancePolicy[]
        {
            new AuthorizationPolicy(),
            new ClassificationPolicy(),
            new DeliveryPolicy(),
        };
        var sut = new GovernanceEngine(policies, NullLogger<GovernanceEngine>.Instance);

        var results = await sut.EvaluateAsync(_envelope);

        results.Should().HaveCount(3);
    }

    private sealed class MockBlockPolicy : IGovernancePolicy
    {
        private readonly GovernanceDecision _decision;

        public MockBlockPolicy(string name, GovernanceDecision decision)
        {
            Name = name;
            _decision = decision;
        }

        public string Name { get; }

        public Task<GovernanceResult> EvaluateAsync(
            EventEnvelope envelope,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _decision == GovernanceDecision.Block
                    ? GovernanceResult.Block(Name, "Mock block")
                    : GovernanceResult.Warn(Name, "Mock warn"));
        }
    }
}
