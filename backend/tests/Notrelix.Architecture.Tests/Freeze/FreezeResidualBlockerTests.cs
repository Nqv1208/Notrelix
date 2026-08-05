using System.Reflection;
using Notrelix.Architecture.Tests.Support;

namespace Notrelix.Architecture.Tests.Freeze;

/// <summary>
/// FZ-00: Residual foundation-freeze blocker tests.
/// Each test reproduces a currently-present foundation defect and is expected
/// to FAIL until its owning freeze item lands:
///   FZ-0001/0002 -> FZ-IDEM-01 (marker-only requests, scoped execution context)
///   FZ-0004      -> FZ-APP-02  (authenticated bootstrap, no user selector)
///   FZ-0005      -> FZ-APP-01..03 (narrow read ports / provisioning service)
///   FZ-0006      -> FZ-APP-04  (Roslyn invocation gate — landed; now asserts detection)
///   FZ-0007*     -> FZ-RES-03/04 (ResourceType cutover and deletion)
/// </summary>
public class FreezeResidualBlockerTests : ArchitectureTestBase
{
    private static readonly Assembly ApplicationAssembly =
        typeof(Notrelix.Application.Common.Behaviors.ValidationBehavior<,>).Assembly;

    private static readonly Assembly DomainAssembly =
        typeof(Notrelix.Domain.Common.Guard).Assembly;

    private static readonly HashSet<string> DbContextPortNames = new(StringComparer.Ordinal)
    {
        "IWorkManagementDbContext",
        "IWorkspaceDbContext",
        "IIdentityDbContext",
        "IAccountDbContext",
        "IDocumentDbContext",
        "ICollaborationDbContext",
        "IAutomationDbContext",
        "IGovernanceDbContext",
        "IIntegrationDbContext",
        "IBillingDbContext",
        "IReportingDbContext",
    };

    private static readonly Dictionary<string, string> ContextToPort = new(StringComparer.Ordinal)
    {
        ["WorkManagement"] = "IWorkManagementDbContext",
        ["Workspaces"] = "IWorkspaceDbContext",
        ["Identity"] = "IIdentityDbContext",
        ["Accounts"] = "IAccountDbContext",
        ["Documents"] = "IDocumentDbContext",
        ["Collaboration"] = "ICollaborationDbContext",
        ["Automation"] = "IAutomationDbContext",
        ["Governance"] = "IGovernanceDbContext",
        ["Integrations"] = "IIntegrationDbContext",
        ["Billing"] = "IBillingDbContext",
        ["Analytics"] = "IReportingDbContext",
    };

    // --- FZ-0001/0002: marker-only idempotency ---

    [Fact]
    public void FZ_0001_IIdempotentRequest_Is_Marker_Only()
    {
        var members = typeof(IIdempotentRequest)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        members.Should().BeEmpty(
            "IIdempotentRequest must be a marker interface — the raw execution key lives in the scoped IdempotencyExecutionContext");
    }

    [Fact]
    public void FZ_0002_No_Idempotent_Command_Owns_An_Idempotency_Key()
    {
        var violations = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => typeof(IIdempotentRequest).IsAssignableFrom(t))
            .SelectMany(t => t.GetMembers(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(m => m.Name.Contains("IdempotencyKey", StringComparison.Ordinal))
            .Select(m => $"{m.DeclaringType?.FullName}.{m.Name}")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        violations.Should().BeEmpty(
            "commands must contain business data only — no IdempotencyKey member, parameter or fallback");
    }

    // --- FZ-0004: authenticated bootstrap, no user selector ---

    [Fact]
    public void FZ_0004_Bootstrap_Query_Has_No_User_Selector_And_Is_Not_Anonymous()
    {
        var queryType = ApplicationAssembly.GetTypes().Single(t => t.Name == "GetBootstrapQuery");

        queryType.GetInterfaces().Any(i => i.Name == "IAnonymousRequest").Should().BeFalse(
            "bootstrap is security-sensitive and must never run unauthenticated");

        queryType.GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.Name)
            .Should().NotContain(name =>
                name.Equals("userId", StringComparison.OrdinalIgnoreCase),
                "bootstrap must obtain the caller from the trusted current-user context, not a request field");
    }

    // --- FZ-0005: exhaustive foreign-DbContext gate (no exceptions) ---

    [Fact]
    public void FZ_0005_No_Handler_Injects_A_Foreign_Context_DbContext()
    {
        var violations = new List<string>();

        foreach (var handler in GetHandlerTypes())
        {
            var context = GetContextFromHandlerNamespace(handler.Namespace);
            if (context is null) continue;

            var owningPort = ContextToPort.GetValueOrDefault(context);
            if (owningPort is null) continue;

            foreach (var ctor in handler.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (var param in ctor.GetParameters())
                {
                    var portName = param.ParameterType.Name;
                    if (DbContextPortNames.Contains(portName) && portName != owningPort)
                        violations.Add($"{handler.Name}:{portName}");
                }
            }
        }

        violations.Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .Should().BeEmpty(
                "the six transitional cross-context reads must be migrated to narrow read ports / the provisioning service");
    }

