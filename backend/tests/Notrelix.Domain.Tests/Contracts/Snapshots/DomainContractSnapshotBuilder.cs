using System.Reflection;

namespace Notrelix.Domain.Tests.Contracts.Snapshots;

public static class DomainContractSnapshotBuilder
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
            .Where(type =>
                type is { IsClass: true, IsAbstract: false }
                && typeof(IDomainEvent).IsAssignableFrom(type)
                && type.Namespace?.StartsWith("Notrelix.Domain.", StringComparison.Ordinal) == true)
            .OrderBy(type => type.GetCustomAttribute<EventNameAttribute>()?.Name ?? type.FullName, StringComparer.Ordinal)
            .ThenBy(type => type.FullName, StringComparer.Ordinal)
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

    public static string BuildDomainEventEnumsSnapshot()
    {
        var enums = DiscoverEventReachableEnums();

        var lines = new List<string>
        {
            "# Domain Event Enum Snapshot",
            "# Snapshot schema: 2",
            "# Contract: Enums|EnumType|UnderlyingType|MemberName|NumericValue",
            ""
        };

        foreach (var enumType in enums.OrderBy(t => t.FullName, StringComparer.Ordinal))
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

    private static HashSet<Type> DiscoverEventReachableEnums()
    {
        var eventTypes = DomainAssembly
            .GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false }
                && typeof(IDomainEvent).IsAssignableFrom(type)
                && type.Namespace?.StartsWith("Notrelix.Domain.", StringComparison.Ordinal) == true)
            .ToArray();

        var rootTypes = eventTypes
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(p => p.GetMethod?.IsPublic == true)
            .Select(p => p.PropertyType)
            .ToArray();

        var visited = new HashSet<Type>();
        var enums = new HashSet<Type>();

        foreach (var rootType in rootTypes)
            CollectEnums(rootType, visited, enums);

        return enums;
    }

    private static void CollectEnums(Type type, HashSet<Type> visited, HashSet<Type> enums)
    {
        if (!visited.Add(type))
            return;

        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            CollectEnums(underlying, visited, enums);
            return;
        }

        if (type.IsArray)
        {
            CollectEnums(type.GetElementType()!, visited, enums);
            return;
        }

        if (type.IsEnum)
        {
            enums.Add(type);
            return;
        }

        if (type.IsPrimitive || IsTerminal(type))
            return;

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
                CollectEnums(argument, visited, enums);
            return;
        }

        if (type.Namespace?.StartsWith("Notrelix.Domain", StringComparison.Ordinal) != true)
            return;

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (prop.GetMethod?.IsPublic == true)
                CollectEnums(prop.PropertyType, visited, enums);
        }
    }

    private static bool IsTerminal(Type type)
    {
        return type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(Guid)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Uri);
    }

    private static bool IsNullableProperty(PropertyInfo prop, NullabilityInfo nullabilityInfo)
    {
        if (prop.PropertyType.IsGenericType && Nullable.GetUnderlyingType(prop.PropertyType) is not null)
            return true;

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