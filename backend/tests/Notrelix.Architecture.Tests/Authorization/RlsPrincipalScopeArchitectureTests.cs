namespace Notrelix.Architecture.Tests;

/// <summary>
/// Guards the shape of the bounded principal scopes: they are never members of the unbounded worker scope,
/// and every scope entry function is an owner-defined SECURITY DEFINER function with a pinned search_path,
/// no dynamic SQL and execution restricted to the application role.
/// </summary>
public class RlsPrincipalScopeArchitectureTests
{
    private static readonly string[] BoundedScopes = ["user", "token", "credential", "registration"];

    private static string ScriptsPath()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null && !File.Exists(Path.Combine(dir, "backend.slnx")))
            dir = Directory.GetParent(dir)?.FullName;
        return Path.Combine(
            dir ?? throw new InvalidOperationException("Could not find project root"),
            "src", "Notrelix.Infrastructure", "Data", "Rls", "RlsSqlScripts");
    }

    private static string Read(string name) => File.ReadAllText(Path.Combine(ScriptsPath(), name));

    [Fact]
    public void WorkerScopeHelper_DoesNotAdmitAnyBoundedScope()
    {
        var helper = Regex.Match(
            Read("002_context_helpers.sql"),
            @"FUNCTION ops\.is_worker_scope\(\).*?\$\$;",
            RegexOptions.Singleline).Value;

        helper.Should().NotBeNullOrEmpty();
        var admitted = Regex.Matches(helper, @"'([a-z_]+)'").Select(m => m.Groups[1].Value).ToArray();

        admitted.Should().BeEquivalentTo(["worker", "system", "migration", "migrator"],
            "ops.is_worker_scope() must not be widened (a worker scope is an unbounded all-tenant bypass)");
        admitted.Should().NotIntersectWith(BoundedScopes);
    }

    [Fact]
    public void NoScriptTreatsABoundedScopeAsWorker()
    {
        foreach (var file in Directory.GetFiles(ScriptsPath(), "*.sql"))
        {
            var text = Regex.Replace(File.ReadAllText(file), @"--[^\n]*", "");
            foreach (var scope in BoundedScopes)
            {
                text.Should().NotMatchRegex(
                    $@"is_worker_scope[^;]*'{scope}'",
                    $"{Path.GetFileName(file)} must not equate the {scope} scope with the worker scope");
            }
        }
    }

    [Fact]
    public void EveryScopeEntryFunction_IsDefinerWithPinnedSearchPath_NoDynamicSql_AndRestrictedExecute()
    {
        var all = Regex.Replace(
            string.Join("\n", Directory.GetFiles(ScriptsPath(), "*.sql").Select(File.ReadAllText)), @"--[^\n]*", "");
        var entries = Regex.Matches(
            all,
            @"CREATE OR REPLACE FUNCTION (ops\.enter_[a-z_]+)\(([^)]*)\)(.*?)\$\$;",
            RegexOptions.Singleline);

        entries.Should().NotBeEmpty("at least the token scope entry function must exist");

        foreach (Match entry in entries)
        {
            var name = entry.Groups[1].Value;
            var signature = string.Join(", ", entry.Groups[2].Value.Split(',').Select(p => p.Trim().Split(' ')[1]));
            var body = entry.Groups[3].Value;

            body.Should().Contain("SECURITY DEFINER", $"{name} must run as its owner");
            body.Should().MatchRegex(@"SET search_path = pg_catalog, ops, pg_temp", $"{name} must pin search_path");
            body.Should().NotContainEquivalentOf("EXECUTE format", $"{name} must not build dynamic SQL");
            body.Should().NotMatchRegex(@"\bEXECUTE\s+'", $"{name} must not build dynamic SQL");

            all.Should().Contain($"REVOKE ALL ON FUNCTION {name}({signature}) FROM PUBLIC",
                $"{name} must not be executable by PUBLIC");
            var grants = Regex.Matches(all, $@"GRANT EXECUTE ON FUNCTION {Regex.Escape(name)}\({Regex.Escape(signature)}\) TO ([^;]+);");
            grants.Should().ContainSingle($"{name} must be granted once");
            grants[0].Groups[1].Value.Trim().Should().Be("notrelix_app", $"{name} is for the application role only");
        }
    }

    [Fact]
    public void BoundedScopeHelpers_AreDefinerWithPinnedSearchPath()
    {
        var script = Read("012_principal_scopes.sql");
        foreach (var name in new[] { "ops.has_user_scope", "ops.has_token_scope" })
        {
            var body = Regex.Match(script, $@"FUNCTION {Regex.Escape(name)}\(.*?\$\$;", RegexOptions.Singleline).Value;
            body.Should().Contain("SECURITY DEFINER").And.Contain("SET search_path = pg_catalog, ops, pg_temp");
            body.Should().Contain("ops.current_request_scope()", $"{name} must check the scope kind, not only the values");
        }
    }
}