    // --- FZ-0006: Roslyn invocation gate detects hidden SaveChanges invocations ---

    [Fact]
    public void FZ_0006_SaveChanges_Invocation_Is_Detected_By_The_Gate()
    {
        // Negative fixture: the old name-based gate (APP_DATA_005) inspected public
        // METHOD NAMES, so a handler whose Handle method invokes SaveChangesAsync
        // internally was invisible to it. FZ-APP-04 replaces it with a Roslyn
        // invocation gate that parses the source and sees the actual call.
        const string evasionSource = """
            internal sealed class EvasionFixture
            {
                private readonly ISaveChangesContext _context;

                public Task<string> Handle(string request, CancellationToken cancellationToken)
                    => _context.SaveChangesAsync(cancellationToken);
            }
            """;

        var violations = HandlerDataAccessInvocationGate.Scan(evasionSource, "EvasionFixture.cs");

        violations.Should().Contain(
            v => v.EndsWith("SaveChangesAsync", StringComparison.Ordinal),
            "a handler invoking SaveChanges must be flagged by the Roslyn invocation gate (FZ-APP-04)");
    }

    // --- FZ-0007: ResourceType / LegacyResourceTypeMappings are deleted ---

    [Fact]
    public void FZ_0007_Global_ResourceType_Enum_Is_Deleted()
    {
        var resourceType = DomainAssembly.GetTypes()
            .SingleOrDefault(t => t.FullName == "Notrelix.Domain.SharedKernel.ResourceType");

        resourceType.Should().BeNull(
            "the global ResourceType enum must be deleted — generic references use ResourceKind");
    }

    [Fact]
    public void FZ_0007b_Legacy_Resource_Type_Mappings_Are_Deleted()
    {
        var legacy = DomainAssembly.GetTypes()
            .SingleOrDefault(t => t.FullName == "Notrelix.Domain.SharedKernel.LegacyResourceTypeMappings");

        legacy.Should().BeNull("LegacyResourceTypeMappings must be deleted after cutover");
    }

    [Fact]
    public void FZ_0007c_No_Reverse_Mapping_Or_Dual_Read_Parser_Remains_In_Production()
    {
        var needle = new Regex(
            "TryToLegacyEnum|ParseResourceKind|ResourceRef\\.Create\\(ResourceType",
            RegexOptions.Compiled);

        var matches = Directory.EnumerateFiles(GetSrcPath(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .SelectMany(f => needle.Matches(RemoveComments(File.ReadAllText(f)))
                .Select(m => $"{Path.GetRelativePath(GetSrcPath(), f)}:{m.Value}"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        matches.Should().BeEmpty(
            "no reverse mapping, dual-read parser or legacy enum factory may remain");
    }

    // --- FZ-0008: legacy ops idempotency residue is zero ---

    [Fact]
    public void FZ_0008_No_Legacy_Ops_Idempotency_Types_Remain_In_Infrastructure()
    {
        var infrastructureAssembly = typeof(Notrelix.Infrastructure.CacheRegistration).Assembly;

        var residue = infrastructureAssembly.GetTypes()
            .Where(t => t.Name.Contains("IdempotencyKeyRecord", StringComparison.Ordinal)
                || t.Name.Contains("DevNullIdempotency", StringComparison.Ordinal)
                || t.Name.Contains("LegacyIdempotency", StringComparison.Ordinal))
            .Select(t => t.FullName!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        residue.Should().BeEmpty(
            "legacy ops idempotency records, DevNull stores and legacy idempotency interfaces must be deleted after cutover");
    }

    [Fact]
    public void FZ_0008b_Legacy_Ops_Test_Project_Residue_Is_Deleted()
    {
        var backendRoot = Path.GetDirectoryName(GetSrcPath());
        var legacyTestDir = Path.Combine(backendRoot!, "tests", "Notrelix.Tests");

        Directory.Exists(legacyTestDir).Should().BeFalse(
            "the legacy Notrelix.Tests project (IdempotencyKeyRecordTests, JobLockRecordTests, ...) must be deleted");
    }

    // --- helpers ---

    private static IEnumerable<Type> GetHandlerTypes()
    {
        return ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(IRequestHandler<>) ||
                 i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))))
            .ToList();
    }

    private static string? GetContextFromHandlerNamespace(string? ns)
    {
        if (ns is null) return null;
        const string prefix = "Notrelix.Application.Features.";
        if (!ns.StartsWith(prefix, StringComparison.Ordinal)) return null;

        var remainder = ns[prefix.Length..];
        var dotIndex = remainder.IndexOf('.');
        return dotIndex > 0 ? remainder[..dotIndex] : remainder;
    }
}
