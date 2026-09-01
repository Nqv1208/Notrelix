using System.Reflection;

namespace Notrelix.Architecture.Tests.ApplicationLayer;

/// <summary>
/// ARCH-BC-006 — Application Transport / Provider Purity (boundary Wave 2).
///
/// Application business code (the whole Notrelix.Application assembly) must not
/// depend on runtime/network/provider client types. Transport and provider
/// mechanics belong to Infrastructure adapters behind Application-owned ports.
///
/// Approved exception EX-BE-APP-EF-001 (Microsoft.EntityFrameworkCore package
/// reference in Application) is intentionally NOT flagged — this gate targets
/// transport/provider mechanisms, not the canonical EF compatibility exception.
/// Domain transport/framework purity is separately enforced by
/// DomainPurity/DomainFrameworkDependencyTests and is not duplicated here.
/// </summary>
public class ApplicationTransportBoundaryTests
{
    /// <summary>
    /// Forbidden namespace roots: HTTP/gRPC transports, broker clients,
    /// database/Redis low-level clients, provider SDKs. Extend only with a
    /// reviewed reason — never to bless an existing violation.
    /// </summary>
    private static readonly IReadOnlyList<string> ForbiddenNamespaceRoots =
    [
        "System.Net.Http",
        "Grpc",
        "Grpc.Core",
        "Grpc.Net.Client",
        "MassTransit",
        "RabbitMQ.Client",
        "Npgsql",
        "StackExchange.Redis",
        "Stripe",
        "SendGrid",
        "Twilio",
        "Microsoft.Graph",
        "Google.Apis",
        "Amazon",
        "Azure.Messaging",
        "Azure.Storage",
    ];

    [Fact]
    public void ApplicationTypes_ShouldNotDependOn_TransportOrProviderClients()
    {
        var violations = Assembly.Load("Notrelix.Application")
            .GetTypes()
            .SelectMany(type => CollectReferencedTypes(type)
                .Select(referenced => (Type: referenced, Reason: ClassifyTransportDependency(referenced)))
                .Where(x => x.Reason is not null)
                .Select(x => $"{type.FullName}: {x.Reason} ({x.Type.FullName})"))
            .Distinct()
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        violations.Should().BeEmpty(
            "ARCH-BC-006: Application code must depend on semantic ports; Infrastructure " +
            "owns transport/provider clients and adapters. Violations:\n" +
            string.Join("\n", violations));
    }

    // ------------------------------------------------------------------
    // Gate self-tests
    // ------------------------------------------------------------------

    [Fact]
    public void Gate_Detects_HttpClientDependency()
    {
        var violation = ClassifyTransportDependency(typeof(System.Net.Http.HttpClient));

        violation.Should().NotBeNull("HttpClient is an Infrastructure-owned transport mechanism");
    }

    [Fact]
    public void Gate_Detects_ProviderSdkDependency()
    {
        var violation = ClassifyTransportDependencyByNamespace("Stripe.Checkout");

        violation.Should().NotBeNull("provider SDK types must not enter Application");
        ClassifyTransportDependencyByNamespace("StackExchange.Redis")
            .Should().NotBeNull("low-level Redis client must not enter Application");
    }

    [Fact]
    public void Gate_Allows_SemanticPortsAndApprovedEfException()
    {
        ClassifyTransportDependency(typeof(Microsoft.EntityFrameworkCore.DbContext)).Should().BeNull(
            "EX-BE-APP-EF-001: EF Core package compatibility is a governed exception, not transport leakage");
        ClassifyTransportDependency(typeof(System.Exception)).Should().BeNull();
    }

    private static string? ClassifyTransportDependency(Type referenced)
        => ClassifyTransportDependencyByNamespace(referenced.Namespace);

    private static string? ClassifyTransportDependencyByNamespace(string? ns)
    {
        if (ns is null)
            return null;

        foreach (var root in ForbiddenNamespaceRoots)
        {
            if (ns == root || ns.StartsWith(root + ".", StringComparison.Ordinal))
                return "transport/provider client";
        }

        return null;
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

    private sealed class FixtureWithHttpClient
    {
        public System.Net.Http.HttpClient? Client { get; set; }
    }
}

