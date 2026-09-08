using System.Reflection;

namespace Notrelix.Architecture.Tests.Events;

/// <summary>
/// TAC-GATE-008B/C/D — Automation process-reference gates.
///
///   008B — the messaging consumer is a thin adapter: it must not own
///          Automation business progression (no DbContext access, no rule
///          evaluation, no execution lifecycle calls).
///   008C — the Automation Domain is broker-neutral: no redelivery/queue/
///          dead-letter vocabulary survives in execution semantics.
///   008D — AutomationExecution is the sole durable process state; no second
///          teaching aggregate may appear.
/// </summary>
public class AutomationProcessReferenceArchitectureTests
{
    private static IReadOnlyList<Type> ProductionTypes() => ProductionAssemblies()
        .SelectMany(a => { try { return a.GetTypes(); } catch { return []; } })
        .ToList();

    private static IEnumerable<Assembly> ProductionAssemblies() =>
    [
        Assembly.Load("Notrelix.Domain"),
        Assembly.Load("Notrelix.Application"),
        Assembly.Load("Notrelix.Infrastructure"),
    ];

    private static Type? ResolveType(string fullName) =>
        ProductionAssemblies()
            .Select(a => a.GetType(fullName))
            .FirstOrDefault(t => t is not null);

    // ------------------------------------------------------------------
    // TAC-GATE-008B — consumer is a thin adapter
    // ------------------------------------------------------------------

    [Fact]
    public void N8nDispatchConsumer_IsAThinAdapterWithoutBusinessState()
    {
        var consumer = ResolveType(
             "Notrelix.Infrastructure.Messaging.Consumers.Automation.N8nDispatchConsumer");

        consumer.Should().NotBeNull("the dispatch consumer is the inbound adapter reference");

        var ctorParams = consumer!.GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType)
            .ToList();

        ctorParams.Should().NotContain(t => t.Name == "IAutomationDbContext",
            "the consumer must not load Automation persistence directly");
        ctorParams.Should().Contain(t => t.Name == "N8nDispatchUseCase",
            "the consumer must delegate business progression to the Application-owned use case");

        var referenced = CollectTypes(consumer);
        referenced.Should().NotContain(t => t.Name == "AutomationExecution",
            "the consumer must not touch execution lifecycle methods");
        referenced.Should().NotContain(t => t.Name == "AutomationRule",
            "the consumer must not interpret rule configuration");
    }

    [Fact]
    public void N8nDispatchUseCase_OwnsBusinessProgression()
    {
        var useCase = ResolveType(
             "Notrelix.Application.Features.Automation.Executions.Services.N8nDispatchUseCase");

        useCase.Should().NotBeNull("business progression lives in the Application-owned use case");

        var ctorParams = useCase!.GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType)
            .ToList();

        ctorParams.Should().Contain(t => t.Name == "IAutomationDbContext",
            "the use case loads and persists the execution");
        ctorParams.Should().Contain(t => t.Name == "IN8nWebhookActions",
            "the use case calls the Integrations-owned semantic action");
    }

    // ------------------------------------------------------------------
    // TAC-GATE-008C — Domain is broker-neutral
    // ------------------------------------------------------------------

    [Fact]
    public void AutomationExecution_Semantics_FreeOfBrokerVocabulary()
    {
        var execution = ResolveType(
             "Notrelix.Domain.Automation.Executions.AutomationExecution");

        execution.Should().NotBeNull();

        var memberNames = execution!.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToList();

        memberNames.Should().NotContain("RequeueForRedelivery",
            "broker-specific transition vocabulary is retired");

        var forbidden = new[] { "Redelivery", "DeadLetter", "Queue", "Topic" };
        var offenders = memberNames
            .Where(name => forbidden.Any(token => name.Contains(token, StringComparison.Ordinal)))
            .ToList();

        offenders.Should().BeEmpty(
            "Automation execution semantics must not mention broker delivery concepts: " + string.Join(", ", offenders));
    }

    // ------------------------------------------------------------------
    // TAC-GATE-008D — one durable process state
    // ------------------------------------------------------------------

    [Fact]
    public void NoSecondAutomationProcessStateAggregate()
    {
        var duplicates = ProductionTypes()
            .Where(t => t.Name is "AutomationProcessState" or "AutomationProcess" or "AutomationWorkflowState")
            .Select(t => t.FullName)
            .ToList();

        duplicates.Should().BeEmpty(
            "AutomationExecution is the durable process state; no parallel workflow aggregate may appear");
    }

    // ------------------------------------------------------------------
    // TAC-GATE-008F — one n8n provider seam, old Common surface retired
    // ------------------------------------------------------------------

    [Fact]
    public void N8nProviderSurface_IsOwnedByIntegrations()
    {
        var providerPort = ResolveType(
             "Notrelix.Application.Features.Integrations.N8n.Providers.IN8nClient");
        var publicAction = ResolveType(
             "Notrelix.Application.Features.Integrations.Public.Commands.IN8nWebhookActions");
        var retiredCommonClient = ResolveType(
             "Notrelix.Application.Common.Integrations.N8n.IN8nClient");

        providerPort.Should().NotBeNull("the provider port lives under Integrations ownership");
        publicAction.Should().NotBeNull("the semantic webhook action is the producer Public surface");
        retiredCommonClient.Should().BeNull(
            "the old Common provider surface was migrated, not kept as a parallel authority");
    }

    [Fact]
    public void N8nWebhookOutcome_CoversAllSemanticClasses()
    {
        var enumType = ResolveType(
             "Notrelix.Application.Features.Integrations.Public.Commands.N8nWebhookOutcome");

        enumType.Should().NotBeNull();
        var names = Enum.GetNames(enumType!);
        names.Should().BeEquivalentTo(
            ["Succeeded", "RetryableFailure", "TerminalFailure", "UnknownOutcome"],
            "the provider boundary must classify business, technical, and unknown outcomes");
    }

    private static IReadOnlyList<Type> CollectTypes(Type root)
    {
        const BindingFlags flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var seen = new HashSet<Type>();
        var collected = new List<Type>();

        void Collect(Type? candidate)
        {
            if (candidate is null || !seen.Add(candidate))
                return;
            collected.Add(candidate);
            if (candidate.IsGenericType && !candidate.IsGenericTypeDefinition)
                foreach (var arg in candidate.GetGenericArguments())
                    Collect(arg);
        }

        Collect(root.BaseType);
        foreach (var iface in root.GetInterfaces())
            Collect(iface);
        foreach (var field in root.GetFields(flags))
            Collect(field.FieldType);
        foreach (var property in root.GetProperties(flags))
            Collect(property.PropertyType);
        foreach (var ctor in root.GetConstructors(flags))
            foreach (var p in ctor.GetParameters())
                Collect(p.ParameterType);

        return collected;
    }
}
