namespace Notrelix.Architecture.Tests;

public class ForbiddenLegacyNameArchitectureTests
{
    private static readonly string[] ForbiddenTableNames =
    [
        "collab.notifications",
        "collab.notification_preferences",
        "collab.notification_deliveries",
        "collab.unread_counters",
        "collab.activity_logs",
        "audit.activity_logs",
        "governance.audit_logs",
        "governance.security_events",
        "automation.outbox_messages",
        "ops.processed_events"
    ];

    private static readonly string[] ForbiddenNamespacePatterns =
    [
        "Notrelix.Domain.Collaboration.Notifications",
        "Notrelix.Domain.Governance.Audit",
        "Notrelix.Domain.Governance.Security"
    ];

    private static string GetSolutionPath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }
        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");
        return current;
    }

    [Fact]
    public void SourceCode_ShouldNotReference_ForbiddenLegacyTableNames()
    {
        var solutionPath = GetSolutionPath();
        var srcPath = Path.Combine(solutionPath, "src");

        if (!Directory.Exists(srcPath))
            return;

        var csFiles = Directory.GetFiles(srcPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"))
            .ToArray();

        var violations = new List<string>();

        foreach (var file in csFiles)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(srcPath, file);

            foreach (var tableName in ForbiddenTableNames)
            {
                if (content.Contains(tableName, StringComparison.OrdinalIgnoreCase))
                {
                    violations.Add($"{relativePath}: contains forbidden table name '{tableName}'");
                }
            }
        }

        violations.Should().BeEmpty(
            "Forbidden legacy table names must not appear in active source code. " +
            "These tables have been removed in Schema V1 refactor.");
    }

    [Fact]
    public void SourceCode_ShouldNotImport_ForbiddenLegacyNamespaces()
    {
        var solutionPath = GetSolutionPath();
        var srcPath = Path.Combine(solutionPath, "src");

        if (!Directory.Exists(srcPath))
            return;

        var csFiles = Directory.GetFiles(srcPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        var violations = new List<string>();

        foreach (var file in csFiles)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(srcPath, file);

            foreach (var ns in ForbiddenNamespacePatterns)
            {
                if (content.Contains($"using {ns}") || content.Contains($"global using {ns}"))
                {
                    violations.Add($"{relativePath}: imports forbidden namespace '{ns}'");
                }
            }
        }

        violations.Should().BeEmpty(
            "Forbidden legacy namespaces must not be imported. " +
            "These namespaces have been removed in Schema V1 refactor.");
    }

    [Fact]
    public void DomainLayer_ShouldNotContain_ForbiddenLegacyEntities()
    {
        var solutionPath = GetSolutionPath();
        var domainPath = Path.Combine(solutionPath, "src", "Notrelix.Domain");

        if (!Directory.Exists(domainPath))
            return;

        var forbiddenPaths = new[]
        {
            Path.Combine(domainPath, "Collaboration", "Notifications"),
            Path.Combine(domainPath, "Governance", "Audit"),
            Path.Combine(domainPath, "Governance", "Security"),
            Path.Combine(domainPath, "Notifications")
        };

        var violations = new List<string>();

        foreach (var path in forbiddenPaths)
        {
            if (Directory.Exists(path))
            {
                var csFiles = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
                if (csFiles.Length > 0)
                {
                    violations.Add($"Directory exists with {csFiles.Length} files: {Path.GetRelativePath(domainPath, path)}");
                }
            }
        }

        violations.Should().BeEmpty(
            "Forbidden legacy entity directories must not exist in Domain layer. " +
            "These have been removed or reclassified in Schema V1 refactor.");
    }

    [Fact]
    public void InfrastructureLayer_ShouldNotMap_ForbiddenLegacyTables()
    {
        var solutionPath = GetSolutionPath();
        var configPath = Path.Combine(solutionPath, "src", "Notrelix.Infrastructure", "Data", "Configurations");

        if (!Directory.Exists(configPath))
            return;

        var csFiles = Directory.GetFiles(configPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        var violations = new List<string>();

        foreach (var file in csFiles)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(configPath, file);

            foreach (var tableName in ForbiddenTableNames)
            {
                var parts = tableName.Split('.');
                var schemaName = parts[0];
                var shortName = parts[1];

                // Check for ToTable("table_name", DbSchemas.Schema) patterns
                // Must match both table name AND schema
                if (content.Contains($"ToTable(\"{shortName}\"") && content.Contains($"DbSchemas.{char.ToUpper(schemaName[0]) + schemaName[1..]}"))
                {
                    violations.Add($"{relativePath}: maps to forbidden table '{tableName}'");
                }
            }
        }

        violations.Should().BeEmpty(
            "EF configurations must not map to forbidden legacy tables. " +
            "These tables have been removed in Schema V1 refactor.");
    }
}
