using System.Reflection;
using System.Text;
using System.Text.Json;
using Notrelix.Infrastructure.Messaging;

namespace Notrelix.Architecture.Tests.Events.Support;

/// <summary>
/// Canonical generator for the global public integration-event contract
/// manifest (backend/contracts/events/notrelix.events.json) — IAREQ132 /
/// P13-EVT-003C.
///
/// The model is derived deterministically from the canonical production
/// contract registry (ContractRegistrySetup), the consumer registry
/// (ConsumerRegistrySetup) and reflection over the contract types using the
/// PRODUCTION serialization conventions (System.Text.Json camelCase, as used
/// by the outbox dispatcher). Domain-only internal events are never included:
/// the source universe is IIntegrationEvent implementations only.
/// </summary>
public static class EventManifestGenerator
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public static string ManifestRelativePath => Path.Combine("backend", "contracts", "events", "notrelix.events.json");

    public static EventManifestModel Build()
    {
        var contracts = ContractRegistrySetup.GetContractDefinitions();
        var consumers = ConsumerRegistrySetup.GetConsumerDefinitions();

        var rows = contracts
            .OrderBy(c => c.Name, StringComparer.Ordinal)
            .ThenBy(c => c.Version)
            .Select(c => BuildRow(c, consumers))
            .ToList();

        return new EventManifestModel(
            SchemaVersion: 2,
            Description: "Global backend baseline for PUBLIC integration-event contracts. Generated from ContractRegistrySetup/ConsumerRegistrySetup via production serializer conventions — do not hand-edit.",
            Contracts: rows);
    }

    public static EventManifestModel? LoadCheckedInManifest(string repoRoot)
    {
        var path = Path.Combine(repoRoot, ManifestRelativePath);
        if (!File.Exists(path))
        {
            return null;
        }

        return JsonSerializer.Deserialize<EventManifestModel>(
            File.ReadAllText(path), SerializerOptions);
    }

    /// <summary>Semantic comparison — incidental JSON formatting is irrelevant.</summary>
    public static List<string> CompareSemantically(EventManifestModel expected, EventManifestModel actual)
    {
        var differences = new List<string>();

        if (expected.SchemaVersion != actual.SchemaVersion)
        {
            differences.Add($"manifest schemaVersion: expected {expected.SchemaVersion}, found {actual.SchemaVersion}");
        }

        var expectedByKey = expected.Contracts.ToDictionary(c => (c.Name, c.Version));
        var actualByKey = actual.Contracts.ToDictionary(c => (c.Name, c.Version));

        foreach (var key in expectedByKey.Keys.OrderBy(k => k.Name).ThenBy(k => k.Version))
        {
            if (!actualByKey.TryGetValue(key, out var actualRow))
            {
                differences.Add($"missing contract row '{key.Name}' v{key.Version} " +
                                $"(producer context {expectedByKey[key].ProducerContext})");
                continue;
            }

            CompareRow(expectedByKey[key], actualRow, differences);
        }

        foreach (var key in actualByKey.Keys.Where(k => !expectedByKey.ContainsKey(k)).OrderBy(k => k.Name))
        {
            differences.Add($"unexpected contract row '{key.Name}' v{key.Version} — source contract removed or renamed without a migration");
        }

        return differences;

        static void CompareRow(ContractRow expected, ContractRow actualRow, List<string> differences)
        {
            if (!string.Equals(expected.ClrType, actualRow.ClrType, StringComparison.Ordinal))
                differences.Add($"'{expected.Name}' v{expected.Version}: clrType changed to '{actualRow.ClrType}' without a version bump");

            if (!string.Equals(expected.ProducerContext, actualRow.ProducerContext, StringComparison.Ordinal))
                differences.Add($"'{expected.Name}' v{expected.Version}: producerContext changed to '{actualRow.ProducerContext}'");

            if (!string.Equals(expected.Classification, actualRow.Classification, StringComparison.Ordinal))
                differences.Add($"'{expected.Name}' v{expected.Version}: classification changed");

            if (!string.Equals(expected.Compatibility, actualRow.Compatibility, StringComparison.Ordinal))
                differences.Add($"'{expected.Name}' v{expected.Version}: compatibility changed");

            if (expected.Deprecated != actualRow.Deprecated)
                differences.Add($"'{expected.Name}' v{expected.Version}: deprecated flag changed");

            if (expected.Scope.CarriesAccountId != actualRow.Scope.CarriesAccountId
                || expected.Scope.CarriesWorkspaceId != actualRow.Scope.CarriesWorkspaceId
                || !string.Equals(expected.Scope.TenantScope, actualRow.Scope.TenantScope, StringComparison.Ordinal))
                differences.Add($"'{expected.Name}' v{expected.Version}: scope metadata changed");

            CompareProperties(expected.SerializedProperties, actualRow.SerializedProperties, expected, differences);
            CompareClassifiedFields(expected.PiiFields, actualRow.PiiFields, "pii", expected, differences);
            CompareClassifiedFields(expected.SensitiveFields, actualRow.SensitiveFields, "sensitive", expected, differences);

            var expectedConsumers = expected.Consumers.OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
            var actualConsumers = actualRow.Consumers.OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
            if (!expectedConsumers.SequenceEqual(actualConsumers))
            {
                differences.Add($"'{expected.Name}' v{expected.Version}: consumer inventory/maturity summary changed");
            }
        }

        static void CompareProperties(
            IReadOnlyList<SerializedProperty> expected,
            IReadOnlyList<SerializedProperty> actualRow,
            ContractRow owner,
            List<string> differences)
        {
            var expectedByName = expected.ToDictionary(p => p.Name);
            var actualByName = actualRow.ToDictionary(p => p.Name);

            foreach (var property in expected)
            {
                if (!actualByName.TryGetValue(property.Name, out var actualProperty))
                {
                    differences.Add($"'{owner.Name}' v{owner.Version}: serialized property '{property.Name}' removed without a version bump — restore v1 schema or add a new version with a migration plan");
                    continue;
                }

                if (!string.Equals(property.Type, actualProperty.Type, StringComparison.Ordinal))
                {
                    differences.Add($"'{owner.Name}' v{owner.Version}: serialized property '{property.Name}' type changed '{property.Type}' → '{actualProperty.Type}' without a version bump — restore v1 schema or add a new version with a migration plan");
                }

                if (property.Nullable != actualProperty.Nullable)
                {
                    differences.Add($"'{owner.Name}' v{owner.Version}: serialized property '{property.Name}' nullability changed without a version bump — restore v1 schema or add a new version with a migration plan");
                }
            }

            foreach (var added in actualByName.Keys.Except(expectedByName.Keys))
            {
                differences.Add($"'{owner.Name}' v{owner.Version}: serialized property '{added}' added without a version bump — restore v1 schema or add a new version with a migration plan");
            }
        }

        static void CompareClassifiedFields<TField>(
            IReadOnlyList<TField> expected,
            IReadOnlyList<TField> actualRow,
            string kind,
            ContractRow owner,
            List<string> differences)
            where TField : ClassifiedField
        {
            static string Key(TField f) => f.Property.ToLowerInvariant();
            var expectedKeys = expected.Select(Key).ToHashSet();
            var actualKeys = actualRow.Select(Key).ToHashSet();

            foreach (var removed in expectedKeys.Except(actualKeys))
            {
                differences.Add($"'{owner.Name}' v{owner.Version}: {kind} classification for '{removed}' removed");
            }

            foreach (var added in actualKeys.Except(expectedKeys))
            {
                differences.Add($"'{owner.Name}' v{owner.Version}: {kind} classification for '{added}' added without an accepted contract decision");
            }
        }
    }

    private static ContractRow BuildRow(ContractDefinition contract, IReadOnlyList<ConsumerDefinition> consumers)
    {
        var type = contract.IntegrationEventType;
        var nullableContext = new NullabilityInfoContext();

        // Production wire shape includes inherited base-record properties; order
        // by MetadataToken gives stable declaration order (base members first).
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Concat(GetBaseRecordProperties(type))
            .DistinctBy(p => p.Name)
            .OrderBy(p => p.MetadataToken)
            .Select(p => ToSerializedProperty(p, nullableContext))
            .ToList();

        var piiAttributes = type.GetCustomAttributes<EventPiiFieldAttribute>()
            .Select(a => new ClassifiedField(
                Property: ToCamelCase(a.PropertyName),
                Purpose: a.Purpose,
                Justification: a.ConsumerJustification))
            .ToList();

        var sensitiveAttributes = type.GetCustomAttributes<EventSensitiveFieldAttribute>()
            .Select(a => new ClassifiedField(
                Property: ToCamelCase(a.PropertyName),
                Purpose: a.Classification,
                Justification: a.Justification))
            .ToList();

        var consumerRows = consumers
            .Where(c => string.Equals(c.EventName, contract.Name, StringComparison.OrdinalIgnoreCase)
                        && c.EventVersion == contract.Version)
            .OrderBy(c => c.ConsumerName, StringComparer.Ordinal)
            .Select(c => new ConsumerSummary(
                Name: c.ConsumerName,
                Context: c.BoundedContext,
                Maturity: c.Maturity.ToString()))
            .ToList();

        return new ContractRow(
            Name: contract.Name,
            Version: contract.Version,
            ClrType: type.FullName ?? type.Name,
            ProducerContext: ResolveProducerContext(type),
            Classification: contract.Classification.ToString(),
            Compatibility: contract.Compatibility.ToString(),
            Deprecated: contract.Deprecated,
            Scope: BuildScope(type),
            SerializedProperties: properties,
            PiiFields: piiAttributes,
            SensitiveFields: sensitiveAttributes,
            Consumers: consumerRows);
    }

    private static IEnumerable<PropertyInfo> GetBaseRecordProperties(Type type)
    {
        var baseType = type.BaseType;
        while (baseType is not null && baseType != typeof(object))
        {
            foreach (var property in baseType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                yield return property;
            }

            baseType = baseType.BaseType;
        }
    }

    private static SerializedProperty ToSerializedProperty(PropertyInfo property, NullabilityInfoContext context)
    {
        var info = context.Create(property);
        return new SerializedProperty(
            Name: ToCamelCase(property.Name),
            Type: FormatType(property.PropertyType),
            // Annotation-based read state reflects the declared contract shape.
            Nullable: info.ReadState is NullabilityState.Nullable);
    }

    private static string FormatType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
        {
            return FormatType(underlying) + "?";
        }

        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`')];
            var arguments = string.Join(", ", type.GetGenericArguments().Select(FormatType));
            return $"{name}<{arguments}>";
        }

        return type.Name switch
        {
            "String" => "string",
            "Int32" => "int",
            "Boolean" => "bool",
            _ => type.Name,
        };
    }

    /// <summary>Production wire names follow the outbox dispatcher's camelCase policy.</summary>
    private static string ToCamelCase(string name) =>
        char.ToLowerInvariant(name[0]) + name[1..];

    private static string ResolveProducerContext(Type type)
    {
        const string marker = "Notrelix.Application.Events.";
        var ns = type.Namespace ?? string.Empty;
        return ns.StartsWith(marker, StringComparison.Ordinal)
            ? ns[marker.Length..]
            : ns;
    }

    private static ScopeMetadata BuildScope(Type type)
    {
        var parameterNames = GetPrimaryConstructorParameterNames(type);
        var tenantScope = type.GetCustomAttribute<IntegrationEventTenantScopeAttribute>()?.Scope.ToString() ?? "Missing";

        return new ScopeMetadata(
            TenantScope: tenantScope,
            CarriesAccountId: parameterNames.Contains("accountId", StringComparer.OrdinalIgnoreCase),
            CarriesWorkspaceId: parameterNames.Contains("workspaceId", StringComparer.OrdinalIgnoreCase));
    }

    private static IReadOnlySet<string> GetPrimaryConstructorParameterNames(Type type)
    {
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var primary = constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

        return primary?.GetParameters().Select(p => p.Name!).ToHashSet(StringComparer.OrdinalIgnoreCase)
               ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Writes the canonical manifest artifact. Used by the regeneration path only.</summary>
    public static void Write(EventManifestModel model, string repoRoot)
    {
        var path = Path.Combine(repoRoot, ManifestRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(model, SerializerOptions) + "\n", new UTF8Encoding(false));
    }
}

public sealed record EventManifestModel(
    int SchemaVersion,
    string Description,
    IReadOnlyList<ContractRow> Contracts);

public sealed record ContractRow(
    string Name,
    int Version,
    string ClrType,
    string ProducerContext,
    string Classification,
    string Compatibility,
    bool Deprecated,
    ScopeMetadata Scope,
    IReadOnlyList<SerializedProperty> SerializedProperties,
    IReadOnlyList<ClassifiedField> PiiFields,
    IReadOnlyList<ClassifiedField> SensitiveFields,
    IReadOnlyList<ConsumerSummary> Consumers);

public sealed record ScopeMetadata(string TenantScope, bool CarriesAccountId, bool CarriesWorkspaceId);

public sealed record SerializedProperty(string Name, string Type, bool Nullable);

public record ClassifiedField(string Property, string Purpose, string Justification);

public sealed record ConsumerSummary(string Name, string Context, string Maturity);
