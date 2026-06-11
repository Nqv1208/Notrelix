using FluentAssertions;
using Notrelix.Domain.Common;
using Xunit;

namespace Notrelix.Domain.Tests.Common;

public class AggregateRootTests
{
    private sealed record TestEvent() : DomainEvent(DateTimeOffset.UtcNow);

    private class TestAggregate : AggregateRoot
    {
        public void DoSomething()
        {
            AddDomainEvent(new TestEvent());
        }
    }

    [Fact]
    public void AddDomainEvent_ShouldAddToCollection()
    {
        var aggregate = new TestAggregate();
        aggregate.DoSomething();

        aggregate.DomainEvents.Should().HaveCount(1);
        aggregate.DomainEvents.First().Should().BeOfType<TestEvent>();
    }

    [Fact]
    public void ClearDomainEvents_ShouldEmptyCollection()
    {
        var aggregate = new TestAggregate();
        aggregate.DoSomething();
        
        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }
}
