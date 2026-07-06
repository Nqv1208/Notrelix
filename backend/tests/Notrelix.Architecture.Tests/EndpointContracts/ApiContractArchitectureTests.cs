namespace Notrelix.Architecture.Tests;

public class ApiContractArchitectureTests
{
    private static string GetApiPath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }
        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");
        return Path.Combine(current, "src", "Notrelix.API");
    }

    private static string[] GetEndpointFiles()
    {
        var apiPath = GetApiPath();
        var endpointsDir = Path.Combine(apiPath, "Endpoints");
        if (!Directory.Exists(endpointsDir))
            return [];

        return Directory.GetFiles(endpointsDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
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

    private static readonly HashSet<string> KnownDomainExposure =
    [
        // Classification: LegacyGap — endpoints directly reference Domain entities for enum/type resolution
        // Target state: Use Application-layer enums or DTOs instead of Domain types
        "MapBoardEndpoints.cs",
        "SaveBoardViewEndpoint.cs",
        "CreateBoardEndpoint.cs",
        "UpdateMemberRoleEndpoint.cs",
        "InviteMemberEndpoint.cs",
        "CreateBlockEndpoint.cs",
    ];

    [Fact]
    public void EndpointFiles_ShouldNotDirectlyInject_DbContext()
    {
        var files = GetEndpointFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));

            if (content.Contains("IApplicationDbContext") ||
                content.Contains("ApplicationDbContext") ||
                content.Contains("IWorkspaceDbContext") ||
                content.Contains("IWorkManagementDbContext") ||
                content.Contains("IIdentityDbContext"))
            {
                violations.Add(Path.GetRelativePath(GetApiPath(), file));
            }
        }

        violations.Should().BeEmpty(
            $"Endpoint files must not directly inject DbContext interfaces. " +
            $"Use Application layer abstractions instead. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void EndpointFiles_ShouldNotReference_EntityFrameworkCore()
    {
        var files = GetEndpointFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));

            if (content.Contains("using Microsoft.EntityFrameworkCore"))
            {
                violations.Add(Path.GetRelativePath(GetApiPath(), file));
            }
        }

        violations.Should().BeEmpty(
            $"Endpoint files must not reference Entity Framework Core. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void EndpointFiles_ShouldNotInject_DomainEntities()
    {
        var files = GetEndpointFiles();
        var violations = new List<string>();

        var domainEntityPatterns = new[]
        {
            "Notrelix.Domain.WorkManagement",
            "Notrelix.Domain.Workspaces",
            "Notrelix.Domain.Documents",
            "Notrelix.Domain.Collaboration",
            "Notrelix.Domain.Governance",
            "Notrelix.Domain.Billing",
            "Notrelix.Domain.Identity",
            "Notrelix.Domain.Automation",
            "Notrelix.Domain.Integrations",
            "Notrelix.Domain.Analytics",
        };

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var fileName = Path.GetFileName(file);

            if (KnownDomainExposure.Contains(fileName))
                continue;

            foreach (var pattern in domainEntityPatterns)
            {
                if (content.Contains($"using {pattern}"))
                {
                    violations.Add($"{Path.GetRelativePath(GetApiPath(), file)}: {pattern}");
                }
            }
        }

        violations.Should().BeEmpty(
            $"Endpoint files must not directly reference Domain entity namespaces. " +
            $"Use Application layer DTOs/abstractions. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void EndpointMethods_ShouldReturn_IResult()
    {
        var files = GetEndpointFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var lines = content.Split('\n');

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Contains("static async Task<") &&
                    !trimmed.Contains("IResult") &&
                    !trimmed.Contains("Task<Unit>") &&
                    trimmed.Contains("(") &&
                    !trimmed.Contains("class ") &&
                    !trimmed.Contains("//"))
                {
                    violations.Add($"{Path.GetFileName(file)}: {trimmed}");
                }
            }
        }

        violations.Should().BeEmpty(
            $"Endpoint handler methods should return IResult. Violations: {string.Join(", ", violations)}");
    }
}
