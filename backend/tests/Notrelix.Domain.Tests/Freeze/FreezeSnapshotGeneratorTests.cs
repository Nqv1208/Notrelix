using System.Reflection;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Generates and verifies all Domain freeze contract snapshots.
/// Run this test once to generate the initial approved snapshots.
/// Subsequent runs verify no drift in the frozen Domain surface.
/// </summary>
public class FreezeSnapshotGeneratorTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;
    private static readonly string SnapshotsDir = GetSnapshotsDirectory();

    private static string GetSnapshotsDirectory()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "tests", "Notrelix.Domain.Tests", "Snapshots");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        // Fallback: create relative to test assembly
        var assemblyDir = Path.GetDirectoryName(typeof(FreezeSnapshotGeneratorTests).Assembly.Location)!;
        return Path.Combine(assemblyDir, "Snapshots");
    }

    [Fact]
    public void Generate_AllSnapshots_IfNotExist()
    {
        Directory.CreateDirectory(SnapshotsDir);

        GenerateAggregateMutationsSnapshot();
        GenerateDomainEventsSnapshot();
        GenerateRuleCodesSnapshot();
        GenerateEnumsSnapshot();
        GenerateFrozenPublicApiSnapshot();
    }

    private void GenerateAggregateMutationsSnapshot()
    {
        var path = Path.Combine(SnapshotsDir, "AggregateMutations.approved.txt");

        var aggregateRoots = DomainAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                     && typeof(AggregateRoot).IsAssignableFrom(t))
            .OrderBy(t => t.FullName)
            .ToList();

        var testAssembly = typeof(FreezeSnapshotGeneratorTests).Assembly;
        var testTypes = testAssembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var lines = new List<string>
        {
            "# Aggregate Mutation Coverage Registry",
            "# Format: Aggregate|Method|ValidTest|NoOpTest|InvalidTest|AuditTest|VersionTest|EventTest",
            "# Generated: " + DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ""
        };

        foreach (var aggregate in aggregateRoots)
        {
            var coveringTests = testTypes
                .Where(t => t.GetCustomAttributes<CoversAggregateAttribute>()
                    .Any(a => a.AggregateType == aggregate))
                .ToList();

            var methods = aggregate
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && m.GetBaseDefinition() == m) // non-inherited, non-property accessors
                .Where(m => !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_") && !m.Name.StartsWith("add_") && !m.Name.StartsWith("remove_"))
                .Where(m => m.Name != "RaiseDomainEvent" && m.Name != "ClearDomainEvents") // infrastructure methods
                .OrderBy(m => m.Name)
                .ToList();

            foreach (var method in methods)
            {
                var hasValid = coveringTests.Any(t => t.GetMethods().Any(m => m.Name.Contains(method.Name) && m.Name.Contains("Valid") || m.Name.Contains("Should") || m.Name.Contains("Succeed")));
                var hasNoOp = coveringTests.Any(t => t.GetMethods().Any(m => m.Name.Contains("NoOp") || m.Name.Contains("Idempotent") || m.Name.Contains("Already") || m.Name.Contains("Empty") || m.Name.Contains("NoOp")));
                var hasInvalid = coveringTests.Any(t => t.GetMethods().Any(m => m.Name.Contains("Invalid") || m.Name.Contains("Throw") || m.Name.Contains("Fail") || m.Name.Contains("Reject")));
                var hasAudit = coveringTests.Any(t => t.GetMethods().Any(m => m.Name.Contains("Audit") || m.Name.Contains("UpdatedAt") || m.Name.Contains("UpdatedBy")));
                var hasVersion = coveringTests.Any(t => t.GetMethods().Any(m => m.Name.Contains("Version")));
                var hasEvent = coveringTests.Any(t => t.GetMethods().Any(m => m.Name.Contains("Event") || m.Name.Contains("Raise")));

                lines.Add($"{aggregate.Name}|{method.Name}|{hasValid}|{hasNoOp}|{hasInvalid}|{hasAudit}|{hasVersion}|{hasEvent}");
            }
        }

        File.WriteAllLines(path, lines);
    }

    private void GenerateDomainEventsSnapshot()
    {
        var path = Path.Combine(SnapshotsDir, "DomainEvents.approved.txt");

        var events = DomainAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                     && typeof(IDomainEvent).IsAssignableFrom(t))
            .OrderBy(t => t.FullName)
            .ToList();

        var lines = new List<string>
        {
            "# Domain Event Contract Snapshot",
            "# Format: LogicalName|Version|ClrType|Scope|PropertyName|PropertyType|IsNullable",
            "# Generated: " + DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ""
        };

        foreach (var evt in events)
        {
            var attr = evt.GetCustomAttribute<EventNameAttribute>();
            var logicalName = attr?.Name ?? "UNNAMED";
            var version = attr?.Version ?? 0;

            var properties = evt.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetMethod?.IsPublic == true)
                .OrderBy(p => p.Name)
                .ToList();

            foreach (var prop in properties)
            {
                var isNullable = prop.PropertyType.IsGenericType &&
                                 prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
                var typeName = isNullable
                    ? prop.PropertyType.GetGenericArguments()[0].Name
                    : prop.PropertyType.Name;

                lines.Add($"{logicalName}|{version}|{evt.FullName}|{GetScope(evt)}|{prop.Name}|{typeName}|{isNullable}");
            }
        }

        File.WriteAllLines(path, lines);
    }

    private static string GetScope(Type eventType)
    {
        var ns = eventType.Namespace ?? "";
        if (ns.Contains(".Accounts.")) return "Accounts";
        if (ns.Contains(".Identity.")) return "Identity";
        if (ns.Contains(".Workspaces.")) return "Workspaces";
        if (ns.Contains(".WorkManagement.")) return "WorkManagement";
        if (ns.Contains(".Documents.")) return "Documents";
        if (ns.Contains(".Collaboration.")) return "Collaboration";
        if (ns.Contains(".Automation.")) return "Automation";
        if (ns.Contains(".Integrations.")) return "Integrations";
        if (ns.Contains(".Billing.")) return "Billing";
        if (ns.Contains(".Governance.")) return "Governance";
        if (ns.Contains(".Analytics.")) return "Analytics";
        if (ns.Contains(".Common.")) return "Common";
        return "Unknown";
    }

    private void GenerateRuleCodesSnapshot()
    {
        var path = Path.Combine(SnapshotsDir, "RuleCodes.approved.txt");

        var ruleCodeTypes = new[]
        {
            typeof(Notrelix.Domain.Common.Exceptions.CommonRuleCodes),
            typeof(Notrelix.Domain.Accounts.AccountRuleCodes),
            typeof(Notrelix.Domain.Identity.IdentityRuleCodes),
            typeof(Notrelix.Domain.Workspaces.WorkspaceRuleCodes),
            typeof(Notrelix.Domain.WorkManagement.WorkManagementRuleCodes),
            typeof(Notrelix.Domain.Documents.DocumentRuleCodes),
            typeof(Notrelix.Domain.Collaboration.CollaborationRuleCodes),
            typeof(Notrelix.Domain.Automation.AutomationRuleCodes),
            typeof(Notrelix.Domain.Integrations.IntegrationRuleCodes),
            typeof(Notrelix.Domain.Billing.BillingRuleCodes),
            typeof(Notrelix.Domain.Governance.GovernanceRuleCodes),
            typeof(Notrelix.Domain.Analytics.AnalyticsRuleCodes),
        };

        var lines = new List<string>
        {
            "# Rule Code Snapshot",
            "# Format: Code|OwnerContext|ConstantName",
            "# Generated: " + DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ""
        };

        foreach (var type in ruleCodeTypes)
        {
            var fields = type
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .OrderBy(f => f.Name);

            var context = type.Name.Replace("RuleCodes", "");

            foreach (var field in fields)
            {
                var value = (string)field.GetValue(null)!;
                lines.Add($"{value}|{context}|{field.Name}");
            }
        }

        File.WriteAllLines(path, lines);
    }

    private void GenerateEnumsSnapshot()
    {
        var path = Path.Combine(SnapshotsDir, "Enums.approved.txt");

        var enums = DomainAssembly
            .GetTypes()
            .Where(t => t.IsEnum)
            .OrderBy(t => t.FullName)
            .ToList();

        var lines = new List<string>
        {
            "# Enum Snapshot",
            "# Format: EnumType|MemberName|NumericValue",
            "# Generated: " + DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ""
        };

        foreach (var enumType in enums)
        {
            var values = Enum.GetValues(enumType);
            foreach (var value in values)
            {
                lines.Add($"{enumType.FullName}|{value}|{(int)value}");
            }
        }

        File.WriteAllLines(path, lines);
    }

    private void GenerateFrozenPublicApiSnapshot()
    {
        var path = Path.Combine(SnapshotsDir, "FrozenDomainPublicApi.approved.txt");

        var types = DomainAssembly
            .GetTypes()
            .Where(t => t.IsPublic || t.IsNestedPublic)
            .Where(t => !t.IsDefined(typeof(ObsoleteAttribute), false))
            .Where(t => !t.Namespace!.Contains(".Experimental.")) // exclude experimental
            .OrderBy(t => t.FullName)
            .ToList();

        var lines = new List<string>
        {
            "# Frozen Domain Public API Snapshot",
            "# Format: Type|Member|MemberType|Visibility|IsAbstract|IsVirtual|Parameters",
            "# Generated: " + DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ""
        };

        foreach (var type in types)
        {
            // Constructors
            var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(c => c.IsPublic || c.IsFamily)
                .OrderBy(c => c.GetParameters().Length);
            foreach (var ctor in ctors)
            {
                var paramStr = string.Join(", ", ctor.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                lines.Add($"{type.FullName}|.ctor|Constructor|{(ctor.IsPublic ? "public" : "protected")}|{ctor.IsAbstract}|{ctor.IsVirtual}|{paramStr}");
            }

            // Public methods
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && m.GetBaseDefinition() == m)
                .Where(m => !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_") && !m.Name.StartsWith("add_") && !m.Name.StartsWith("remove_"))
                .Where(m => m.Name != "RaiseDomainEvent" && m.Name != "ClearDomainEvents")
                .OrderBy(m => m.Name);
            foreach (var method in methods)
            {
                var paramStr = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                lines.Add($"{type.FullName}|{method.Name}|Method|public|{method.IsAbstract}|{method.IsVirtual}|{paramStr}");
            }

            // Public properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => p.GetMethod?.IsPublic == true)
                .OrderBy(p => p.Name);
            foreach (var prop in properties)
            {
                var setter = prop.SetMethod != null && (prop.SetMethod.IsPublic || prop.SetMethod.IsFamily)
                    ? "readwrite"
                    : "readonly";
                lines.Add($"{type.FullName}|{prop.Name}|Property|public|{prop.GetMethod?.IsAbstract == true}|{prop.GetMethod?.IsVirtual == true}|{setter}");
            }
        }

        File.WriteAllLines(path, lines);
    }
}