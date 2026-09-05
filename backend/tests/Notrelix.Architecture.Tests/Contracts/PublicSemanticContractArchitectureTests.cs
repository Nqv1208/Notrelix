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
            violations.AddRange(EvaluatePublicContract(type));
        }

        violations.Should().BeEmpty(
            "ARCH-BC-005: producer Public contracts must be small immutable semantic " +
            "surfaces. Violations:\n" + string.Join("\n", violations));
    }

    /// <summary>
    /// Evaluates a single Public contract against the ARCH-BC-005 purity rules,
    /// classifying the contract's own surface first and then each dependency it
    /// references. The production gate and the structural self-tests share this
    /// exact traversal, so a fixture's failure proves the production algorithm
    /// (not just a private classifier called in isolation).
    /// </summary>
    private static IEnumerable<string> EvaluatePublicContract(Type type)
    {
        var ownProducer = ResolveProducerFromPublicNamespace(type.Namespace);

        // Self-surface first: classify the Public contract type itself so a
        // persistence-leaking signature (IQueryable / DbSet / EF) is rejected
        // structurally, not just its referenced dependencies.
        if (ClassifyPurity(type, ownProducer) is { } selfReason)
            yield return $"{type.FullName}: {selfReason} (contract surface)";

        // Then each dependency the contract references.
        foreach (var referenced in CollectReferencedTypes(type))
        {
            if (ClassifyPurity(referenced, ownProducer) is { } reason)
                yield return $"{type.FullName}: {reason} ({referenced.FullName})";
        }
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
        // The real current data-session mechanism identity. Classified via the
        // exact reviewed identity + Common-branch rejection, not a stale FQN.
        ClassifyPurity(typeof(Notrelix.Application.Common.Data.IRequestDataSession), ownProducer: null)
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
                typeof(Notrelix.Application.Features.Accounts.Public.Membership.AccountMembershipAdmissionFact),
                ownProducer: "Identity")
            .Should().NotBeNull("foreign producer Public contracts are denied by default");
    }

    [Fact]
    public void Gate_Allows_OwnProducerPublicContract()
    {
        ClassifyPurity(
                typeof(Notrelix.Application.Features.Accounts.Public.Membership.AccountMembershipAdmissionFact),
                ownProducer: "Accounts")
            .Should().BeNull("a producer may reference its own Public surface");
    }

    // TAC-M3A / P4 — repository/mechanism rejection independent of own-Public
    // allowance. ARCH-BC-005 hardening: a repository/mechanism contract placed on
    // the producer's own Public surface must FAIL via structural classification,
    // not pass through the own-Public allowance and not be lexical.
    //
    // The decisive negative proof uses the real-`Type` path: the isolated fixture
    // (PublicSurfaceSmugglingFixture) stays logically on the own Producer.Public
    // namespace, is named WITHOUT mechanism words, and is rejected because its
    // signature surfaces a strong persistence mechanism (IQueryable / DbSet).

    [Fact]
    public void Gate_Detects_OwnProducer_Public_RepositoryMechanism()
    {
        // Real-Type path through the SAME algorithm the production gate uses
        // (EvaluatePublicContract classifies the contract surface first, then
        // dependencies). Rejection is structural (surface/signature evidence),
        // not caused by the fixture's name; the fixture's name contains no
        // mechanism words.
        EvaluatePublicContract(
                typeof(Notrelix.Application.Features.Accounts.Public.IAccountReadWriteSurface))
            .Should().NotBeEmpty(
                "repository/mechanism contracts must be rejected on the own Producer.Public " +
                "semantic surface even though they are own-Public; rejection must be caused " +
                "by structural persistence-surface evidence, not by mechanism nouns in the name");
    }

    [Fact]
    public void Gate_Allows_OwnProducer_Public_SemanticSurfaces()
    {
        // Legitimate own-Public semantic surfaces (actions/facts/queries) must pass.
        ClassifyPurity(
                typeof(Notrelix.Application.Features.Accounts.Public.Membership.IAccountMembershipActions),
                ownProducer: "Accounts")
            .Should().BeNull("an own-Public action surface is a semantic contract");
        ClassifyPurity(
                typeof(Notrelix.Application.Features.Accounts.Public.Membership.IAccountMembershipFacts),
                ownProducer: "Accounts")
            .Should().BeNull("an own-Public read-query surface is a semantic contract");
    }

    [Fact]
    public void Gate_Allows_OwnProducer_Public_EnumerableQuerySurface()
    {
        // A semantic read surface returning a plain enumerable must NOT be treated
        // as persistence. Task<IReadOnlyList<T>> / IEnumerable<T> are legitimate
        // semantic result shapes and are not strong persistence evidence.
        ClassifyPurity(
                typeof(Notrelix.Application.Features.WorkManagement.Public.Queries.IWorkItemProjectionSource),
                ownProducer: "WorkManagement")
            .Should().BeNull(
                "a semantic enumerable read-query surface is not persistence and must pass");
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

    /// <summary>
    /// Exact full-type identities of approved persistence/session abstractions.
    /// A type whose own-Public surface is literally one of these (or derives
    /// from one) is a mechanism contract. These are exact reviewed identities,
    /// not name-substring tokens.
    /// </summary>
    private static readonly IReadOnlySet<string> ReviewedMechanismIdentities =
        CrossContextBoundaryScanner.ContextDbContextInterface.Values
            .ToHashSet(StringComparer.Ordinal);

    private const string RequestDataSessionFullName =
        "Notrelix.Application.Common.Data.IRequestDataSession";

    /// <summary>
    /// True when the referenced own-Public type is structurally a
    /// repository/mechanism contract. This is the ARCH-BC-005 hardening path:
    /// it runs only inside the own-Public allowance branch and uses structural
    /// evidence, never a name-substring heuristic.
    ///
    /// Orphaned lexically common "repository/store/session" words are NOT
    /// evidence; only a persistence-leaking structure is rejected.
    /// </summary>
    private static bool IsStructuralMechanismContract(Type type)
    {
        if (HasPersistenceSurface(type))
            return true;

        if (ReviewedMechanismIdentities.Contains(type.FullName ?? string.Empty))
            return true;

        return false;
    }

    /// <summary>
    /// A referenced type exposes a persistence mechanism when its own surface,
    /// or the surface of any base/interface it derives from, references a strong
    /// persistence primitive (IQueryable, DbSet / EF-rooted type, a known
    /// data-session abstraction, or an exact owned DbContext abstraction).
    /// Generic enumerables (IEnumerable/IReadOnlyList/IAsyncEnumerable) and
    /// plain method names (Add/Update/Save) are deliberately NOT strong evidence.
    /// </summary>
    private static bool HasPersistenceSurface(Type type)
    {
        if (HasStrongPersistenceAncestry(type))
            return true;

        foreach (var iface in type.GetInterfaces())
        {
            if (HasStrongPersistenceAncestry(iface))
                return true;
        }

        foreach (var method in type.GetMethods(ReflectionFlags).Where(m => !m.IsSpecialName))
        {
            if (IsStrongPersistenceType(method.ReturnType))
                return true;

            foreach (var parameter in method.GetParameters())
            {
                if (IsStrongPersistenceType(parameter.ParameterType))
                    return true;
            }
        }

        foreach (var property in type.GetProperties(ReflectionFlags))
        {
            if (IsStrongPersistenceType(property.PropertyType))
                return true;
        }

        return false;
    }

    private static bool HasStrongPersistenceAncestry(Type type)
    {
        if (IsStrongPersistenceType(type))
            return true;

        if (type.BaseType is { } baseType && HasStrongPersistenceAncestry(baseType))
            return true;

        foreach (var iface in type.GetInterfaces())
        {
            if (HasStrongPersistenceAncestry(iface))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Strong persistence evidence: IQueryable, EF/DbContext-rooted types, the
    /// known data-session abstraction, or an exact owned DbContext abstraction.
    /// Generic enumerables and plain persistence-shaped method names are not.
    /// </summary>
    private static bool IsStrongPersistenceType(Type type)
    {
        var candidate = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

        if (candidate.FullName == "System.Linq.IQueryable`1")
            return true;

        var ns = candidate.Namespace;
        if (ns is null)
            return false;

        if (ns.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
            return true;

        if (candidate.FullName == RequestDataSessionFullName)
            return true;

        return ReviewedMechanismIdentities.Contains(candidate.FullName ?? string.Empty);
    }

    private const BindingFlags ReflectionFlags =
        BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static IEnumerable<Type> CollectReferencedTypes(Type type)
    {
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
        foreach (var field in type.GetFields(ReflectionFlags))
            Collect(field.FieldType);
        foreach (var property in type.GetProperties(ReflectionFlags))
            Collect(property.PropertyType);
        foreach (var ctor in type.GetConstructors(ReflectionFlags))
            foreach (var parameter in ctor.GetParameters())
                Collect(parameter.ParameterType);
        foreach (var method in type.GetMethods(ReflectionFlags).Where(m => !m.IsSpecialName))
        {
            Collect(method.ReturnType);
            foreach (var parameter in method.GetParameters())
                Collect(parameter.ParameterType);
        }

        return collected;
    }

    private static string? ClassifyPurity(Type referenced, string? ownProducer)
        => ClassifyPurity(referenced, referenced.Namespace, referenced.FullName, ownProducer);

    /// <summary>
    /// String overload for self-tests over type identities that are not
    /// referenced by this test project (Platform, provider SDKs). No real-`Type`
    /// surface is available, so structural persistence-surface detection cannot
    /// run on this path (it is not the M3A own-Public closure proof).
    /// </summary>
    private static string? ClassifyPurity(string typeFullName, string? ownProducer)
    {
        var separator = typeFullName.LastIndexOf('.');
        var ns = separator > 0 ? typeFullName[..separator] : null;
        return ClassifyPurity(type: null, ns, typeFullName, ownProducer);
    }

    /// <summary>
    /// Classification core over (namespace, fullName) so self-tests can
    /// exercise types that are not referenced by this test project
    /// (Platform, provider SDKs) while preserving exact-type Common
    /// allowlist semantics. <paramref name="type"/> carries the real CLR type
    /// when available so the own-Public mechanism classifier can inspect the
    /// signature surface for structural persistence evidence.
    /// </summary>
    private static string? ClassifyPurity(Type? type, string? ns, string? fullName, string? ownProducer)
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
            if (referencedProducer != ownProducer)
                return "foreign producer Public contract";

            // ARCH-BC-005 hardening (TAC-M3A / P4): own Producer.Public is the
            // approved semantic surface. Repository/mechanism contracts must
            // be rejected here independently of the own-Public allowance — a
            // producer must not smuggle a persistence/session/DbContext/repository
            // mechanism onto its Public surface under the cover of "own Public".
            // Detection is structural (real-Type surface inspection primary;
            // exact reviewed mechanism identities as defense), never lexical.
            if (type is not null && IsStructuralMechanismContract(type))
                return "repository/mechanism on own Producer.Public semantic surface";

            return null;
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
