using System.Reflection;

namespace Notrelix.Architecture.Tests.InfrastructureLayer;

/// <summary>
/// INF-07 / DI-001..002: Production adapter graph verification.
/// Critical Application ports must resolve to real Infrastructure implementations.
/// Production types must not contain DevNull/NoOp/Fake/Stub/Mock/Placeholder.
/// </summary>
public class ProductionAdapterGraphTests
{
    private static readonly Assembly InfrastructureAssembly =
        typeof(Notrelix.Infrastructure.Data.ApplicationDbContext).Assembly;

    private static readonly string[] ForbiddenProductionSuffixes =
    [
        "DevNull",
        "NoOp",
        "Fake",
        "Stub",
        "Mock",
        "Placeholder",
    ];

    /// <summary>
    /// Types that exist in Infrastructure but are conditionally registered only in
    /// Development/Testing with an environment guard that throws in Production.
    /// </summary>
    private static readonly HashSet<string> ConditionallyRegisteredDevTypes = new(StringComparer.Ordinal)
    {
        "Notrelix.Infrastructure.Billing.DevNullEntitlementChecker",
        "Notrelix.Infrastructure.Billing.DevNullSubscriptionChecker",
        "Notrelix.Infrastructure.Billing.DevNullFeatureGateChecker",
        "Notrelix.Infrastructure.Email.NoopEmailService",
        "Notrelix.Infrastructure.Integrations.Providers.NoopN8nClient",
        "Notrelix.Infrastructure.DevNullIntegrationEventBus",
        "Notrelix.Infrastructure.Messaging.DevNullRealtimePublisher",
    };

