using System.Reflection;
using Xunit;

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
            foreach (var referenced in CollectReferencedTypes(type))
            {
                if (ClassifyPurity(referenced) is { } reason)
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
        var violation = ClassifyPurity(typeof(Notrelix.Domain.WorkManagement.Boards.Board));

        violation.Should().NotBeNull("producer Domain aggregates must not leak into Public contracts");
    }

    [Fact]
    public void Gate_Detects_TransportTypeInsidePublicContract()
    {
        var violation = ClassifyPurity(typeof(System.Net.Http.HttpClient));

        violation.Should().NotBeNull("transport types must not leak into Public contracts");
    }

    [Fact]
    public void Gate_Allows_SystemAndOwnPublicPrimitives()
    {
        ClassifyPurity(typeof(Guid)).Should().BeNull();
        ClassifyPurity(typeof(Notrelix.Application.Common.Events.IntegrationEvent)).Should().BeNull();
        ClassifyPurity(typeof(PublicSemanticContractArchitectureTests)).Should().BeNull(
            "own Public namespace types are allowed");
    }

    /// <summary>
    /// A type belongs to the Public surface when its namespace sits under    /// `Features.{Context}.Public`. Mirrors CrossContextBoundaryScanner's
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

    private static string? ClassifyPurity(Type referenced)
    {
        var ns = referenced.Namespace;
        if (ns is null)
            return null;

        if (ns.StartsWith("System.Net.Http", StringComparison.Ordinal))
            return "transport type";

        if (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal))
            return null;

        if (ns.StartsWith("Notrelix.Application.Common.", StringComparison.Ordinal))
            return null;

        if (IsPublicSurfaceType(referenced))
            return null;

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
