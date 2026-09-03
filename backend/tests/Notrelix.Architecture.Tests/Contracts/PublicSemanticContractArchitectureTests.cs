using System.Reflection;

namespace Notrelix.Architecture.Tests.Contracts;

/// <summary>
/// ARCH-BC-005 — Public Semantic Contract Purity (boundary Wave 2).
///
/// Types under `Application/Features/{Producer}/Public/**` are the approved
/// cross-context semantic surface. They must stay semantic and transport
/// neutral:
///   allowed roots — System.*, exact approved technical Common primitives,
///   the producer's own Public namespace;
///   forbidden — producer Domain models, other contexts' internal types,
///   Infrastructure/API/Platform types, EF Core, transport (ASP.NET, gRPC,
///   HTTP clients), broker clients and provider SDK types.
///
/// Zero Public surfaces is valid: a context with no cross-context consumer
/// must not create the folder (BND-PUB-001). The gate activates automatically
/// as soon as a Public type exists.
///
/// Policy note: the provider/SDK roots below are this gate's own local policy
/// (gate owner ARCH-BC-005). They intentionally mirror the transport/provider
/// roots enforced for the whole Application assembly by ARCH-BC-006
/// (ApplicationTransportBoundaryTests) without sharing code — each gate owns
/// its rule.
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
    // Gate self-tests — TAC-GATE-003 forbidden matrix
    // ------------------------------------------------------------------

    // Domain aggregate → FAIL

    [Fact]
    public void Gate_Detects_DomainAggregateInsidePublicContract()
    {
        ClassifyPurity(typeof(Notrelix.Domain.WorkManagement.Boards.Board), ownProducer: null)
            .Should().NotBeNull("producer Domain aggregates must not leak into Public contracts");
    }

    // Domain enum → FAIL

    [Fact]
    public void Gate_Detects_DomainEnumInsidePublicContract()
    {
        ClassifyPurity(typeof(Notrelix.Domain.Integrations.Connections.IntegrationProvider), ownProducer: null)
            .Should().NotBeNull("producer Domain enums must not leak into Public contracts");
    }

    // DbContext → FAIL

    [Fact]
    public void Gate_Detects_DbContextInsidePublicContract()
    {
        ClassifyPurity(typeof(Microsoft.EntityFrameworkCore.DbContext), ownProducer: null)
            .Should().NotBeNull("EF Core DbContext must not leak into Public contracts");
    }

    // repository → FAIL

    [Fact]
    public void Gate_Detects_RepositoryInsidePublicContract()
    {
        ClassifyPurity("Notrelix.Application.Common.Requests.Transactions.IRequestDataSession", ownProducer: null)
            .Should().NotBeNull("repository/session mechanisms must not leak into Public contracts");
    }

    // Infrastructure / Platform / API type → FAIL

    [Fact]
    public void Gate_Detects_InfrastructureTypeInsidePublicContract()
    {
        ClassifyPurity("Notrelix.Infrastructure.Billing.DatabaseFeatureGateChecker", ownProducer: null)
            .Should().NotBeNull("Infrastructure types must not leak into Public contracts");
    }

    [Fact]
    public void Gate_Detects_PlatformTypeInsidePublicContract()
    {
        ClassifyPurity("Notrelix.Platform.Messaging.ConsumerHost", ownProducer: null)
            .Should().NotBeNull("Platform types must not leak into Public contracts");
    }

    [Fact]
    public void Gate_Detects_ApiTypeInsidePublicContract()
    {
        ClassifyPurity("Notrelix.API.Endpoints.Health.HealthEndpoint", ownProducer: null)
            .Should().NotBeNull("API types must not leak into Public contracts");
    }

    // EF type → FAIL

    [Fact]
    public void Gate_Detects_EfCoreTypeInsidePublicContract()
    {
        ClassifyPurity("Microsoft.EntityFrameworkCore.EntityTypeBuilder", ownProducer: null)
            .Should().NotBeNull("EF Core types must not leak into Public contracts");
    }

    // transport / broker / provider SDK → FAIL

    [Fact]
    public void Gate_Detects_HttpClientInsidePublicContract()
    {
        ClassifyPurity(typeof(System.Net.Http.HttpClient), ownProducer: null)
            .Should().NotBeNull("transport types must not leak into Public contracts");
    }

    [Fact]
    public void Gate_Detects_MassTransitInsidePublicContract()
    {
        ClassifyPurity("MassTransit.IBus", ownProducer: null)
            .Should().NotBeNull("broker clients must not leak into Public contracts");
    }

    [Fact]
    public void Gate_Detects_GrpcInsidePublicContract()
    {
        ClassifyPurity("Grpc.Core.CallOptions", ownProducer: null)
            .Should().NotBeNull("gRPC types must not leak into Public contracts");
    }

    [Theory]
    [InlineData("Stripe.Checkout.SessionService")]
    [InlineData("SendGrid.Helpers.Mail.MailHelper")]
    [InlineData("Twilio.Rest.Api.V2010.Account.MessageResource")]
    [InlineData("Microsoft.Graph.User")]
    [InlineData("Google.Apis.Calendar.v3.CalendarService")]
    [InlineData("Amazon.S3.AmazonS3Client")]
    [InlineData("Azure.Messaging.ServiceBus.ServiceBusClient")]
    [InlineData("Azure.Storage.Blobs.BlobClient")]
    public void Gate_Detects_ProviderSdkInsidePublicContract(string providerSdkTypeName)
    {
        ClassifyPurity(providerSdkTypeName, ownProducer: null)
            .Should().NotBeNull($"provider SDK type '{providerSdkTypeName}' must not leak into Public contracts");
    }

    // internal Application type → FAIL

    [Fact]
    public void Gate_Detects_InternalApplicationTypeInsidePublicContract()
    {
        ClassifyPurity("Notrelix.Application.Features.Workspaces.Members.Commands.AddMember.AddMemberCommand", ownProducer: null)
            .Should().NotBeNull("non-Public Application types must not leak into Public contracts");
    }

    // arbitrary Common → FAIL

    [Fact]
    public void Gate_Detects_ArbitraryCommonBusinessType()
    {
        // Business vocabulary is a foreign context's concern: any Common type
        // outside the exact approved technical allowlist fails even for its
        // own producer.
        ClassifyPurity(typeof(Notrelix.Application.Common.Entitlements.FeatureCode), ownProducer: null)
            .Should().NotBeNull("arbitrary Common business semantics must not enter Public contracts");
    }

    [Fact]
    public void Gate_Detects_UnapprovedCommonType_InSameNamespace_AsApprovedPrimitive()
    {
        // Exact-type semantics: the approved allowlist is per full type name,
        // not per namespace — another type in the same Common namespace fails.
        ClassifyPurity("Notrelix.Application.Common.Models.NotAResult", ownProducer: null)
            .Should().NotBeNull("the approved Common allowlist is exact-type, not namespace-wide");
    }

    // approved Common → PASS

    [Fact]
    public void Gate_Allows_ApprovedCommonPrimitive()
    {
        ClassifyPurity(typeof(Notrelix.Application.Common.Models.Result), ownProducer: null)
            .Should().BeNull("the stable result envelope is an approved technical Common primitive");
    }

    // System.* → PASS

    [Fact]
    public void Gate_Allows_SystemPrimitives()
    {
        ClassifyPurity(typeof(Guid), ownProducer: null).Should().BeNull();
        ClassifyPurity(typeof(System.Exception), ownProducer: null).Should().BeNull();
    }

    // foreign Public → FAIL / own Public → PASS (production Public types)

    [Fact]
    public void Gate_Detects_ForeignProducerPublicContract()
    {
        // Public-to-Public references across producers are denied by default:
        // producer awareness means Identity.Public may reference only its own
        // surface, never Accounts.Public (exact reviewed exceptions only).
        ClassifyPurity(
                typeof(Notrelix.Application.Features.Accounts.Public.Facts.AccountMembershipAdmissionFact),
                ownProducer: "Identity")
            .Should().NotBeNull("foreign producer Public contracts are denied by default");
    }

    [Fact]
    public void Gate_Allows_OwnProducerPublicContract()
    {
        ClassifyPurity(
                typeof(Notrelix.Application.Features.Accounts.Public.Facts.AccountMembershipAdmissionFact),
                ownProducer: "Accounts")
            .Should().BeNull("a producer may reference its own Public surface");
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

    /// <summary>
    /// Provider/SDK roots forbidden in Public contracts. Local policy of this
    /// gate (see class doc); mirrors ARCH-BC-006 without code sharing.
    /// </summary>
    private static readonly IReadOnlyList<string> ForbiddenProviderRoots =
    [
        "Stripe",
        "SendGrid",
        "Twilio",
        "Microsoft.Graph",
        "Google.Apis",
        "Amazon",
        "Azure.Messaging",
        "Azure.Storage",
    ];

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
        => ClassifyPurity(referenced.Namespace, referenced.FullName, ownProducer);

    /// <summary>
    /// String overload for self-tests over type identities that are not
    /// referenced by this test project (Platform, provider SDKs).
    /// </summary>
    private static string? ClassifyPurity(string typeFullName, string? ownProducer)
    {
        var separator = typeFullName.LastIndexOf('.');
        var ns = separator > 0 ? typeFullName[..separator] : null;
        return ClassifyPurity(ns, typeFullName, ownProducer);
    }

    /// <summary>
    /// Classification core over (namespace, fullName) so self-tests can
    /// exercise types that are not referenced by this test project
    /// (Platform, provider SDKs) while preserving exact-type Common
    /// allowlist semantics.
    /// </summary>
    private static string? ClassifyPurity(string? ns, string? fullName, string? ownProducer)
    {
        if (ns is null)
            return null;

        if (ns.StartsWith("System.Net.Http", StringComparison.Ordinal))
            return "transport type";

        if (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal))
            return null;

        if (ns.StartsWith("Notrelix.Application.Common.", StringComparison.Ordinal))
        {
            return ApprovedCommonPrimitives.Contains(fullName ?? string.Empty)
                ? null
                : "unapproved Common type (exact technical allowlist only)";
        }

        if (IsPublicSurfaceNamespace(ns))
        {
            // Producer-aware Public rule: a producer's Public surface may
            // reference only its own Public types. Foreign producer Public is
            // denied by default; a reviewed exception must be an exact entry.
            var referencedProducer = ResolveProducerFromPublicNamespace(ns);
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
            ns.StartsWith("Grpc", StringComparison.Ordinal) ||
            ns.StartsWith("MassTransit", StringComparison.Ordinal))
            return "transport type";

        foreach (var root in ForbiddenProviderRoots)
        {
            if (ns == root || ns.StartsWith(root + ".", StringComparison.Ordinal))
                return "provider SDK type";
        }

        if (ns.StartsWith("Notrelix.Application.Features.", StringComparison.Ordinal))
            return "non-Public Application type";

        return null;
    }

    private static bool IsPublicSurfaceNamespace(string ns)
    {
        if (!ns.StartsWith("Notrelix.Application.Features.", StringComparison.Ordinal))
            return false;

        var segments = ns.Split('.');
        return segments.Length > 4 && segments[4] == "Public";
    }
}
