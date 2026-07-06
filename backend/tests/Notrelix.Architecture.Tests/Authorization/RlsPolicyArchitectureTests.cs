namespace Notrelix.Architecture.Tests;

/// <summary>
/// Enforces RLS policy rules:
/// - RlsPolicyApplier must run 001-011, not 000
/// - Missing resource must throw, not skip
/// </summary>
public class RlsPolicyArchitectureTests
{
    [Fact]
    public void RlsPolicyApplier_ShouldNotRun_000_apply_order()
    {
        var source = File.ReadAllText(Path.Combine(
            FindProjectRoot(), "src", "Notrelix.Infrastructure", "Data", "Rls", "RlsPolicyApplier.cs"));

        source.Should().NotContain("000_apply_order",
            "RlsPolicyApplier must not execute 000_apply_order.sql (psql meta-commands only)");
    }

    [Fact]
    public void RlsPolicyApplier_ShouldRun_001_Through_011()
    {
        var source = File.ReadAllText(Path.Combine(
            FindProjectRoot(), "src", "Notrelix.Infrastructure", "Data", "Rls", "RlsPolicyApplier.cs"));

        // Verify all 11 scripts are listed
        for (int i = 1; i <= 11; i++)
        {
            var padded = i.ToString("D3");
            source.Should().Contain(padded,
                $"RlsPolicyApplier must include script {padded}");
        }
    }

    [Fact]
    public void RlsPolicyApplier_ShouldThrow_OnMissingResource()
    {
        var source = File.ReadAllText(Path.Combine(
            FindProjectRoot(), "src", "Notrelix.Infrastructure", "Data", "Rls", "RlsPolicyApplier.cs"));

        // Must throw, not just warn and skip
        source.Should().NotContain("Skipping",
            "RlsPolicyApplier must throw on missing resource, not skip");
        source.Should().Contain("throw new InvalidOperationException",
            "RlsPolicyApplier must throw when resource is missing");
    }

    [Fact]
    public void RlsSplitScripts_ShouldNotContain_PsqlMetaCommands()
    {
        var scriptsPath = Path.Combine(
            FindProjectRoot(), "src", "Notrelix.Infrastructure", "Data", "Rls", "RlsSqlScripts");

        var scripts = Directory.GetFiles(scriptsPath, "0[0-1][0-9]_*.sql");

        foreach (var script in scripts)
        {
            var content = File.ReadAllText(script);
            var fileName = Path.GetFileName(script);

            // 000 is the psql-only file, skip it
            if (fileName.StartsWith("000_")) continue;

            // Check for psql meta-commands
            content.Should().NotContain("\\i ",
                $"Script {fileName} must not contain psql meta-commands");
            content.Should().NotContain("\\echo ",
                $"Script {fileName} must not contain psql meta-commands");
        }
    }

    private static string FindProjectRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null && !File.Exists(Path.Combine(dir, "backend.slnx")))
            dir = Directory.GetParent(dir)?.FullName;
        return dir ?? throw new InvalidOperationException("Could not find project root");
    }
}