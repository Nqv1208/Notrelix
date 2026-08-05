using System.Xml.Linq;

namespace Notrelix.Architecture.Tests.InfrastructureLayer;

/// <summary>
/// PLT-001..007: Platform Messaging boundary enforcement.
/// Platform is a Messaging Runtime — all production namespaces under Notrelix.Platform.Messaging.*,
/// no API/Infrastructure reference, no aggregate/entity/value-object definitions,
/// consumers dispatch Application boundary.
/// </summary>
public class PlatformBoundaryTests
{
    [Fact]
    public void PLT_001_Platform_And_PlatformTests_In_Solution()
    {
        var backendRoot = FindBackendRoot();
        var slnxPath = Path.Combine(backendRoot, "backend.slnx");
        var content = File.ReadAllText(slnxPath);

        content.Should().Contain("Notrelix.Platform.csproj",
            "Platform must be in the solution build graph");
        content.Should().Contain("Notrelix.Platform.Tests.csproj",
            "Platform.Tests must be in the solution test graph");
    }

    [Fact]
    public void PLT_002_Platform_References_Only_Application_And_Domain()
    {
        var backendRoot = FindBackendRoot();
        var csprojPath = Path.Combine(backendRoot, "src", "Notrelix.Platform", "Notrelix.Platform.csproj");
        var doc = XDocument.Load(csprojPath);

        var projectRefs = doc.Descendants("ProjectReference")
            .Select(e => e.Attribute("Include")?.Value ?? "")
            .Select(p => Path.GetFileNameWithoutExtension(p.Replace('\\', Path.AltDirectorySeparatorChar)))
            .ToList();

        projectRefs.Should().OnlyContain(name =>
                name == "Notrelix.Application" || name == "Notrelix.Domain",
            "Platform must only reference Application and Domain — no API or Infrastructure");
    }

    [Fact]
    public void PLT_003_Every_Production_Namespace_Starts_With_Platform_Messaging()
    {
        var platformPath = GetPlatformPath();
        var csFiles = Directory.GetFiles(platformPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.Combine("bin", "")) && !f.Contains(Path.Combine("obj", "")));

        var violations = new List<string>();

        foreach (var file in csFiles)
        {
            var lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("namespace ", StringComparison.Ordinal))
                    continue;

                var ns = trimmed["namespace ".Length..].TrimEnd(';').Trim();
                if (!ns.StartsWith("Notrelix.Platform.Messaging", StringComparison.Ordinal))
                {
                    var relativePath = Path.GetRelativePath(platformPath, file);
                    violations.Add($"{relativePath}: namespace '{ns}'");
                }
            }
        }

        violations.Should().BeEmpty(
            "all Platform production namespaces must be under Notrelix.Platform.Messaging.*");
    }

    [Fact]
    public void PLT_005_No_Aggregate_Entity_ValueObject_Definitions()
    {
        var platformPath = GetPlatformPath();
        var csFiles = Directory.GetFiles(platformPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.Combine("bin", "")) && !f.Contains(Path.Combine("obj", "")));

        var violations = new List<string>();
        var domainBaseTypes = new[] { "AggregateRoot", "Entity", "ValueObject" };

        foreach (var file in csFiles)
        {
            var content = File.ReadAllText(file);
            foreach (var baseType in domainBaseTypes)
            {
                if (content.Contains($": {baseType}") || content.Contains($": {baseType},"))
                {
                    var relativePath = Path.GetRelativePath(platformPath, file);
                    violations.Add($"{relativePath}: inherits {baseType}");
                }
            }
        }

        violations.Should().BeEmpty(
            "Platform must not define Domain aggregates, entities, or value objects");
    }

    [Fact]
    public void PLT_007_No_Generic_Business_Repository()
    {
        var platformPath = GetPlatformPath();
        var csFiles = Directory.GetFiles(platformPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.Combine("bin", "")) && !f.Contains(Path.Combine("obj", "")));

        var violations = new List<string>();

        foreach (var file in csFiles)
        {
            var fileName = Path.GetFileName(file);
            if (fileName.Contains("Repository", StringComparison.Ordinal))
            {
                var relativePath = Path.GetRelativePath(platformPath, file);
                violations.Add(relativePath);
            }
        }

        violations.Should().BeEmpty("Platform must not define generic business repositories");
    }

    private static string GetPlatformPath()
    {
        var backendRoot = FindBackendRoot();
        return Path.Combine(backendRoot, "src", "Notrelix.Platform");
    }

    private static string FindBackendRoot()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }

        return current ?? throw new DirectoryNotFoundException("Could not find backend.slnx root.");
    }
}