    [Fact]
    public void DI_001_No_Fake_Or_DevNull_Types_In_Production_Infrastructure()
    {
        var productionTypes = InfrastructureAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true })
            .ToList();

        var violations = productionTypes
            .Where(t => ForbiddenProductionSuffixes.Any(suffix =>
                t.Name.Contains(suffix, StringComparison.OrdinalIgnoreCase)))
            .Where(t => !ConditionallyRegisteredDevTypes.Contains(t.FullName!))
            .Select(t => t.FullName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        violations.Should().BeEmpty(
            "production Infrastructure assembly must not contain DevNull/NoOp/Fake/Stub/Mock/Placeholder types. " +
            "Tests must register fakes explicitly in their own composition root.");
    }

    [Fact]
    public void DI_001_Critical_Ports_Have_Real_Implementations()
    {
        var criticalPortNames = new[]
        {
            "IIdempotencyStore",
            "IRequestDataSession",
            "IRlsSessionContext",
            "IRedisCacheService",
            "IRealtimePublisher",
        };

        var implementationTypes = InfrastructureAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .ToList();

        var missing = new List<string>();

        foreach (var portName in criticalPortNames)
        {
            var hasImpl = implementationTypes.Any(t =>
                t.GetInterfaces().Any(i => i.Name == portName));

            if (!hasImpl)
            {
                missing.Add(portName);
            }
        }

        missing.Should().BeEmpty(
            "every critical Application port must have a production Infrastructure implementation");
    }

    [Fact]
    public void DI_002_IdempotencyStore_Implementation_Is_Not_Fake()
    {
        var storeImpl = InfrastructureAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i => i.Name == "IIdempotencyStore"))
            .ToList();

        storeImpl.Should().NotBeEmpty("IIdempotencyStore must have a production implementation");

        foreach (var impl in storeImpl)
        {
            impl.Name.Should().NotContainAny(ForbiddenProductionSuffixes,
                $"idempotency store implementation '{impl.Name}' must be a real adapter");
        }
    }

    // ------------------------------------------------------------------
    // PRE-M4 CrossContext runtime composition proof (tests.md §24K2 / P2/P4).
    //
    // Cross-context Port/Adapter runtime registrations must be owned by the
    // dedicated CrossContextRegistration/AddCrossContextBindings owner, not by
    // PersistenceRegistration. Persistence mappings (I*DbContext ->
    // ApplicationDbContext) remain allowed in PersistenceRegistration.
    // ------------------------------------------------------------------

    /// <summary>
    /// The exact PRE-M4 cross-context runtime bindings by simple type name
    /// (port -> adapter). These must be registered by CrossContextRegistration
    /// and must NOT be owned by PersistenceRegistration.
    /// </summary>
    private static readonly IReadOnlyList<(string Port, string Adapter)> CrossContextBindings =
    [
        ("IWorkManagementCollaborationReadPort", "WorkManagementCollaborationReadAdapter"),
        ("IIdentityBootstrapReadPort", "IdentityBootstrapReadAdapter"),
        ("IWorkActionPort", "WorkItemActionAdapter"),
        ("IWorkItemProjectionSource", "WorkItemProjectionSourceAdapter"),
        ("IWorkItemProjectionSourceAdapter", "WorkItemProjectionSourceAdapter"),
    ];

    [Fact]
    public void CrossContextRegistration_Owns_AllCrossContextRuntimeBindings()
    {
        var source = ReadRegistrationSource("DependencyInjection/CrossContextRegistration.cs");
        var found = ExtractScopedBindings(source);

        var expected = CrossContextBindings.ToHashSet();
        var missing = expected.Except(found).ToList();
        var extra = found.Except(expected).ToList();

        missing.Should().BeEmpty(
            "PRE-M4: CrossContextRegistration must register every cross-context runtime binding. Missing:\n" +
            string.Join("\n", missing.Select(b => $"  {b.Port} -> {b.Adapter}")));

        extra.Should().BeEmpty(
            "PRE-M4: CrossContextRegistration should not own non-cross-context registrations. Extra:\n" +
            string.Join("\n", extra.Select(b => $"  {b.Port} -> {b.Adapter}")));
    }

    [Fact]
    public void PersistenceRegistration_DoesNotOwn_CrossContextRuntimeBindings()
    {
        var source = ReadRegistrationSource("DependencyInjection/PersistenceRegistration.cs");
        var found = ExtractScopedBindings(source);
        var expected = CrossContextBindings.ToHashSet();

        var violations = found.Where(expected.Contains).ToList();

        // Scoped negative assertion: ONLY the explicit cross-context bindings
        // must not live here. No blanket "persistence may contain only
        // persistence types" rule is asserted.
        violations.Should().BeEmpty(
            "PRE-M4: PersistenceRegistration must not own cross-context runtime adapters (they " +
            "belong in CrossContextRegistration). Violations:\n" +
            string.Join("\n", violations.Select(b => $"  {b.Port} -> {b.Adapter}")));
    }

    // ------------------------------------------------------------------
    // Gate self-tests — regression fixtures for the composition classifier.
    // ------------------------------------------------------------------

    [Fact]
    public void Gate_Classifies_CorrectCrossContextPlacement()
    {
        const string syntheticCrossContext =
            """
            namespace Notrelix.Infrastructure;
            public static class CrossContextRegistration
            {
                public static IServiceCollection AddCrossContextBindings(this IServiceCollection services)
                {
                    services.AddScoped<IWorkManagementCollaborationReadPort, WorkManagementCollaborationReadAdapter>();
                    services.AddScoped<IIdentityBootstrapReadPort, IdentityBootstrapReadAdapter>();
                    services.AddScoped<IWorkActionPort, WorkItemActionAdapter>();
                    services.AddScoped<
                        Notrelix.Application.Features.WorkManagement.Public.Queries.IWorkItemProjectionSource,
                        WorkItemProjectionSourceAdapter>();
                    services.AddScoped<
                        Notrelix.Infrastructure.Messaging.Consumers.Analytics.IWorkItemProjectionSourceAdapter,
                        WorkItemProjectionSourceAdapter>();
                    return services;
                }
            }
            """;

        var found = ExtractScopedBindings(syntheticCrossContext);

        CrossContextBindings.ToHashSet().Except(found).Should().BeEmpty(
            "correct placement must register every cross-context binding");
    }

    [Fact]
    public void Gate_Detects_CrossContextBinding_InPersistence()
    {
        // The same adapter binding placed in PersistenceRegistration must be
        // classified as owned by it, so the scoped negative assertion can fail.
        const string syntheticPersistence =
            """
            namespace Notrelix.Infrastructure;
            public static class PersistenceRegistration
            {
                public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration c)
                {
                    services.AddDbContext<ApplicationDbContext>((sp, o) => { });
                    services.AddScoped<IWorkActionPort, WorkItemActionAdapter>();
                    return services;
                }
            }
            """;

        var found = ExtractScopedBindings(syntheticPersistence);
        var expected = CrossContextBindings.ToHashSet();

        found.Where(expected.Contains).Should().NotBeEmpty(
            "a cross-context binding in PersistenceRegistration must be detected so the proof can fail");
    }

    /// <summary>Reads an Infrastructure source file relative to the project root.</summary>
    private static string ReadRegistrationSource(string relativePath)
    {
        var infraDir = Path.GetFullPath(
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "..", "src", "Notrelix.Infrastructure"));

        var fullPath = Path.Combine(infraDir, relativePath);
        File.Exists(fullPath).Should().BeTrue($"Infrastructure source file expected at {fullPath}");

        return File.ReadAllText(fullPath);
    }

    /// <summary>
    /// Extracts (port, implementation) type-name pairs from every
    /// <c>AddScoped&lt;...&gt;</c> registration in the given source, matched by
    /// simple type name (namespace-agnostic) so fully-qualified and bare
    /// type references are equivalent.
    /// </summary>
    private static IReadOnlyList<(string Port, string Adapter)> ExtractScopedBindings(string source)
    {
        const string marker = "AddScoped<";
        var results = new List<(string, string)>();

        var idx = 0;
        while ((idx = source.IndexOf(marker, idx, StringComparison.Ordinal)) >= 0)
        {
            var open = idx + marker.Length;
            var depth = 0;
            var i = open;
            while (i < source.Length)
            {
                if (source[i] == '<') depth++;
                else if (source[i] == '>')
                {
                    if (depth == 0) break;
                    depth--;
                }

                i++;
            }

            var args = source[open..i];
            var comma = FindTopLevelComma(args);
            if (comma > 0)
            {
                var portType = SimpleName(args[..comma]);
                var implType = SimpleName(args[(comma + 1)..]);
                results.Add((portType, implType));
            }

            idx = i;
        }

        return results;
    }

    private static int FindTopLevelComma(string s)
    {
        var depth = 0;
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c is '<' or '(' or '[' or '{') depth++;
            else if (c is '>' or ')' or ']' or '}') depth--;
            else if (c == ',' && depth == 0) return i;
        }

        return -1;
    }

    private static string SimpleName(string typeRef)
    {
        typeRef = typeRef.Trim();
        var generic = typeRef.IndexOf('<');
        if (generic >= 0) typeRef = typeRef[..generic];
        var lastDot = typeRef.LastIndexOf('.');
        return lastDot >= 0 ? typeRef[(lastDot + 1)..] : typeRef;
    }
}
