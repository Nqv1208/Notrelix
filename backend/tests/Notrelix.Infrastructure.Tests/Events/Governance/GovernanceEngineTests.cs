using Notrelix.Infrastructure.Events;
using Notrelix.Infrastructure.Events.Governance;

namespace Notrelix.Infrastructure.Tests.Events.Governance;

public class GovernanceEngineTests
{
    private readonly EventEnvelope _envelope = new()
    {
        Id = Guid.NewGuid(),
        EventName = "TestEvent",
        EventVersion = 1,
        SourceContext = "Test",
        AggregateType = "Test",
        AggregateId = Guid.NewGuid(),
        SubjectType = "User",
        SubjectId = Guid.NewGuid(),
        CorrelationId = "corr-1",
    };

    [Fact]
    public async Task EvaluateAsync_NoRules_Allows()
    {
        var engine = new GovernanceEngine([]);

        var result = await engine.EvaluateAsync(_envelope);

        result.Decision.Should().Be(GovernanceDecision.Allow);
        result.RuleName.Should().Be("GovernanceEngine");
    }

    [Fact]
    public async Task EvaluateAsync_AllAllow_ReturnsAllowed()
    {
        var rule1 = CreateMockRule("Rule1", GovernanceDecision.Allow);
        var rule2 = CreateMockRule("Rule2", GovernanceDecision.Allow);
        var engine = new GovernanceEngine([rule1.Object, rule2.Object]);

        var result = await engine.EvaluateAsync(_envelope);

        result.Decision.Should().Be(GovernanceDecision.Allow);
    }

    [Fact]
    public async Task EvaluateAsync_OneBlock_ReturnsBlocked()
    {
        var allowRule = CreateMockRule("AllowRule", GovernanceDecision.Allow);
        var blockRule = CreateMockRule("BlockRule", GovernanceDecision.Block, "not allowed");
        var engine = new GovernanceEngine([allowRule.Object, blockRule.Object]);

        var result = await engine.EvaluateAsync(_envelope);

        result.Decision.Should().Be(GovernanceDecision.Block);
        result.RuleName.Should().Be("BlockRule");
        result.Reason.Should().Be("not allowed");
    }

    [Fact]
    public async Task EvaluateAsync_AllWarn_ReturnsAllowed()
    {
        var rule1 = CreateMockRule("WarnRule1", GovernanceDecision.Warn, "warning 1");
        var rule2 = CreateMockRule("WarnRule2", GovernanceDecision.Warn, "warning 2");
        var engine = new GovernanceEngine([rule1.Object, rule2.Object]);

        var result = await engine.EvaluateAsync(_envelope);

        result.Decision.Should().Be(GovernanceDecision.Allow);
    }

    [Fact]
    public async Task ContractValidationRule_ValidContract_Allows()
    {
        var contractRegistry = new Mock<IContractRegistry>();
        contractRegistry
            .Setup(r => r.Get("TestEvent", 1))
            .Returns(new ContractDefinition { Name = "TestEvent", Version = 1 });

        var rule = new ContractValidationRule(contractRegistry.Object);

        var result = await rule.EvaluateAsync(_envelope);

        result.Decision.Should().Be(GovernanceDecision.Allow);
        result.RuleName.Should().Be("ContractValidation");
    }

    [Fact]
    public async Task TraceContextValidationRule_MissingBothTraceFields_Warns()
    {
        var envelope = _envelope with { CorrelationId = null, TraceParent = null };
        var rule = new TraceContextValidationRule();

        var result = await rule.EvaluateAsync(envelope);

        result.Decision.Should().Be(GovernanceDecision.Warn);
        result.RuleName.Should().Be("TraceContextValidation");
    }

    [Fact]
    public async Task EvaluateAsync_ThrowingRule_Throws()
    {
        var throwingRule = new Mock<IGovernanceRule>();
        throwingRule.Setup(r => r.Name).Returns("ThrowingRule");
        throwingRule
            .Setup(r => r.EvaluateAsync(It.IsAny<EventEnvelope>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var engine = new GovernanceEngine([throwingRule.Object]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => engine.EvaluateAsync(_envelope));
    }

    private static Mock<IGovernanceRule> CreateMockRule(
        string name,
        GovernanceDecision decision,
        string reason = "")
    {
        var verdict = decision switch
        {
            GovernanceDecision.Allow => GovernanceResult.Allow(name),
            GovernanceDecision.Block => GovernanceResult.Block(name, reason),
            GovernanceDecision.Warn => GovernanceResult.Warn(name, reason),
            _ => GovernanceResult.Allow(name),
        };

        var rule = new Mock<IGovernanceRule>();
        rule.Setup(r => r.Name).Returns(name);
        rule
            .Setup(r => r.EvaluateAsync(It.IsAny<EventEnvelope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(verdict);
        return rule;
    }
}
