using System.Reflection;
using Notrelix.Domain.Common;

namespace Notrelix.Architecture.Tests.Events;

/// <summary>
/// ARCH-BC-007 — Integration Event Ownership / Version semantics (boundary
/// Wave 3). Focused companion to the existing event gates; it reuses the same
/// IIntegrationEvent discovery as ContractRegistryCompletenessTests and does
/// not introduce a second registry.
///
/// Enforced structurally:
///   1. every outward integration event resolves to a canonical producer
///      context via its `Application/Events/{Context}` namespace — an event
///      with no mappable producer owner is not a stable contract;
///   2. consumer-instruction smells (run/refresh/reindex/trigger prefixes,
///      `For{Consumer}` suffixes) are blocked unless the reviewed baseline
///      contains the exact contract identity with its classification
///      (BOUND-EVT-003: events describe producer-completed facts, not
///      consumer instructions).
///
/// Semantic intent (is it really a completed fact?) stays a certification
/// review question — the machine gate only freezes the reviewed inventory.
/// </summary>
public class IntegrationEventOwnershipArchitectureTests
{
    private static readonly IReadOnlySet<string> BusinessContexts =
        CrossContextBoundaryScanner.BusinessContexts;

    private static IReadOnlyList<Type> IntegrationEventTypes => typeof(IIntegrationEvent).Assembly
        .GetTypes()
        .Where(t => t is { IsAbstract: false, IsInterface: false }
                    && typeof(IIntegrationEvent).IsAssignableFrom(t))
        .ToList();

    [Fact]
    public void EveryOutwardEvent_ResolvesToCanonicalProducerContext()
    {
        var unmapped = IntegrationEventTypes
            .Where(t => !IsSharedMechanismContract(t))
            .Select(t => (Type: t, ProducerContext: ResolveProducerContext(t)))
            .Where(x => x.ProducerContext is null)
            .Select(x => $"{x.Type.FullName} (namespace '{x.Type.Namespace}')")
            .ToList();

        unmapped.Should().BeEmpty(
            "ARCH-BC-007: every outward event must resolve to a canonical producer context " +
            "under Application/Events/{Context}. Unmapped:\n  " + string.Join("\n  ", unmapped));
    }

    [Fact]
    public void EveryOutwardEvent_FreeOfConsumerInstructionSmell()
    {
        var violations = new List<string>();

        foreach (var type in IntegrationEventTypes)
        {
            var eventName = type.GetCustomAttribute<EventNameAttribute>()?.Name
                            ?? type.FullName ?? type.Name;

            if (MatchesInstructionPrefixSmell(eventName) || MatchesInstructionPrefixSmell(type.Name))
            {
                violations.Add(
                    $"{eventName}: instruction-style naming (run/refresh/reindex/trigger). " +
                    "Events describe producer-completed facts — the consumer owns its reaction.");
            }
            else if (MatchesConsumerCoupledSuffix(type.Name) && !IsReviewedConsumerCoupledContract(type))
            {
                violations.Add(
                    $"{eventName}: consumer-coupled naming (For{{Consumer}}). Producer facts " +
                    "must not be named after a single consumer; add a reviewed baseline entry " +
                    "or rename to the producer fact.");
            }
        }

        violations.Should().BeEmpty(
            "ARCH-BC-007: outward events must describe producer-completed facts.\n" +
            string.Join("\n", violations));
    }

    // ------------------------------------------------------------------
    // Reviewed baseline — exact contract identities classified during the
    // BND-M6 event inventory. Add an entry only with a certification review
    // record (owner, reason, migration trigger). Never a wildcard.
    // ------------------------------------------------------------------

    private static readonly HashSet<(string ClrType, string Classification)> ReviewedConsumerCoupledContracts =
        new()
        {
            (
                "Notrelix.Application.Events.Automation.BoardItemMemberAssignedForAutomationIntegrationEvent",
                "MIGRATE-ON-TOUCH (R2): fact is WorkManagement member assignment; name is " +
                "consumer-coupled. Trigger: next material edit to the member-assigned event path. " +
                "Target: WorkManagement-owned fact event."
            ),
        };

    private static string? ResolveProducerContext(Type type)
    {
        const string marker = "Notrelix.Application.Events.";
        var ns = type.Namespace ?? string.Empty;
        if (!ns.StartsWith(marker, StringComparison.Ordinal))
            return null;

        var remainder = ns[marker.Length..];
        var dot = remainder.IndexOf('.');
        var candidate = dot > 0 ? remainder[..dot] : remainder;

        return BusinessContexts.Contains(candidate) ? candidate : null;
    }

    /// <summary>
    /// Shared pipeline mechanism contracts (e.g. Common.Realtime change
    /// records) implement IIntegrationEvent for transport uniformity but are
    /// not context-owned business facts; the emitting context supplies their
    /// scope at runtime. Business-context ownership applies to context event
    /// namespaces only.
    /// </summary>
    private static bool IsSharedMechanismContract(Type type)
    {
        var ns = type.Namespace ?? string.Empty;
        return ns.StartsWith("Notrelix.Application.Common.", StringComparison.Ordinal);
    }

    private static bool MatchesInstructionPrefixSmell(string name)
    {
        // Wire names: '{context}.{rest}' — inspect the fact segment after the
        // producer context prefix; CLR names are inspected whole.
        var factSegment = name.Contains('.', StringComparison.Ordinal)
            ? name[(name.IndexOf('.') + 1)..]
            : name;

        var prefixes = new[] { "run", "refresh", "reindex", "trigger" };

        foreach (var prefix in prefixes)
        {
            if (factSegment.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool MatchesConsumerCoupledSuffix(string clrTypeName)
    {
        // 'For' followed by a capitalized consumer segment near the end of the
        // CLR type name, e.g. BoardItemMemberAssignedForAutomationIntegrationEvent.
        const string marker = "For";
        var index = clrTypeName.IndexOf(marker, StringComparison.Ordinal);

        while (index >= 0)
        {
            var after = clrTypeName[(index + marker.Length)..];
            if (after.Length > 0 && char.IsUpper(after[0]))
                return true;

            index = clrTypeName.IndexOf(marker, index + 1, StringComparison.Ordinal);
        }

        return false;
    }

    private static bool IsReviewedConsumerCoupledContract(Type type)
    {
        return ReviewedConsumerCoupledContracts.Contains((type.FullName ?? type.Name, string.Empty))
               || ReviewedConsumerCoupledContracts.Any(entry =>
                   entry.ClrType == (type.FullName ?? type.Name));
    }
}
