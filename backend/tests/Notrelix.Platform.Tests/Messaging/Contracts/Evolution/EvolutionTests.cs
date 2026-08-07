using FluentAssertions;
using Moq;
using Notrelix.Application.Common.Events;
using Notrelix.Platform.Messaging.Contracts;
using Notrelix.Platform.Messaging.Contracts.Evolution;
using Notrelix.Platform.Messaging.Runtime;
using Notrelix.Platform.Messaging.Runtime.Governance;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Contracts.Evolution;

public sealed class UpcasterTests
{
    private sealed class V1ToV2Upcaster : IUpcaster
    {
        public string EventName => "test.event";
        public bool CanUpcast(int fromVersion, int toVersion) => fromVersion == 1 && toVersion == 2;
        public object Upcast(object @event, int fromVersion, int toVersion)
        {
            if (@event is V1Event v1)
                return new V2Event { Id = v1.Id, Name = v1.Name, Extra = "default" };
            return @event;
        }
    }

    private sealed record V1Event { public int Id { get; init; } public string Name { get; init; } = ""; }
    private sealed record V2Event { public int Id { get; init; } public string Name { get; init; } = ""; public string Extra { get; init; } = ""; }

    [Fact]
    public void CanUpcast_ShouldReturnTrue_WhenVersionMatches()
    {
        var upcaster = new V1ToV2Upcaster();
        upcaster.CanUpcast(1, 2).Should().BeTrue();
    }

    [Fact]
    public void CanUpcast_ShouldReturnFalse_WhenVersionMismatch()
    {
        var upcaster = new V1ToV2Upcaster();
        upcaster.CanUpcast(2, 3).Should().BeFalse();
    }

    [Fact]
    public void Upcast_ShouldConvertV1ToV2()
    {
        var upcaster = new V1ToV2Upcaster();
        var v1 = new V1Event { Id = 1, Name = "test" };

        var result = upcaster.Upcast(v1, 1, 2);

        result.Should().BeOfType<V2Event>();
        ((V2Event)result).Id.Should().Be(1);
        ((V2Event)result).Name.Should().Be("test");
        ((V2Event)result).Extra.Should().Be("default");
    }
}

public sealed class UpcasterRegistryTests
{
    private sealed class V1ToV2Upcaster : IUpcaster
    {
        public string EventName => "test.event";
        public bool CanUpcast(int fromVersion, int toVersion) => fromVersion == 1 && toVersion == 2;
        public object Upcast(object @event, int fromVersion, int toVersion) => "v2";
    }

    private sealed class V2ToV3Upcaster : IUpcaster
    {
        public string EventName => "test.event";
        public bool CanUpcast(int fromVersion, int toVersion) => fromVersion == 2 && toVersion == 3;
        public object Upcast(object @event, int fromVersion, int toVersion) => "v3";
    }

    private readonly UpcasterRegistry _registry = new();

    public UpcasterRegistryTests()
    {
        _registry.Register(new V1ToV2Upcaster());
        _registry.Register(new V2ToV3Upcaster());
    }

    [Fact]
    public void Register_ShouldAddUpcaster()
    {
        var registry = new UpcasterRegistry();
        registry.Register(new V1ToV2Upcaster());
        registry.GetUpcasters("test.event").Should().HaveCount(1);
    }

    [Fact]
    public void CanUpcast_ShouldReturnTrue_WhenUpcasterExists()
    {
        _registry.CanUpcast("test.event", 1, 2).Should().BeTrue();
    }

    [Fact]
    public void CanUpcast_ShouldReturnFalse_WhenNoUpcasterForVersions()
    {
        _registry.CanUpcast("test.event", 1, 3).Should().BeFalse();
    }

    [Fact]
    public void CanUpcast_ShouldReturnTrue_WhenSameVersion()
    {
        _registry.CanUpcast("test.event", 1, 1).Should().BeTrue();
    }

    [Fact]
    public void Upcast_ShouldReturnNull_WhenNoUpcaster()
    {
        _registry.Upcast("event", "test.event", 1, 3).Should().BeNull();
    }

    [Fact]
    public void UpcastChain_ShouldChainVersions_V1ToV3()
    {
        var result = _registry.UpcastChain("initial", "test.event", 1, 3);
        result.Should().Be("v3");
    }

    [Fact]
    public void UpcastChain_ShouldReturnSame_WhenSameVersion()
    {
        var result = _registry.UpcastChain("initial", "test.event", 1, 1);
        result.Should().Be("initial");
    }
}

public sealed class VersionCompatibilityEvaluatorTests
{
    [Fact]
    public void Backward_ShouldPass_WhenConsumerNewer()
    {
        var evaluator = new VersionCompatibilityEvaluator(CompatibilityLevel.Backward);
        var descriptor = CreateDescriptor("test", 1);

        var result = evaluator.Evaluate(descriptor, 2);

        result.Compatible.Should().BeTrue();
        result.Level.Should().Be(CompatibilityLevel.Backward);
    }

