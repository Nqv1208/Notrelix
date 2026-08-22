using System.Reflection;
using MassTransit;
using Notrelix.Application.Common.Events;
using Notrelix.Architecture.Tests.Events.Support;
using Notrelix.Domain.Common;
using Notrelix.Infrastructure.Messaging;

namespace Notrelix.Architecture.Tests.Events;

/// <summary>
/// Executable Phase 13 event-contract gates:
///
/// IA-TST-EVT-INV-ARCH-001..003 — source-complete inventory, explicit consumer
///   maturity, registry/actual consumer agreement.
/// IA-TST-EVT-SEC-001 — prohibited secret material absent from public payloads.
/// IA-TST-EVT-PRIV-001..002 — PII-bearing fields explicitly classified; stable-ID
///   events carry no unclassified personal data.
/// IA-TST-EVT-CONTRACT-001..004 — canonical manifest drift, same-version schema
///   freeze, production serializer naming, global scope/owner isolation.
/// IA-TST-MIG-EVT-001 — every production public-event resolution path passes
///   the compound (Name, Version); no name-only fallback remains.
/// IA-TST-X-EVT-002 — stub consumers are reported as STUB.
/// </summary>
public class PublicEventContractArchitectureTests : ArchitectureTestBase
{
    private static readonly IReadOnlyList<ContractDefinition> Contracts =
        ContractRegistrySetup.GetContractDefinitions();

    private static readonly IReadOnlyList<ConsumerDefinition> ConsumerDefs =
        ConsumerRegistrySetup.GetConsumerDefinitions();

    private static EventManifestModel Generated => EventManifestGenerator.Build();

    private static string GetRepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (current is not null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }

