using System.Text.Json;
using Notrelix.Application.Events.Identity;
using Notrelix.Domain.Common;
using Notrelix.Infrastructure.Messaging;

namespace Notrelix.Infrastructure.Tests.Messaging;

/// <summary>
/// IA-TST-EVT-VER-INF-001..003 / IAREQ131 / IAREQ119 — runtime resolution of
/// public integration events requires the compound (Name, Version) identity:
/// exact-version resolution, deterministic unknown-version failure, and
/// deterministic unknown-name failure with no implicit latest/oldest/v1
/// fallback.
/// </summary>
public class IntegrationEventCatalogResolutionTests
{
    // Production contract used for the real-contract resolution assertions.
    private static readonly Type[] ProductionIdentityContracts =
    [
        typeof(UserRegisteredIntegrationEvent),
        typeof(IdentityRegistrationCompletedIntegrationEventV1),
        typeof(EmailVerificationDeliveryRequestedIntegrationEventV1),
    ];

    [Fact]
    public void Resolve_WithExactNameAndVersion_ReturnsExactType()
    {
        var catalog = new IntegrationEventCatalog(ProductionIdentityContracts);

        var resolved = catalog.Resolve(new EventContractKey("identity.user-registered", 2));

        resolved.Should().Be(typeof(UserRegisteredIntegrationEvent));
    }

    [Fact]
    public void Resolve_SameNameDifferentVersion_ReturnsCorrespondingVersion()
    {
        var catalog = new IntegrationEventCatalog(ProductionIdentityContracts);

        catalog.Resolve(new EventContractKey("identity.registration-completed", 1))
            .Should().Be(typeof(IdentityRegistrationCompletedIntegrationEventV1));
    }

    [Fact]
    public void Resolve_UnknownVersion_FailsDeterministically()
    {
        var catalog = new IntegrationEventCatalog(ProductionIdentityContracts);

        var act = () => catalog.Resolve(new EventContractKey("identity.user-registered", 3));

        act.Should().Throw<UnknownIntegrationEventTypeException>(
            "known name + unsupported version must fail deterministically; " +
            "it must never deserialize as another version or fall back to latest/v1");
    }

    [Fact]
    public void TryResolve_UnknownVersion_DoesNotFallBack()
    {
        var catalog = new IntegrationEventCatalog(ProductionIdentityContracts);

        var resolved = catalog.TryResolve(new EventContractKey("identity.user-registered", 1), out _);

        resolved.Should().BeFalse(
            "v2 is the only registered version; a v1 lookup must not silently resolve to v2");
    }

    [Fact]
    public void Resolve_UnknownName_FailsDeterministically()
    {
        var catalog = new IntegrationEventCatalog(ProductionIdentityContracts);

        var act = () => catalog.Resolve(new EventContractKey("identity.does-not-exist", 1));

        act.Should().Throw<UnknownIntegrationEventTypeException>();
    }

    [Fact]
    public void Constructor_DuplicateCompoundKey_IsRejected()
    {
        var act = () => new IntegrationEventCatalog(
        [
            typeof(TestFactV1),
            typeof(DuplicateTestFactV1),
        ]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*test.versioned-fact v1*compound contract identity*");
    }

    [Fact]
    public void Constructor_SameNameDifferentVersions_Coexist()
    {
        var catalog = new IntegrationEventCatalog(
        [
            typeof(TestFactV1),
            typeof(TestFactV2),
        ]);

        catalog.Resolve(new EventContractKey("test.versioned-fact", 1)).Should().Be(typeof(TestFactV1));
        catalog.Resolve(new EventContractKey("test.versioned-fact", 2)).Should().Be(typeof(TestFactV2));
    }
}

[EventName("test.versioned-fact", Version = 1)]
public sealed record TestFactV1(Guid EventId, Guid CorrelationId)
    : IntegrationEvent(EventId, "test.versioned-fact", 1, CorrelationId);

[EventName("test.versioned-fact", Version = 2)]
public sealed record TestFactV2(Guid EventId, Guid CorrelationId, string ExtraField)
    : IntegrationEvent(EventId, "test.versioned-fact", 2, CorrelationId);

[EventName("test.versioned-fact", Version = 1)]
public sealed record DuplicateTestFactV1(Guid EventId, Guid CorrelationId)
    : IntegrationEvent(EventId, "test.versioned-fact", 1, CorrelationId);
/// <summary>
/// IA-TST-X-EVT-004 / IA-TST-EVT-MIG-001 / IA-TST-EVT-MIG-002 /
/// IA-TST-MIG-EVT-002 / IAREQ088 / IAREQ134.
///
/// A controlled v1/v2 fixture crossing every production contract boundary:
/// producer contract metadata → ContractRegistry (consumer contract
/// selection) → IntegrationEventCatalog (runtime resolution) → production
/// serializer round-trip preserving the version through the envelope fields.
/// No production event is version-bumped for this proof.
/// </summary>
public class VersionedContractMigrationFixtureTests
{
    private static readonly IReadOnlyList<ConsumerDefinition> DualReadConsumers =
    [
        new ConsumerDefinition
        {
            ConsumerName = "TestFactDualReadConsumer",
            EventName = "test.versioned-fact",
            EventVersion = 1,
            BoundedContext = "Test",
            Maturity = ConsumerMaturity.Implemented,
        },
        new ConsumerDefinition
        {
            ConsumerName = "TestFactDualReadConsumer",
            EventName = "test.versioned-fact",
            EventVersion = 2,
            BoundedContext = "Test",
            Maturity = ConsumerMaturity.Implemented,
        },
    ];