    [Fact]
    public void Backward_ShouldFail_WhenConsumerOlder()
    {
        var evaluator = new VersionCompatibilityEvaluator(CompatibilityLevel.Backward);
        var descriptor = CreateDescriptor("test", 2);

        var result = evaluator.Evaluate(descriptor, 1);

        result.Compatible.Should().BeFalse();
    }

    [Fact]
    public void Forward_ShouldPass_WhenConsumerOlder()
    {
        var evaluator = new VersionCompatibilityEvaluator(CompatibilityLevel.Forward);
        var descriptor = CreateDescriptor("test", 2);

        var result = evaluator.Evaluate(descriptor, 1);

        result.Compatible.Should().BeTrue();
        result.Level.Should().Be(CompatibilityLevel.Forward);
    }

    [Fact]
    public void Forward_ShouldFail_WhenConsumerNewer()
    {
        var evaluator = new VersionCompatibilityEvaluator(CompatibilityLevel.Forward);
        var descriptor = CreateDescriptor("test", 1);

        var result = evaluator.Evaluate(descriptor, 2);

        result.Compatible.Should().BeFalse();
    }

    [Fact]
    public void Full_ShouldAlwaysPass()
    {
        var evaluator = new VersionCompatibilityEvaluator(CompatibilityLevel.Full);
        var descriptor = CreateDescriptor("test", 1);

        var result = evaluator.Evaluate(descriptor, 99);

        result.Compatible.Should().BeTrue();
    }

    [Fact]
    public void SameVersion_ShouldPass()
    {
        var evaluator = new VersionCompatibilityEvaluator(CompatibilityLevel.Backward);
        var descriptor = CreateDescriptor("test", 2);

        var result = evaluator.Evaluate(descriptor, 2);

        result.Compatible.Should().BeTrue();
        result.Level.Should().Be(CompatibilityLevel.Full);
    }

    private static EventDescriptor CreateDescriptor(string name, int version) => new()
    {
        Name = name,
        Version = version,
        EventType = typeof(object),
        Classification = EventClassification.Business,
    };
}

public sealed class DeprecationPolicyTests
{
    private readonly Mock<IEventDescriptorProvider> _providerMock = new();

    [Fact]
    public async Task EvaluateAsync_ShouldAllow_WhenNotDeprecated()
    {
        _providerMock.Setup(p => p.Get("test.event", 1))
            .Returns(new EventDescriptor
            {
                Name = "test.event",
                Version = 1,
                EventType = typeof(object),
                Classification = EventClassification.Business,
                Deprecated = false,
            });

        var policy = new DeprecationPolicy(_providerMock.Object);
        var envelope = CreateEnvelope("test.event", 1);

        var result = await policy.EvaluateAsync(envelope);

        result.Decision.Should().Be(GovernanceDecision.Allow);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldWarn_WhenDeprecatedWithinGracePeriod()
    {
        _providerMock.Setup(p => p.Get("test.event", 1))
            .Returns(new EventDescriptor
            {
                Name = "test.event",
                Version = 1,
                EventType = typeof(object),
                Classification = EventClassification.Business,
                Deprecated = true,
                DeprecationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                ReplacementEventName = "test.event.v2",
            });

        var policy = new DeprecationPolicy(_providerMock.Object);
        var envelope = CreateEnvelope("test.event", 1);

        var result = await policy.EvaluateAsync(envelope);

        result.Decision.Should().Be(GovernanceDecision.Warn);
        result.Reason.Should().Contain("deprecated");
    }

    [Fact]
    public async Task EvaluateAsync_ShouldBlock_WhenDeprecatedPastGracePeriod()
    {
        _providerMock.Setup(p => p.Get("test.event", 1))
            .Returns(new EventDescriptor
            {
                Name = "test.event",
                Version = 1,
                EventType = typeof(object),
                Classification = EventClassification.Business,
                Deprecated = true,
                DeprecationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                ReplacementEventName = "test.event.v2",
            });

        var policy = new DeprecationPolicy(_providerMock.Object);
        var envelope = CreateEnvelope("test.event", 1);

        var result = await policy.EvaluateAsync(envelope);

        result.Decision.Should().Be(GovernanceDecision.Block);
        result.Reason.Should().Contain("deprecated");
    }

    [Fact]
    public async Task EvaluateAsync_ShouldAllow_WhenUnknownEvent()
    {
        _providerMock.Setup(p => p.Get("unknown.event", 1))
            .Throws(new UnknownEventDescriptorException("unknown.event"));

        var policy = new DeprecationPolicy(_providerMock.Object);
        var envelope = CreateEnvelope("unknown.event", 1);

        var result = await policy.EvaluateAsync(envelope);

        result.Decision.Should().Be(GovernanceDecision.Allow);
    }

    private static EventEnvelope CreateEnvelope(string eventName, int version) => new()
    {
        Id = Guid.NewGuid(),
        EventName = eventName,
        EventVersion = version,
        CorrelationId = Guid.NewGuid(),
        OccurredAt = DateTimeOffset.UtcNow,
        Data = new byte[0],
        ContentType = "application/json",
    };
}