        var repoRoot = Path.GetDirectoryName(current!)
            ?? throw new DirectoryNotFoundException("Could not resolve repository root from backend.slnx location.");
        return repoRoot;
    }

    // ── Inventory ───────────────────────────────────────────────────────────

    /// <summary>IA-TST-EVT-INV-ARCH-001 / IAREQ084 / IAREQ085 / IAREQ133.</summary>
    [Fact]
    public void EveryPublicIntegrationEvent_IsManifestVisible_WithContractMetadata()
    {
        var integrationTypes = typeof(IIntegrationEvent).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(IIntegrationEvent).IsAssignableFrom(t))
            .ToList();

        integrationTypes.Should().NotBeEmpty("the public contract universe must be discoverable");

        var violations = new List<string>();
        var manifestByClrType = new Dictionary<string, ContractRow>();

        foreach (var row in Generated.Contracts)
        {
            manifestByClrType.TryAdd(row.ClrType, row);
        }

        foreach (var type in integrationTypes)
        {
            var clrType = type.FullName ?? type.Name;

            if (type.GetCustomAttribute<EventNameAttribute>() is null)
            {
                violations.Add($"{clrType}: missing [EventName] metadata");
                continue;
            }

            if (!manifestByClrType.TryGetValue(clrType, out var row))
            {
                violations.Add($"{clrType}: missing canonical manifest/inventory row");
                continue;
            }

            if (row.Version <= 0)
            {
                violations.Add($"{clrType}: manifest version must be positive");
            }
        }

        violations.Should().BeEmpty(
            "every public IIntegrationEvent must be inventory/manifest visible with name/version/producer. " +
            "A new event missing the manifest must fail until the canonical registry includes it: "
            + string.Join("; ", violations));
    }

    /// <summary>IA-TST-EVT-INV-ARCH-002 / IAREQ133 — maturity metadata is explicit.</summary>
    [Fact]
    public void ConsumerRegistry_MaturityIsExplicitForEveryEntry()
    {
        var invalidMaturity = ConsumerDefs
            .Where(c => c.Maturity is not (ConsumerMaturity.Implemented or ConsumerMaturity.Stub))
            .ToList();

        invalidMaturity.Should().BeEmpty(
            "no implicit/default consumer maturity value is accepted (IAREQ133)");

        ConsumerDefs.Select(c => c.ConsumerName).Should().OnlyHaveUniqueItems(
            "consumer identity in the canonical registry must be unique");

        foreach (var entry in ConsumerDefs.Where(e => e.Maturity == ConsumerMaturity.Implemented))
        {
            entry.Description.Should().NotBeNullOrWhiteSpace(
                $"{entry.ConsumerName}: implemented rows describe their business effect");
        }
    }

    /// <summary>
    /// IA-TST-EVT-INV-ARCH-003 / IA-TST-X-EVT-002 / IAREQ133 — a registered
    /// IMPLEMENTED consumer must be backed by an actual production IConsumer&lt;T&gt;
    /// consuming the exact (name, version); stub rows never satisfy this.
    /// </summary>
    [Fact]
    public void ImplementedRegistryRows_AreBackedByActualConsumerTypes_AtExactVersion()
    {
        var actualConsumersByEventKey = DiscoverActualConsumers()
            .SelectMany(x => x.EventKeys.Select(k => (k.Name, k.Version, x.ConsumerTypeName)))
            .GroupBy(x => (x.Name.ToLowerInvariant(), x.Version))
            .ToDictionary(g => g.Key, g => g.Select(x => x.ConsumerTypeName).ToList());

        var violations = new List<string>();

        foreach (var row in ConsumerDefs.Where(c => c.Maturity == ConsumerMaturity.Implemented))
        {
            var key = (row.EventName.ToLowerInvariant(), row.EventVersion);
            if (!actualConsumersByEventKey.TryGetValue(key, out var consumerTypes))
            {
                violations.Add(
                    $"'{row.ConsumerName}' claims IMPLEMENTED for '{row.EventName}' v{row.EventVersion} " +
                    "but no actual production IConsumer<T> consumes that exact contract version");
            }
        }

        violations.Should().BeEmpty(
            "registry presence alone does not prove an implemented downstream capability: "
            + string.Join("; ", violations));
    }

    /// <summary>IA-TST-X-EVT-002 — current stub consumers are reported as STUB, not IMPLEMENTED.</summary>
    [Fact]
    public void KnownPlaceholderConsumers_AreReportedAsStub()
    {
        const string stubConsumerName = "BoardRenamed";

        ConsumerDefs.Should().Contain(
            c => c.ConsumerName == stubConsumerName && c.Maturity == ConsumerMaturity.Stub,
            "the placeholder log-only consumer must be classified STUB in the canonical inventory");

        var manifestRow = Generated.Contracts
            .FirstOrDefault(c => c.Consumers.Any(x => x.Name == stubConsumerName));

        manifestRow.Should().NotBeNull();
        manifestRow!.Consumers.Single(x => x.Name == stubConsumerName).Maturity.Should().Be(nameof(ConsumerMaturity.Stub),
            "the generated inventory reports STUB, never IMPLEMENTED for a placeholder");
    }

    // ── Payload safety ──────────────────────────────────────────────────────

    private static readonly string[] ProhibitedSecretFragments =
    [
        "password",
        "passwordhash",
        "accesstoken",
        "refreshtoken",
        "clientsecret",
        "privatekey",
        "mfasecret",
        "totpsecret",
        "recoverycode",
        "apikey",
        "secretkey",
        "authorizationheader",
        "sessionsecret",
    ];

    /// <summary>
    /// IA-TST-EVT-SEC-001 / IAREQ086 — prohibited raw secret material is absent
    /// from every public integration-event serialized contract.
    /// </summary>
    [Fact]
    public void PublicEventPayloads_DoNotExposeProhibitedSecretMaterial()
    {
        var violations = new List<string>();

        foreach (var contract in Generated.Contracts)
        {
            foreach (var property in contract.SerializedProperties)
            {
                var normalized = property.Name.ToLowerInvariant();
                var prohibited = ProhibitedSecretFragments.Any(normalized.Contains);

                if (prohibited)
                {
                    violations.Add($"'{contract.Name}' v{contract.Version}: serialized property " +
                                   $"'{property.Name}' matches prohibited secret material naming");
                }
            }

            foreach (var secretLikeProperty in contract.SensitiveFields.Select(f => f.Property))
            {
                var normalized = secretLikeProperty.ToLowerInvariant();
                var prohibited = ProhibitedSecretFragments.Any(normalized.Contains);

                prohibited.Should().BeFalse(
                    "'{0}' v{1}: sensitive-field classification exists to protect intentional delivery " +
                    "material, not to allowlist raw secrets ('" + secretLikeProperty + "')",
                    contract.Name, contract.Version);
            }
        }

        violations.Should().BeEmpty(string.Join("; ", violations));
    }

    /// <summary>
    /// IA-TST-EVT-PRIV-001 / IA-TST-EVT-PRIV-002 / IAREQ086 — personal-data
    /// fields on public events are intentionally classified with purpose and
    /// consumer justification; unclassified PII-bearing properties fail.
    /// </summary>
    [Fact]
    public void PiiBearingPublicEvents_CarryExplicitClassificationMetadata()
    {
        var violations = new List<string>();

        foreach (var contract in Generated.Contracts)
        {
            foreach (var property in contract.SerializedProperties)
            {
                if (!LooksLikePersonalData(property.Name))
                {
                    continue;
                }

                var classification = contract.PiiFields.FirstOrDefault(f =>
                    f.Property.Equals(property.Name, StringComparison.OrdinalIgnoreCase));

                if (classification is null)
                {
                    violations.Add(
                        $"'{contract.Name}' v{contract.Version}: personal-data-like property " +
                        $"'{property.Name}' has no [EventPiiField] classification");
                }
                else
                {
                    classification.Purpose.Should().NotBeNullOrWhiteSpace(
                        "'{0}' v{1}: {2} purpose is mandatory", contract.Name, contract.Version, property.Name);

                    classification.Justification.Should().NotBeNullOrWhiteSpace(
                        "'{0}' v{1}: {2} consumer justification is mandatory", contract.Name, contract.Version, property.Name);
                }
            }

            foreach (var pii in contract.PiiFields)
            {
                contract.SerializedProperties.Should().Contain(
                    p => p.Name.Equals(pii.Property, StringComparison.OrdinalIgnoreCase),
                    "'{0}' v{1}: PII classification targets a real serialized property ({2})",
                    contract.Name, contract.Version, pii.Property);
            }
        }

        violations.Should().BeEmpty(string.Join("; ", violations));
    }

    private static bool LooksLikePersonalData(string serializedPropertyName)
    {
        var normalized = serializedPropertyName.ToLowerInvariant();
        return normalized.Contains("email") || normalized.Contains("displayname");
    }

    // ── Manifest drift ──────────────────────────────────────────────────────

    /// <summary>
    /// IA-TST-EVT-CONTRACT-001 / IAREQ132 — canonical manifest drift gate.
    ///
    /// Regeneration path: run with REGENERATE_EVENT_MANIFEST=1 to intentionally
    /// rewrite backend/contracts/events/notrelix.events.json from accepted
    /// source contract changes, then review the diff. The gate never silently
    /// overwrites drift during ordinary runs.
    /// </summary>
    [Fact]
    public void CanonicalManifest_MatchesGeneratedSourceShape()
    {
        var repoRoot = GetRepoRoot();

        if (Environment.GetEnvironmentVariable("REGENERATE_EVENT_MANIFEST") == "1")
        {
            EventManifestGenerator.Write(Generated, repoRoot);
        }

        var checkedIn = EventManifestGenerator.LoadCheckedInManifest(repoRoot);

        checkedIn.Should().NotBeNull(
            $"the canonical artifact {EventManifestGenerator.ManifestRelativePath} must exist");

        var expected = Generated;
        var differences = EventManifestGenerator.CompareSemantically(expected, checkedIn!);

        differences.Should().BeEmpty(
            "public event contracts drifted from the canonical manifest. Restore the v1 schema or add a " +
            "new version with a migration plan, then regenerate through the canonical generator "
            + "(REGENERATE_EVENT_MANIFEST=1): " + string.Join("; ", differences));
    }

    /// <summary>
    /// IA-TST-EVT-CONTRACT-002 / IAREQ088 — proves the comparator fails a
    /// same-version serialized-schema mutation (property type/nullability/add-remove).
    /// </summary>
    [Fact]
    public void ManifestComparator_RejectsSameVersionSchemaChange()
    {
        var expected = Generated;
        var mutatedRows = Clone(expected).Contracts.ToList();

        var victimIndex = mutatedRows
            .Select((row, index) => (row, index))
            .First(x => x.row.SerializedProperties.Count > 3).index;

        var victim = mutatedRows[victimIndex];
        mutatedRows[victimIndex] = victim with
        {
            SerializedProperties = victim.SerializedProperties
                .Select(p => p with { Type = p.Type == "string" ? "int" : p.Type })
                .ToList(),
        };

        var mutated = expected with { Contracts = mutatedRows };

        EventManifestGenerator.CompareSemantically(expected, mutated)
            .Should().Contain(d => d.Contains("type changed", StringComparison.OrdinalIgnoreCase))
            .And.Contain(d => d.Contains("version bump", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// IA-TST-EVT-CONTRACT-003 / IAREQ132 — manifest uses PRODUCTION wire names
    /// (camelCase), not naive CLR reflection names.
    /// </summary>
    [Fact]
    public void ManifestUsesProductionSerializerNaming()
    {
        var generated = Generated;

        generated.Contracts.Should().NotBeEmpty();

        foreach (var contract in generated.Contracts.Take(10))
        {
            contract.SerializedProperties.Should().Contain(
                p => p.Name == "eventId",
                "'{0}' v{1}: CLR 'EventId' must appear under its production camelCase wire name",
                contract.Name, contract.Version);
        }

        // A fixture where CLR and wire naming differ proves the transform ran.
        var sampleWithCamelDifference = generated.Contracts.First(c =>
            c.SerializedProperties.Any(p => char.IsLower(p.Name[0]) && !p.Name.EndsWith("id")));
        sampleWithCamelDifference.SerializedProperties
            .Where(p => char.IsUpper(p.Name[0]))
            .Should().BeEmpty("all manifest property names must use production camelCase wire names");
    }

    /// <summary>
    /// IA-TST-EVT-CONTRACT-004 / IAREQ132 / IAREQ133 / IAREQ139 — global scope:
    /// complete coverage, producer isolation, and no Domain-only internal events
    /// promoted into the public manifest.
    /// </summary>
    [Fact]
    public void GlobalManifest_PreservesBoundedContextOwnership()
    {
        var generated = Generated;

        // Complete coverage of the canonical production registry...
        var registryKeys = Contracts.Select(c => (c.Name.ToLowerInvariant(), c.Version)).ToHashSet();
        var manifestKeys = generated.Contracts.Select(c => (c.Name.ToLowerInvariant(), c.Version)).ToHashSet();

        manifestKeys.Except(registryKeys).Should().BeEmpty(
            "manifest rows without a canonical registry entry are forbidden");

        registryKeys.Except(manifestKeys).Should().BeEmpty(
            "canonical production contracts missing from the global manifest are forbidden");

        // ...producer ownership is explicit for every row...
        generated.Contracts.Select(c => c.ProducerContext)
            .Should().NotContain(string.Empty, "every public contract records its owning context");

        // ...and Domain-only internal events are NOT promoted.
        var domainEventNames = typeof(IDomainEvent).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(IDomainEvent).IsAssignableFrom(t)
                        && typeof(IIntegrationEvent).IsAssignableFrom(t) is false)
            .Select(t => t.FullName)
            .ToHashSet();

        generated.Contracts.Select(c => c.ClrType)
            .Should().NotIntersectWith(domainEventNames.ToList(),
                "Domain-only internal events are not rows in the public integration-event manifest");
    }

    // ── Migration protocol evidence ─────────────────────────────────────────

    /// <summary>
    /// IA-TST-MIG-EVT-001 / IAREQ131 / IAREQ139 — no production name-only
    /// public-event resolution path remains after the compound-key migration.
    /// </summary>
    [Fact]
    public void ProductionResolutionPaths_UseCompoundContractIdentity()
    {
        // 1. The catalog surface exposes only compound-key resolution.
        typeof(IIntegrationEventCatalog)
            .GetMethods()
            .Where(m => m.Name is nameof(IIntegrationEventCatalog.Resolve) or nameof(IIntegrationEventCatalog.TryResolve))
            .SelectMany(m => m.GetParameters())
            .Should().Contain(p => p.ParameterType == typeof(EventContractKey),
                "runtime resolution requires (Name, Version)")
            .And.NotContain(
                p => p.ParameterType == typeof(string),
                "name-only resolution APIs are removed — no silent fallback may remain");

        // 2. The production dispatcher resolves through the compound key using
        //    the version carried by the durable envelope row.
        var dispatcherSource = RemoveComments(File.ReadAllText(Path.Combine(
            GetInfrastructurePath(), "BackgroundJobs", "OutboxDispatcher.cs")));

        dispatcherSource.Should().Contain("new EventContractKey(message.MessageName, message.SchemaVersion)",
            "the outbox dispatch path resolves by the envelope's own (name, version)");
    }

    private static List<(string ConsumerTypeName, List<(string Name, int Version)> EventKeys)> DiscoverActualConsumers()
    {
        return typeof(ConsumerRegistry).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Select(t => (
                ConsumerTypeName: t.FullName ?? t.Name,
                EventKeys: t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
                    .Select(i => i.GetGenericArguments()[0])
                    .Select(eventType => (eventType.GetCustomAttribute<EventNameAttribute>()?.Name, eventType))
                    .Where(x => x.Name is not null)
                    .Select(x => (x.Name!, x.eventType.GetCustomAttribute<EventNameAttribute>()!.Version))
                    .ToList()))
            .Where(x => x.EventKeys.Count > 0)
            .ToList();
    }

    private static EventManifestModel Clone(EventManifestModel model) => model with
    {
        Contracts = model.Contracts.Select(c => c with
        {
            Scope = c.Scope with { },
            SerializedProperties = c.SerializedProperties.Select(p => p with { }).ToList(),
            PiiFields = c.PiiFields.Select(f => f with { }).ToList(),
            SensitiveFields = c.SensitiveFields.Select(f => f with { }).ToList(),
            Consumers = c.Consumers.Select(s => s with { }).ToList(),
        }).ToList(),
    };
}