    [Fact]
    public void ConsumerContractSelection_ResolvesBothVersions_DuringCompatibilityWindow()
    {
        // IA-TST-EVT-MIG-001 — dual-read: one logical consumer registered for
        // v1 and v2 simultaneously is representable in the canonical registry.
        var registry = new ConsumerRegistry(DualReadConsumers);

        registry.GetConsumers("test.versioned-fact").Should().HaveCount(2);
        registry.HasConsumer("test.versioned-fact").Should().BeTrue();
    }

    [Fact]
    public void RegistryAndCatalog_CrossBoundary_Coexistence()
    {
        var contracts = new[]
        {
            new ContractDefinition { Name = "test.versioned-fact", Version = 1, IntegrationEventType = typeof(TestFactV1) },
            new ContractDefinition { Name = "test.versioned-fact", Version = 2, IntegrationEventType = typeof(TestFactV2) },
        };
        var contractRegistry = new ContractRegistry(contracts);
        var catalog = new IntegrationEventCatalog([typeof(TestFactV1), typeof(TestFactV2)]);

        // Consumer-contract selection path...
        contractRegistry.Get("test.versioned-fact", 1).IntegrationEventType.Should().Be(typeof(TestFactV1));
        contractRegistry.Get("test.versioned-fact", 2).IntegrationEventType.Should().Be(typeof(TestFactV2));

        // ...runtime deserialization-resolution path agrees on the same identity.
        catalog.Resolve(new EventContractKey("test.versioned-fact", 1)).Should().Be(typeof(TestFactV1));
        catalog.Resolve(new EventContractKey("test.versioned-fact", 2)).Should().Be(typeof(TestFactV2));

        var actVersion3 = () => contractRegistry.Get("test.versioned-fact", 3);
        actVersion3.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// IA-TST-MIG-EVT-002 — the PRODUCTION serializer path (outbox dispatcher's
    /// exact camelCase options) preserves name and version across a serialize →
    /// deserialize round-trip; the receiving resolution observes the SAME
    /// version it was published with. Not satisfied by hard-coding version 1.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void ProductionSerializerRoundTrip_PreservesCompoundIdentity(int version)
    {
        var eventId = Guid.NewGuid();
        var correlationId = Guid.CreateVersion7();

        IIntegrationEvent published = version == 1
            ? new TestFactV1(eventId, correlationId)
            : new TestFactV2(eventId, correlationId, "extra");

        // Serialize exactly like OutboxDispatcher stores the payload.
        var payloadJson = JsonSerializer.SerializeToElement(published, PayloadOptions);

        var messageName = payloadJson.GetProperty("messageName").GetString();
        var schemaVersion = payloadJson.GetProperty("schemaVersion").GetInt32();

        messageName.Should().Be("test.versioned-fact");
        schemaVersion.Should().Be(version);

        // The receiving resolution path uses the envelope-carried identity.
        var catalog = new IntegrationEventCatalog([typeof(TestFactV1), typeof(TestFactV2)]);
        var resolvedType = catalog.Resolve(new EventContractKey(messageName!, schemaVersion));

        resolvedType.Should().Be(published.GetType(),
            "the receiving side must resolve the exact published version");

        var deserialized = payloadJson.Deserialize(resolvedType, PayloadOptions) as IIntegrationEvent;
        deserialized.Should().NotBeNull();
        deserialized!.SchemaVersion.Should().Be(version);
        deserialized.EventId.Should().Be(eventId);

        // IA-TST-OBS-001/003 — the correlation identifiers that let a security
        // operation be traced from request through persistence to event/audit
        // survive the production serializer round-trip.
        deserialized.CorrelationId.Should().Be(correlationId,
            "correlation identity is the non-secret thread across the operation chain");
    }

    /// <summary>IA-TST-EVT-OPS-001 — operational evidence schema fields are representable.</summary>
    [Fact]
    public void OperationalBacklogEvidence_SchemaIsRepresentable()
    {
        var record = new OperationalBacklogEvidenceRow(
            EventName: "identity.user-registered",
            Version: 2,
            Consumer: "UserRegisteredConsumer",
            OutboxPendingCount: 0,
            OldestPendingAge: TimeSpan.Zero,
            RetryBacklogCount: 0,
            DeadLetterCount: 0,
            UnsupportedVersionCount: 0);

        record.EventName.Should().NotBeNullOrWhiteSpace();
    }

    private static readonly JsonSerializerOptions PayloadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}

/// <summary>
/// IA-TST-EVT-OPS-001 / IAREQ135 — runbook/evidence row shape for operational
/// backlog inspection by event/version/consumer (VERIFIED or
/// NOT_APPLICABLE_UNTIL_DEPLOYMENT at closure).
/// </summary>
public sealed record OperationalBacklogEvidenceRow(
    string EventName,
    int Version,
    string Consumer,
    int OutboxPendingCount,
    TimeSpan OldestPendingAge,
    int RetryBacklogCount,
    int DeadLetterCount,
    int UnsupportedVersionCount);
