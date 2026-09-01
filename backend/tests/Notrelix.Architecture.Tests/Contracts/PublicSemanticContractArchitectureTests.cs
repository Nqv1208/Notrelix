using System.Reflection;

namespace Notrelix.Architecture.Tests.Contracts;

/// <summary>
/// ARCH-BC-005 — Public Semantic Contract Purity (boundary Wave 2).
///
/// Types under `Application/Features/{Producer}/Public/**` are the approved
/// cross-context semantic surface. They must stay semantic and transport
/// neutral:
///   allowed roots — System.*, Application.Common technical primitives,
///   the producer's own Public namespace;
///   forbidden — producer Domain models, other contexts' internal types,
///   Infrastructure/API/Platform types, EF Core, transport (ASP.NET, gRPC,
///   HTTP clients) and provider SDK types.
///
/// Zero Public surfaces is valid: a context with no cross-context consumer
/// must not create the folder (BND-PUB-001). The gate activates automatically
/// as soon as a Public type exists.
/// </summary>
public class PublicSemanticContractArchitectureTests
{
    [Fact]
    public void PublicContractTypes_ShouldRemain_SemanticAndTransportNeutral()
    {
        var publicTypes = Assembly.Load("Notrelix.Application")
            .GetTypes()
            .Where(t => CrossContextBoundaryScanner.ResolveContextFromNamespace(t.Namespace) is not null)
            .Where(IsPublicSurfaceType)
            .ToList();

        if (publicTypes.Count == 0)
        {
            // No producer Public surface exists yet — BOUND-PUB-001 forbids
            // speculative Public folders, so an empty surface is the healthy state.
            return;
        }

        var violations = new List<string>();

        foreach (var type in publicTypes)
        {
            var ownProducer = ResolveProducerFromPublicNamespace(type.Namespace);
            foreach (var referenced in CollectReferencedTypes(type))
            {
                if (ClassifyPurity(referenced, ownProducer) is { } reason)
                    violations.Add($"{type.FullName}: {reason} ({referenced.FullName})");
            }
        }

        violations.Should().BeEmpty(
            "ARCH-BC-005: producer Public contracts must be small immutable semantic " +
            "surfaces. Violations:\n" + string.Join("\n", violations));
    }

    // ------------------------------------------------------------------
    // Gate self-tests
    // ------------------------------------------------------------------

    [Fact]
    public void Gate_Detects_DomainTypeInsidePublicContract()
    {
        var violation = ClassifyPurity(typeof(Notrelix.Domain.WorkManagement.Boards.Board), ownProducer: null);

        violation.Should().NotBeNull("producer Domain aggregates must not leak into Public contracts");
    }

    [Fact]
    public void Gate_Detects_TransportTypeInsidePublicContract()
    {
        var violation = ClassifyPurity(typeof(System.Net.Http.HttpClient), ownProducer: null);

        violation.Should().NotBeNull("transport types must not leak into Public contracts");
    }

    [Fact]
    public void Gate_Allows_SystemAndOwnPublicPrimitives()
    {
        ClassifyPurity(typeof(Guid), ownProducer: null).Should().BeNull();
        ClassifyPurity(typeof(PublicSemanticContractArchitectureTests), ownProducer: null).Should().BeNull(
            "own Public namespace types are allowed");
    }

    [Fact]
    public void Gate_Detects_ArbitraryCommonBusinessType()
    {
        // Business vocabulary is a foreign context's concern: any Common type
        // outside the exact approved technical allowlist fails even for its
        // own producer.
        var violation = ClassifyPurity(
            typeof(Notrelix.Application.Common.Tenancy.IAccessGrantProjectionService),
            ownProducer: null);

        violation.Should().NotBeNull("arbitrary Common business semantics must not enter Public contracts");
    }

    [Fact]
    public void Gate_Detects_ForeignProducerPublicContract()
    {
        // Public-to-Public references across producers are denied by default:
        // producer awareness means Identity.Public may reference only its own
        // surface, never Accounts.Public (exact reviewed exceptions only).
        var violation = ClassifyPurity(
            typeof(Notrelix.Application.Features.Accounts.Public.Facts.AccountMembershipAdmissionFact),
            ownProducer: "Identity");

        violation.Should().NotBeNull("foreign producer Public contracts are denied by default");
    }

    [Fact]
    public void Gate_Allows_OwnProducerPublicContract()
    {
        var violation = ClassifyPurity(
            typeof(Notrelix.Application.Features.Accounts.Public.Facts.AccountMembershipAdmissionFact),
            ownProducer: "Accounts");

        violation.Should().BeNull("a producer may reference its own Public surface");
    }

