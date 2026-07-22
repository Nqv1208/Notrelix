using Notrelix.Domain.Common;
using Notrelix.Infrastructure.Events;

namespace Notrelix.Architecture.Tests;

public class ClassificationPolicyArchitectureTests
{
    [Fact]
    public void ClassificationPolicy_ShouldNotThrow_ForAnyDomainEvent()
    {
        var policy = ClassificationPolicy.CreateBuilder().Build();

        var domainEvents = typeof(IDomainEvent).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(IDomainEvent).IsAssignableFrom(t))
            .ToList();

        foreach (var eventType in domainEvents)
        {
            var classification = policy.GetClassification(eventType);
            classification.Should().NotBeNull();
            classification.Value.Should().BeOneOf(
                EventClassification.Business,
                EventClassification.Lifecycle,
                EventClassification.System,
                EventClassification.Audit,
                EventClassification.Internal);
        }
    }

    [Fact]
    public void DeliveryPolicy_ShouldNotThrow_ForAnyDomainEvent()
    {
        var policy = DeliveryPolicy.CreateBuilder().Build();

        var domainEvents = typeof(IDomainEvent).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(IDomainEvent).IsAssignableFrom(t))
            .ToList();

        foreach (var eventType in domainEvents)
        {
            var decision = policy.GetDecision(eventType);
            decision.Should().NotBeNull();
        }
    }
}
