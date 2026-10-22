using System.Reflection;
using Notrelix.Infrastructure.Messaging;

namespace Notrelix.Architecture.Tests.PlatformMessaging;

/// <summary>
/// PF-08 / NRX-009 / NRX-010 (BE-PLT ordering and poison rules): every production
/// message consumer carries one explicit ordering declaration, so a new consumer
/// cannot silently skip the ordering decision.
///
/// Production delivery runs on MassTransit plus the dedup filter. Platform's
/// ConsumerHost / OrderingEnforcer / PoisonDetector are reusable mechanisms that
/// are not in that graph and no integration event carries a contiguous per-stream
/// sequence, so the only declaration available today is "ordering not applicable"
/// with a reason. A consumer that needs ordered delivery has no valid declaration
/// until the ordering owner and sequence producer are decided (Class G/I change).
/// </summary>
public class ConsumerOrderingDeclarationTests
{
    private const int MinimumReasonLength = 40;

    private static readonly Assembly InfrastructureAssembly = typeof(ConsumerRegistry).Assembly;

    private static bool IsConsumerInterface(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition().FullName == "MassTransit.IConsumer`1";

    internal static IReadOnlyList<Type> FindConsumers(IEnumerable<Type> types) =>
        types
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(IsConsumerInterface))
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();

    internal static IReadOnlyList<string> FindViolations(IEnumerable<Type> consumers)
    {
        var violations = new List<string>();
        foreach (var consumer in consumers)
        {
            var declaration = consumer.GetCustomAttribute<OrderingNotApplicableAttribute>(inherit: false);
            if (declaration is null)
            {
                violations.Add($"{consumer.FullName}: missing an ordering declaration ([OrderingNotApplicable(reason)]).");
            }
            else if (string.IsNullOrWhiteSpace(declaration.Reason)
                     || declaration.Reason.Trim().Length < MinimumReasonLength)
            {
                violations.Add($"{consumer.FullName}: the ordering declaration needs a real justification (>= {MinimumReasonLength} characters).");
            }
        }

        return violations;
    }

    [Fact]
    public void EveryProductionConsumer_DeclaresOrderingExplicitly()
    {
        var consumers = FindConsumers(InfrastructureAssembly.GetTypes());

        // NRX-016: the rule must have inspected real consumers, never pass on an empty set.
        consumers.Count.Should().BeGreaterThan(40);

        FindViolations(consumers).Should().BeEmpty(
            "every production consumer must state whether delivery order matters, so new consumers cannot skip the PF-08 decision");
    }

    [Fact]
    public void Rule_FailsForAConsumerWithoutADeclaration()
    {
        var violations = FindViolations(FindConsumers([typeof(UndeclaredConsumer)]));

        violations.Should().ContainSingle().Which.Should().Contain(nameof(UndeclaredConsumer));
    }

    [Fact]
    public void Rule_FailsForABlankOrTrivialJustification()
    {
        var violations = FindViolations(FindConsumers([typeof(TrivialReasonConsumer)]));

        violations.Should().ContainSingle().Which.Should().Contain("justification");
    }

    [Fact]
    public void Rule_AcceptsADeclaredConsumer()
    {
        FindViolations(FindConsumers([typeof(DeclaredConsumer)])).Should().BeEmpty();
    }

    [Fact]
    public void DeclarationAttribute_IsOnlyUsedOnConsumers()
    {
        var consumers = FindConsumers(InfrastructureAssembly.GetTypes()).ToHashSet();

        var strays = InfrastructureAssembly.GetTypes()
            .Where(t => t.GetCustomAttribute<OrderingNotApplicableAttribute>(inherit: false) is not null)
            .Where(t => !consumers.Contains(t))
            .Select(t => t.FullName)
            .ToList();

        strays.Should().BeEmpty("the ordering declaration describes a consumer, not arbitrary types");
    }

    [Fact]
    public void ConsumerRegistry_DoesNotClaimOrderingThatNoRuntimeEnforces()
    {
        var definitions = ConsumerRegistrySetup.GetConsumerDefinitions();

        definitions.Count.Should().BeGreaterThan(40);

        // ConsumerDefinition.OrderingRequired is inert metadata: nothing in the
        // MassTransit production graph reads it. Setting it would claim a guarantee
        // that is not enforced; it needs the ordering-owner decision first.
        definitions.Where(d => d.OrderingRequired).Select(d => d.ConsumerName)
            .Should().BeEmpty("no production runtime honors OrderingRequired, so it must not be declared");
    }

    private sealed class UndeclaredConsumer : MassTransit.IConsumer<UndeclaredMessage>
    {
        public Task Consume(MassTransit.ConsumeContext<UndeclaredMessage> context) => Task.CompletedTask;
    }

    [OrderingNotApplicable("x")]
    private sealed class TrivialReasonConsumer : MassTransit.IConsumer<UndeclaredMessage>
    {
        public Task Consume(MassTransit.ConsumeContext<UndeclaredMessage> context) => Task.CompletedTask;
    }

    [OrderingNotApplicable("Synthetic consumer with an independent, order-insensitive effect for the rule test.")]
    private sealed class DeclaredConsumer : MassTransit.IConsumer<UndeclaredMessage>
    {
        public Task Consume(MassTransit.ConsumeContext<UndeclaredMessage> context) => Task.CompletedTask;
    }

    public sealed record UndeclaredMessage;
}
