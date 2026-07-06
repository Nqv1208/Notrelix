namespace Notrelix.Architecture.Tests;

public class DbContextBoundaryArchitectureTests
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

    private static string[] GetHandlerFiles()
    {
        var appPath = GetApplicationPath();
        return Directory.GetFiles(Path.Combine(appPath, "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && f.Contains("Handler.cs"))
            .ToArray();
    }

    private static string RemoveComments(string input)
    {
        var blockComments = @"/\*(.*?)\*/";
        var lineComments = @"//(.*?)\r?\n";
        var cleaned = Regex.Replace(input, blockComments, "", RegexOptions.Singleline);
        cleaned = Regex.Replace(cleaned, lineComments, "\n");
        return cleaned;
    }

    private static readonly Dictionary<string, string> ModuleToExpectedContext = new()
    {
        ["Workspaces"] = "IWorkspaceDbContext",
        ["WorkManagement"] = "IWorkManagementDbContext",
        ["Identity"] = "IIdentityDbContext",
        ["Documents"] = "IDocumentDbContext",
        ["Collaboration"] = "ICollaborationDbContext",
        ["Governance"] = "IGovernanceDbContext",
        ["Automation"] = "IAutomationDbContext",
        ["Integrations"] = "IIntegrationDbContext",
        ["Billing"] = "IBillingDbContext",
        ["Accounts"] = "IAccountDbContext",
    };

    private static string[] AllForbiddenContexts => ModuleToExpectedContext.Values.ToArray();

    private static readonly HashSet<string> KnownCrossModuleViolations = [];

    [Theory]
    [InlineData("Workspaces", "IWorkManagementDbContext")]
    [InlineData("Workspaces", "IIdentityDbContext")]
    [InlineData("WorkManagement", "IWorkspaceDbContext")]
    [InlineData("WorkManagement", "IIdentityDbContext")]
    [InlineData("Identity", "IWorkspaceDbContext")]
    [InlineData("Identity", "IWorkManagementDbContext")]
    [InlineData("Documents", "IWorkManagementDbContext")]
    [InlineData("Documents", "IWorkspaceDbContext")]
    [InlineData("Documents", "ICollaborationDbContext")]
    [InlineData("Documents", "IIdentityDbContext")]
    [InlineData("Collaboration", "IWorkManagementDbContext")]
    [InlineData("Collaboration", "IWorkspaceDbContext")]
    [InlineData("Collaboration", "IDocumentDbContext")]
    [InlineData("Collaboration", "IIdentityDbContext")]
    [InlineData("Governance", "IWorkManagementDbContext")]
    [InlineData("Governance", "IWorkspaceDbContext")]
    [InlineData("Governance", "ICollaborationDbContext")]
    [InlineData("Automation", "IWorkManagementDbContext")]
    [InlineData("Automation", "IWorkspaceDbContext")]
    [InlineData("Automation", "IBillingDbContext")]
    [InlineData("Integrations", "IWorkManagementDbContext")]
    [InlineData("Integrations", "IWorkspaceDbContext")]
    [InlineData("Billing", "IWorkManagementDbContext")]
    [InlineData("Billing", "IWorkspaceDbContext")]
    [InlineData("Accounts", "IWorkManagementDbContext")]
    [InlineData("Accounts", "IWorkspaceDbContext")]
    [InlineData("Accounts", "IIdentityDbContext")]
    public void Handlers_ShouldNotInject_CrossModuleDbContext(string module, string forbiddenContext)
    {
        var violations = AssertModuleContextBoundaries(module, forbiddenContext);
        violations.Should().BeEmpty(
            $"{module} handlers should not inject {forbiddenContext}. " +
            $"Use {ModuleToExpectedContext.GetValueOrDefault(module, "the module's own DbContext")} " +
            $"instead. Violations: {string.Join(", ", violations)}");
    }

    private static List<string> AssertModuleContextBoundaries(string module, string forbiddenContext)
    {
        var files = GetHandlerFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var featuresIndex = file.IndexOf("Features", StringComparison.Ordinal);
            if (featuresIndex < 0) continue;

            var relativePath = file.Substring(featuresIndex);
            if (!relativePath.Contains($"{Path.DirectorySeparatorChar}{module}{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;

            var content = RemoveComments(File.ReadAllText(file));
            var fileName = Path.GetFileName(file);

            if (KnownCrossModuleViolations.Contains(fileName))
                continue;

            if (content.Contains(forbiddenContext))
            {
                violations.Add($"{fileName} injects {forbiddenContext}");
            }
        }

        return violations;
    }
}
