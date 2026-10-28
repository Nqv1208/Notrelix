namespace Notrelix.Architecture.Tests;

/// <summary>
/// Guards the internal principals: background work connects as the worker login (never as the application
/// login with a self-declared scope), the application login reaches protected rows only through bounded
/// owner-defined functions, and each of those functions is a pinned, restricted SECURITY DEFINER function.
/// </summary>
public class InternalPrincipalArchitectureTests
{
    private static string Root()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null && !File.Exists(Path.Combine(dir, "backend.slnx")))
            dir = Directory.GetParent(dir)?.FullName;
        return dir ?? throw new InvalidOperationException("Could not find project root");
    }

    private static string InfrastructurePath(params string[] parts) =>
        Path.Combine([Root(), "src", "Notrelix.Infrastructure", .. parts]);

    private static string Sql()
    {
        var scripts = Path.Combine(InfrastructurePath("Data", "Rls", "RlsSqlScripts"));
        return Regex.Replace(
            string.Join("\n", Directory.GetFiles(scripts, "*.sql").OrderBy(f => f).Select(File.ReadAllText)),
            @"--[^\n]*", "");
    }

    private static IEnumerable<string> SourceFiles() =>
        Directory.GetFiles(Path.Combine(Root(), "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"));

    [Fact]
    public void EveryBackgroundService_CreatesItsScopesAsTheWorkerPrincipal()
    {
        var offenders = new List<string>();
        var checkedServices = 0;
        foreach (var file in SourceFiles())
        {
            var text = File.ReadAllText(file);
            if (!Regex.IsMatch(text, @"class\s+\w+\s*:\s*BackgroundService\b"))
                continue;

            checkedServices++;
            if (Regex.IsMatch(text, @"\.CreateAsyncScope\(\)|\.CreateScope\(\)"))
                offenders.Add(Path.GetFileName(file));
            if (!text.Contains("CreateWorkerAsyncScope()"))
                offenders.Add(Path.GetFileName(file) + " (no worker scope)");
        }

        checkedServices.Should().BeGreaterThanOrEqualTo(6, "the dispatchers, sweeps and reapers are background services");
        offenders.Should().BeEmpty(
            "a background loop must create its dependency-injection scope with CreateWorkerAsyncScope() so its " +
            "database context connects as the worker login, never as the application login");
    }

    [Fact]
    public void EveryConsumeFilterRegistration_PutsTheWorkerPrincipalFilterFirst()
    {
        var text = File.ReadAllText(InfrastructurePath("DependencyInjection", "MessagingRegistration.cs"));
        var registrations = Regex.Matches(text, @"UseConsumeFilter\(typeof\((\w+)<>\), ctx\)")
            .Select(m => m.Groups[1].Value)
            .ToList();

        registrations.Should().Contain("WorkerPrincipalConsumeFilter");
        var transports = registrations.Count(r => r == "TenantContextConsumeFilter");
        transports.Should().BeGreaterThanOrEqualTo(2);

        for (var i = 0; i < registrations.Count; i++)
        {
            if (registrations[i] is "TenantContextConsumeFilter" or "DeduplicationConsumeFilter")
            {
                registrations.Take(i).Should().Contain("WorkerPrincipalConsumeFilter",
                    "the consume scope's context is fixed when the first filter resolves it, so the worker " +
                    "principal filter must be registered before any filter that uses the database");
            }
        }
    }

    [Fact]
    public void SystemContextScope_IsUsedOnlyByBackgroundJobs()
    {
        var offenders = SourceFiles()
            // CurrentWorkspace.cs declares its own nested scope type of the same name (an ambient-context guard).
            .Where(f => Path.GetFileName(f) != "CurrentWorkspace.cs")
            .Where(f => File.ReadAllText(f).Contains("new SystemContextScope("))
            .Where(f => !f.Replace('\\', '/').Contains("/Notrelix.Infrastructure/BackgroundJobs/"))
            .Select(Path.GetFileName)
            .ToList();

        offenders.Should().BeEmpty(
            "a system scope is the background principal; request-serving code must use a bounded scope or an " +
            "owner-defined function instead");
    }

    [Fact]
    public void SetSystem_IsCalledOnlyByTheKnownEntryPoints()
    {
        var callers = SourceFiles()
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"\.SetSystem\(\)\s*;"))
            .Select(Path.GetFileName)
            .OrderBy(x => x)
            .ToList();

        callers.Should().BeEquivalentTo(new[]
        {
            "ApiTokenAuthenticationHandler.cs",        // query-filter bypass only; the database scope is the token scope
            "ApplicationDbContextInitialiser.cs",      // seed, on the owner connection
            "SystemContextScope.cs",                   // background jobs (worker connection)
            "TenantContextConsumeFilter.cs",           // consumers of tenantless events (worker connection)
        });
    }

    [Fact]
    public void TheApiTokenHandler_EntersTheTokenScope_AndNeverTheWorkerScope()
    {
        var text = File.ReadAllText(InfrastructurePath("Auth", "ApiTokens", "ApiTokenAuthenticationHandler.cs"));
        text.Should().Contain("TokenScopePurpose.ApiToken");
        text.Should().NotContain("SystemContextScope");
    }

    private static readonly (string Function, string Signature, string Grantees)[] BoundedFunctions =
    [
        ("ops.record_security_event", "text, text, text, integer, text, text, text, uuid, text, uuid, text, jsonb, timestamptz", "notrelix_app, notrelix_worker"),
        ("ops.upsert_access_grant", "uuid, uuid, uuid, text, timestamptz", "notrelix_app"),
        ("ops.revoke_access_grant", "uuid, uuid, uuid, timestamptz", "notrelix_app"),
        ("ops.disclosable_member_emails", "uuid, uuid[]", "notrelix_app"),
        ("ops.shared_member_phones", "uuid, uuid[]", "notrelix_app"),
        ("ops.has_api_token_binding", "uuid, uuid", "notrelix_app"),
    ];

    [Fact]
    public void EveryBoundedFunction_IsDefinerWithPinnedSearchPath_AndGrantedToItsRolesOnly()
    {
        var all = Sql();
        var normalized = Regex.Replace(Regex.Replace(all, @"\s+", " "), @"\( ", "(");
        foreach (var (function, signature, grantees) in BoundedFunctions)
        {
            var body = Regex.Match(
                all, $@"CREATE OR REPLACE FUNCTION {Regex.Escape(function)}\(.*?\$\$;", RegexOptions.Singleline).Value;
            body.Should().NotBeNullOrEmpty($"{function} must exist");
            body.Should().Contain("SECURITY DEFINER", $"{function} must run as its owner");
            body.Should().MatchRegex(@"SET search_path = pg_catalog, [a-z_, ]*pg_temp", $"{function} must pin search_path");
            body.Should().NotContainEquivalentOf("EXECUTE format", $"{function} must not build dynamic SQL");

            normalized.Should().Contain($"REVOKE ALL ON FUNCTION {function}({signature}) FROM PUBLIC",
                $"{function} must not be executable by PUBLIC");
            var grants = Regex.Matches(normalized,
                $@"GRANT EXECUTE ON FUNCTION {Regex.Escape(function)}\({Regex.Escape(signature)}\) TO ([^;]+);");
            grants.Should().ContainSingle($"{function} must be granted once");
            grants[0].Groups[1].Value.Trim().Should().Be(grantees, $"{function} is granted to {grantees} only");
        }
    }

    [Fact]
    public void TheSecurityEventFunction_IsInsertOnly()
    {
        var body = Regex.Match(
            Sql(), @"CREATE OR REPLACE FUNCTION ops\.record_security_event\(.*?\$\$;", RegexOptions.Singleline).Value;

        body.Should().Contain("INSERT INTO audit.security_events");
        body.Should().NotMatchRegex(@"\b(UPDATE|DELETE|TRUNCATE)\b", "the function writes one row and reads nothing back");
        body.Should().NotMatchRegex(@"\bSELECT\b[^;]*\bFROM\b", "the function reads no table");
        body.Should().NotMatchRegex(@"\bp_user\b|\bp_user_id\b", "the actor is derived from the scope, never supplied");
        body.Should().NotMatchRegex(@"\bp_workspace\b|\bp_workspace_id\b", "the tenant is derived from the scope, never supplied");
    }

    [Fact]
    public void AccessGrants_KeepNoBroadWritePolicyForTheApplicationRole()
    {
        var all = Sql();
        var policies = Regex.Matches(
                all, @"create_policy\(\s*'authz',\s*'access_grants',\s*'(\w+)',\s*'(\w+)',\s*'notrelix_app'",
                RegexOptions.IgnoreCase)
            .Select(m => $"{m.Groups[1].Value}:{m.Groups[2].Value}")
            .ToList();

        policies.Should().BeEquivalentTo(new[] { "p_app_select_own:SELECT", "p_app_update_revoke_own:UPDATE" },
            "the application role reads its own grants and ends its own (leave); every other grant write goes through " +
            "ops.upsert_access_grant / ops.revoke_access_grant");
    }

    [Fact]
    public void SecurityEvents_KeepNoReadWriteBackdoorForTheApplicationRole()
    {
        var all = Sql();
        all.Should().NotMatchRegex(
            @"create_policy\(\s*'audit',\s*'security_events',\s*'\w+',\s*'(UPDATE|DELETE|ALL)',\s*'notrelix_app'",
            "no policy gives the application role update or delete on security events");
    }
}
