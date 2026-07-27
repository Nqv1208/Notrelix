using FluentAssertions;
using Notrelix.Platform.Messaging.Contracts;
using Notrelix.Platform.Messaging.Runtime;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Runtime;

public sealed class CompatibilityEvaluatorTests
{
    [Fact]
    public void BackwardCompatibility_ShouldAllow_WhenConsumerNewer()
    {
        var evaluator = new BackwardCompatibilityEvaluator();
        var descriptor = new EventDescriptor
        {
            Name = "test.event",
            Version = 1,
            EventType = typeof(object),
        };

        var result = evaluator.Evaluate(descriptor, consumerVersion: 2);

        result.Compatible.Should().BeTrue();
        result.Level.Should().Be(CompatibilityLevel.Backward);
    }

    [Fact]
    public void BackwardCompatibility_ShouldFail_WhenConsumerOlder()
    {
        var evaluator = new BackwardCompatibilityEvaluator();
        var descriptor = new EventDescriptor
        {
            Name = "test.event",
            Version = 2,
            EventType = typeof(object),
        };

        var result = evaluator.Evaluate(descriptor, consumerVersion: 1);

        result.Compatible.Should().BeFalse();
        result.Level.Should().Be(CompatibilityLevel.None);
    }

    [Fact]
    public void BackwardCompatibility_ShouldAllow_WhenSameVersion()
    {
        var evaluator = new BackwardCompatibilityEvaluator();
        var descriptor = new EventDescriptor
        {
            Name = "test.event",
            Version = 1,
            EventType = typeof(object),
        };

        var result = evaluator.Evaluate(descriptor, consumerVersion: 1);

        result.Compatible.Should().BeTrue();
    }

    [Fact]
    public void FullCompatibility_ShouldAlwaysAllow()
    {
        var evaluator = new FullCompatibilityEvaluator();
        var descriptor = new EventDescriptor
        {
            Name = "test.event",
            Version = 1,
            EventType = typeof(object),
        };

        var newerResult = evaluator.Evaluate(descriptor, consumerVersion: 2);
        var olderResult = evaluator.Evaluate(descriptor, consumerVersion: 1);

        newerResult.Compatible.Should().BeTrue();
        olderResult.Compatible.Should().BeTrue();
    }
}
