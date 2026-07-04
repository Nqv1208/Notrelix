namespace Notrelix.Architecture.Tests;

public class DbContextBoundaryTests
{
    private static string GetApplicationPath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }
        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");
        return Path.Combine(current, "src", "Notrelix.Application");
    }

    /// <summary>
    /// Existing handlers that legitimately access multiple bounded contexts.
    /// These are known cross-context dependencies that should be migrated
    /// to service/projection/event patterns over time.
    /// </summary>
    private static readonly HashSet<string> CrossContextAllowlist =
    [
        "GetBootstrap.cs",
        "RegisterCommandHandler.cs",
        "GetBoardItem.cs",
        "GetFullBoard.cs",
    ];

    [Fact]
    public void NewHandlers_ShouldNotInjectGlobalIApplicationDbContext()
    {
        var appPath = GetApplicationPath();
        var violations = new List<string>();

        foreach (var file in Directory.GetFiles(Path.Combine(appPath, "Features"), "*Handler.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                continue;

            var content = File.ReadAllText(file);
            if (content.Contains("IApplicationDbContext"))
            {
                violations.Add(Path.GetFileName(file));
            }
        }

        violations.Should().BeEmpty(
            $"Handlers must not inject IApplicationDbContext. Use bounded-context interfaces " +
            $"(IWorkManagementDbContext, IDocumentDbContext, etc.). Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void CrossContextDbContextInjection_ShouldBeAllowlisted()
    {
        // Verify that the 4 known cross-context violations are still the only ones.
        // If this test fails, a NEW cross-context dependency was introduced without review.
        var appPath = GetApplicationPath();
        var violations = new List<string>();

        var contextToFeature = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["IIdentityDbContext"] = "Identity",
            ["IWorkspaceDbContext"] = "Workspaces",
            ["IWorkManagementDbContext"] = "WorkManagement",
            ["IDocumentDbContext"] = "Documents",
            ["ICollaborationDbContext"] = "Collaboration",
            ["IGovernanceDbContext"] = "Governance",
            ["IBillingDbContext"] = "Billing",
            ["IAutomationDbContext"] = "Automation",
            ["IIntegrationDbContext"] = "Integrations",
            ["IReportingDbContext"] = "Analytics",
            ["IAccountDbContext"] = "Accounts",
        };

        foreach (var file in Directory.GetFiles(Path.Combine(appPath, "Features"), "*Handler.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                continue;

            var content = File.ReadAllText(file);
            var fileName = Path.GetFileName(file);

            // Determine handler's bounded context from path
            var relativePath = Path.GetRelativePath(appPath, file);
            var parts = relativePath.Split(Path.DirectorySeparatorChar);
            var handlerContextIndex = Array.FindIndex(parts, p =>
                contextToFeature.Values.Any(v =>
                    p.Equals(v, StringComparison.OrdinalIgnoreCase)));
            var handlerContext = handlerContextIndex >= 0 ? parts[handlerContextIndex] : null;

            foreach (var (contextInterface, featureName) in contextToFeature)
            {
                if (!content.Contains(contextInterface)) continue;

                // Allow same-context usage
                if (handlerContext != null &&
                    handlerContext.Equals(featureName, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Allowlisted cross-context usage
                if (CrossContextAllowlist.Contains(fileName)) continue;

                violations.Add($"{fileName} uses {contextInterface} (belongs to {featureName}) from {handlerContext ?? "unknown"} context");
            }
        }

        violations.Should().BeEmpty(
            $"New cross-context DbContext injection detected. " +
            $"Use service/projection/event patterns instead. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void BoundedContextInterfacesExist()
    {
        var appPath = GetApplicationPath();
        var dbContextTypes = new[]
        {
            "IIdentityDbContext",
            "IWorkspaceDbContext",
            "IWorkManagementDbContext",
            "IDocumentDbContext",
            "ICollaborationDbContext",
            "IGovernanceDbContext",
            "IBillingDbContext",
            "IAutomationDbContext",
            "IIntegrationDbContext",
            "IReportingDbContext",
            "IAccountDbContext",
            "IApplicationDbContext",
        };

        var violations = new List<string>();

        foreach (var typeName in dbContextTypes)
        {
            var files = Directory.GetFiles(appPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                         && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                .Where(f => File.ReadAllText(f).Contains($"interface {typeName}"));

            if (!files.Any())
            {
                violations.Add($"Interface {typeName} is not defined in any Application file");
            }
        }

        violations.Should().BeEmpty(
            $"All bounded-context DbContext interfaces must exist. Violations: {string.Join(", ", violations)}");
    }
}