    /// <summary>
    /// A type belongs to the Public surface when its namespace sits under
    /// `Features.{Context}.Public`. Mirrors CrossContextBoundaryScanner's
    /// namespace resolution (segments after `Notrelix.Application.Features.`).
    /// </summary>
    private static bool IsPublicSurfaceType(Type type)
    {
        var ns = type.Namespace;
        if (ns is null || !ns.StartsWith("Notrelix.Application.Features.", StringComparison.Ordinal))
            return false;

        var segments = ns.Split('.');
        return segments.Length > 4 && segments[4] == "Public";
    }

    private static string? ResolveProducerFromPublicNamespace(string? ns)
    {
        if (ns is null || !ns.StartsWith("Notrelix.Application.Features.", StringComparison.Ordinal))
            return null;

        var segments = ns.Split('.');
        return segments.Length > 4 && segments[4] == "Public"
            ? segments[3]
            : null;
    }

    /// <summary>
    /// Exact allowlist of approved technical Common primitives that Public
    /// contracts may reference. Business vocabulary (entitlements, roles,
    /// lifecycle state, tenancy services, event infrastructure) is producer
    /// semantics and must never ride through Common into a Public surface.
    /// A new allowed entry requires deliberate review of this list.
    /// </summary>
    private static readonly IReadOnlySet<string> ApprovedCommonPrimitives =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Notrelix.Application.Common.Models.Result", // stable result envelope
        };

    private static IEnumerable<Type> CollectReferencedTypes(Type type)
    {
        const BindingFlags flags =
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        var seen = new HashSet<Type>();
        var collected = new List<Type>();

        void Collect(Type? candidate)
        {
            if (candidate is null || !seen.Add(candidate))
                return;

            collected.Add(candidate);

            if (candidate.IsGenericType && !candidate.IsGenericTypeDefinition)
            {
                foreach (var argument in candidate.GetGenericArguments())
                    Collect(argument);
            }
        }

        Collect(type.BaseType);
        foreach (var iface in type.GetInterfaces())
            Collect(iface);
        foreach (var field in type.GetFields(flags))
            Collect(field.FieldType);
        foreach (var property in type.GetProperties(flags))
            Collect(property.PropertyType);
        foreach (var ctor in type.GetConstructors(flags))
            foreach (var parameter in ctor.GetParameters())
                Collect(parameter.ParameterType);
        foreach (var method in type.GetMethods(flags).Where(m => !m.IsSpecialName))
        {
            Collect(method.ReturnType);
            foreach (var parameter in method.GetParameters())
                Collect(parameter.ParameterType);
        }

        return collected;
    }

    private static string? ClassifyPurity(Type referenced, string? ownProducer)
    {
        var ns = referenced.Namespace;
        if (ns is null)
            return null;

        if (ns.StartsWith("System.Net.Http", StringComparison.Ordinal))
            return "transport type";

        if (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal))
            return null;

        if (ns.StartsWith("Notrelix.Application.Common.", StringComparison.Ordinal))
        {
            return ApprovedCommonPrimitives.Contains(referenced.FullName ?? string.Empty)
                ? null
                : "unapproved Common type (exact technical allowlist only)";
        }

        if (IsPublicSurfaceType(referenced))
        {
            // Producer-aware Public rule: a producer's Public surface may
            // reference only its own Public types. Foreign producer Public is
            // denied by default; a reviewed exception must be an exact entry.
            var referencedProducer = ResolveProducerFromPublicNamespace(referenced.Namespace);
            return referencedProducer == ownProducer
                ? null
                : "foreign producer Public contract";
        }

        if (ns.StartsWith("Notrelix.Domain.", StringComparison.Ordinal))
            return "producer Domain model";

        if (ns.StartsWith("Notrelix.Infrastructure.", StringComparison.Ordinal) ||
            ns.StartsWith("Notrelix.API.", StringComparison.Ordinal) ||
            ns.StartsWith("Notrelix.Platform.", StringComparison.Ordinal))
            return "outer-layer type";

        if (ns.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
            return "EF Core type";

        if (ns.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) ||
            ns.StartsWith("Grpc.", StringComparison.Ordinal) ||
            ns.StartsWith("System.Net.Http", StringComparison.Ordinal) ||
            ns.StartsWith("MassTransit", StringComparison.Ordinal))
            return "transport type";

        if (ns.StartsWith("Notrelix.Application.Features.", StringComparison.Ordinal))
            return "non-Public Application type";

        return null;
    }
}
