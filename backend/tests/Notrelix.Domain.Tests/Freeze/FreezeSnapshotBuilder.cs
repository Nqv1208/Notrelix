using System.Reflection;
using Notrelix.Domain.Tests.Freeze.Snapshots;

namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Builds deterministic snapshot content for Domain freeze contracts.
/// Output contains no timestamps, machine paths, or commit hashes.
/// Uses StringComparer.Ordinal and UTF-8 without BOM with \n newlines.
/// </summary>
public static class FreezeSnapshotBuilder
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;

    public static string BuildDomainEventsSnapshot()
    {
        var lines = new List<string>
        {
            "# Domain Event Contract Snapshot",
            "# Snapshot schema: 1",
            "# Contract: DomainEvents|LogicalName|Version|ClrType|Scope|PropertyName|PropertyType|IsNullable",
            ""
        };

        var events = DomainAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                     && typeof(IDomainEvent).IsAssignableFrom(t)
                     && DomainCapabilityRegistry.ResolveCapability(t) == DomainCapabilityStatus.Frozen)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();

        var nullabilityContext = new NullabilityInfoContext();

        foreach (var evt in events)
        {
            var attr = evt.GetCustomAttribute<EventNameAttribute>();
            var logicalName = attr?.Name ?? "UNNAMED";
            var version = attr?.Version ?? 0;
            var scope = ResolveScopeFromBaseType(evt);

            var properties = evt.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => p.GetMethod?.IsPublic == true)
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .ToList();

            foreach (var prop in properties)
            {
                var nullabilityInfo = nullabilityContext.Create(prop);
                var isNullable = IsNullableProperty(prop, nullabilityInfo);

                var typeName = CanonicalTypeNameFormatter.Format(prop.PropertyType);
                if (prop.PropertyType.IsGenericType && Nullable.GetUnderlyingType(prop.PropertyType) is { } underlying)
                    typeName = CanonicalTypeNameFormatter.Format(underlying);

                lines.Add($"{logicalName}|{version}|{evt.FullName}|{scope}|{prop.Name}|{typeName}|{isNullable}");
            }
        }

        return Encode(lines);
    }

    public static string BuildRuleCodesSnapshot()
    {
        // Static classes in C# are compiled as abstract+sealed at the IL level.
        var ruleCodeTypes = DomainAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: true, IsSealed: true, IsPublic: true }
                     && t.Name.EndsWith("RuleCodes", StringComparison.Ordinal)
                     && t.Namespace?.StartsWith("Notrelix.Domain", StringComparison.Ordinal) == true)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();

        var lines = new List<string>
        {
            "# Rule Code Snapshot",
            "# Snapshot schema: 1",
            "# Contract: RuleCodes|Code|OwnerContext|ConstantName",
            ""
        };

        foreach (var type in ruleCodeTypes)
        {
            var fields = type
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .OrderBy(f => f.Name, StringComparer.Ordinal);

            var context = type.Name.Replace("RuleCodes", "");

            foreach (var field in fields)
            {
                var value = (string)field.GetValue(null)!;
                lines.Add($"{value}|{context}|{field.Name}");
            }
        }

        return Encode(lines);
    }

    public static string BuildEnumsSnapshot()
    {
        var enums = DomainAssembly
            .GetTypes()
            .Where(t => t.IsEnum
                     && DomainCapabilityRegistry.ResolveCapability(t) == DomainCapabilityStatus.Frozen)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();

        var lines = new List<string>
        {
            "# Enum Snapshot",
            "# Snapshot schema: 2",
            "# Contract: Enums|EnumType|UnderlyingType|MemberName|NumericValue",
            ""
        };

        foreach (var enumType in enums)
        {
            var underlyingType = Enum.GetUnderlyingType(enumType);
            var underlyingName = CanonicalTypeNameFormatter.Format(underlyingType);
            var values = Enum.GetValues(enumType);
            foreach (var value in values)
            {
                var numericValue = Convert.ChangeType(value, underlyingType);
                lines.Add($"{enumType.FullName}|{underlyingName}|{value}|{numericValue}");
            }
        }

        return Encode(lines);
    }

    public static string BuildFrozenPublicApiSnapshot()
    {
        var types = DomainAssembly
            .GetTypes()
            .Where(t => t.IsPublic || t.IsNestedPublic)
            .Where(t => !t.IsDefined(typeof(ObsoleteAttribute), false))
            .Where(t => IsFrozenType(t))
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();

        var lines = new List<string>
        {
            "# Frozen Domain Public API Snapshot",
            "# Snapshot schema: 2",
            "# Contract: FrozenApi|Type|Member|MemberType|Visibility|IsAbstract|IsVirtual|ReturnOrPropertyType|ParametersOrAccessor",
            ""
        };

        foreach (var type in types)
        {
            var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(c => c.IsPublic || c.IsFamily)
                .OrderBy(c => c.GetParameters().Length)
                .ThenBy(c => CanonicalParameterSignature(c), StringComparer.Ordinal);
            foreach (var ctor in ctors)
            {
                var visibility = ctor.IsPublic ? "public" : "protected";
                lines.Add(string.Join(
                    "|",
                    type.FullName,
                    ".ctor",
                    "Constructor",
                    visibility,
                    ctor.IsAbstract,
                    ctor.IsVirtual,
                    "System.Void",
                    CanonicalParameterSignature(ctor)));
            }

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && m.GetBaseDefinition() == m)
                .Where(m => !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_") && !m.Name.StartsWith("add_") && !m.Name.StartsWith("remove_"))
                .Where(m => m.Name != "RaiseDomainEvent" && m.Name != "ClearDomainEvents")
                .OrderBy(m => m.Name, StringComparer.Ordinal)
                .ThenBy(m => CanonicalParameterSignature(m), StringComparer.Ordinal)
                .ThenBy(m => CanonicalTypeNameFormatter.Format(m.ReturnType), StringComparer.Ordinal);
            foreach (var method in methods)
            {
                var returnType = CanonicalTypeNameFormatter.Format(method.ReturnType);
                lines.Add(string.Join(
                    "|",
                    type.FullName,
                    method.Name,
                    "Method",
                    "public",
                    method.IsAbstract,
                    method.IsVirtual,
                    returnType,
                    CanonicalParameterSignature(method)));
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => p.GetMethod?.IsPublic == true)
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .ThenBy(p => CanonicalTypeNameFormatter.Format(p.PropertyType), StringComparer.Ordinal);
            foreach (var prop in properties)
            {
                var accessor = prop.SetMethod != null && (prop.SetMethod.IsPublic || prop.SetMethod.IsFamily)
                    ? "readwrite"
                    : "readonly";
                var propertyType = CanonicalTypeNameFormatter.Format(prop.PropertyType);
                lines.Add(string.Join(
                    "|",
                    type.FullName,
                    prop.Name,
                    "Property",
                    "public",
                    prop.GetMethod?.IsAbstract == true,
                    prop.GetMethod?.IsVirtual == true,
                    propertyType,
                    accessor));
            }
        }

        return Encode(lines);
    }

    private static string CanonicalParameterSignature(MethodBase method)
    {
        return string.Join(", ", method.GetParameters().Select(p =>
            $"{CanonicalTypeNameFormatter.Format(p.ParameterType)} {p.Name}"));
    }

    private static bool IsFrozenType(Type type)
    {
        if (type.Namespace is null) return false;

        return DomainCapabilityRegistry.ResolveCapability(type)
            == DomainCapabilityStatus.Frozen;
    }

    private static bool IsNullableProperty(PropertyInfo prop, NullabilityInfo nullabilityInfo)
    {
        // Nullable value types (T?) are always nullable
        if (prop.PropertyType.IsGenericType && Nullable.GetUnderlyingType(prop.PropertyType) is not null)
            return true;

        // Reference types: check NullabilityInfo.ReadState
        // NullabilityState: Unknown=0, NotNull=1, MaybeNull=2, MaybeNullReferenceType=3
        if (!prop.PropertyType.IsValueType)
            return nullabilityInfo.ReadState != NullabilityState.NotNull;

        return false;
    }

    private static string ResolveScopeFromBaseType(Type eventType)
    {
        var baseType = eventType.BaseType;
        while (baseType is not null && baseType != typeof(object))
        {
            var ns = baseType.Namespace ?? "";
            var name = baseType.Name;

            if (name == "GlobalDomainEvent" || (ns == "Notrelix.Domain.Common" && name == "DomainEvent"))
                return "Global";

            if (name == "AccountScopedDomainEvent" || name == "BillingAccountScopedDomainEvent")
                return "Account";

            if (name == "WorkspaceScopedDomainEvent")
                return "Workspace";

            baseType = baseType.BaseType;
        }

        return "Global";
    }

    private static string Encode(List<string> lines)
    {
        var content = string.Join("\n", lines);
        if (!content.EndsWith('\n'))
            content += "\n";
        return content;
    }
}
