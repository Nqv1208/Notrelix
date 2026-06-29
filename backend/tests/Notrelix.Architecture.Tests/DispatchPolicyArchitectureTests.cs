using Notrelix.Application.Common.Events;
using Notrelix.Domain.Common;
using Notrelix.Infrastructure.Events;

namespace Notrelix.Architecture.Tests;

public class DispatchPolicyArchitectureTests
{
    [Fact]
    public void AllDomainEvents_ShouldBeRegistered_InDispatchPolicy()
    {
        var policy = new DomainEventDispatchPolicy();

        var domainEvents = typeof(IDomainEvent).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(IDomainEvent).IsAssignableFrom(t))
            .ToList();

        var unregistered = new List<string>();
        foreach (var eventType in domainEvents)
        {
            try
            {
                policy.GetMode(eventType);
            }
            catch (InvalidOperationException)
            {
                unregistered.Add(eventType.FullName!);
            }
        }

        unregistered.Should().BeEmpty(
            $"All IDomainEvent types must be registered in DomainEventDispatchPolicy. " +
            $"Missing: {string.Join(", ", unregistered)}");
    }

    [Fact]
    public void DispatchPolicy_ShouldNotThrow_ForAnyRegisteredEvent()
    {
        var policy = new DomainEventDispatchPolicy();

        var domainEvents = typeof(IDomainEvent).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(IDomainEvent).IsAssignableFrom(t))
            .ToList();

        foreach (var eventType in domainEvents)
        {
            var mode = policy.GetMode(eventType);
            mode.Should().BeOneOf(DomainEventDispatchMode.Inline, DomainEventDispatchMode.Outbox, DomainEventDispatchMode.Ignore);
        }
    }
}
