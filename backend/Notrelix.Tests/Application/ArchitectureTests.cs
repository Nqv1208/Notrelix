using System.Text.RegularExpressions;
using FluentAssertions;

namespace Notrelix.Application.Tests;

public class ArchitectureTests
{
    private static string GetApplicationPath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !Directory.Exists(Path.Combine(current, "Notrelix.Application")))
        {
            current = Path.GetDirectoryName(current);
        }
        if (current == null)
        {
            var fallback = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Notrelix.Application"));
            if (Directory.Exists(fallback)) return fallback;

            throw new DirectoryNotFoundException("Could not find Notrelix.Application root folder.");
        }
        return Path.Combine(current, "Notrelix.Application");
    }

    private static string[] GetApplicationFeatureFiles()
    {
        var appPath = GetApplicationPath();
        return Directory.GetFiles(Path.Combine(appPath, "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
    }

    [Fact]
    public void RequestRecords_ShouldNotUse_RawIRequest()
    {
        var files = GetApplicationFeatureFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));

            var lines = content.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Contains(": IRequest<") || trimmed.Contains(": IRequest,") || trimmed == ": IRequest")
                {
                    violations.Add($"{Path.GetFileName(file)}: {trimmed}");
                }
            }
        }

        violations.Should().BeEmpty($"Request records must use ICommand/IQuery instead of raw IRequest: {string.Join(", ", violations)}");
    }

    [Fact]
    public void RequestRecords_ShouldImplement_ICommandOrIQuery()
    {
        var appPath = GetApplicationPath();
        var requestFiles = Directory.GetFiles(Path.Combine(appPath, "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && (f.Contains($"{Path.DirectorySeparatorChar}Commands{Path.DirectorySeparatorChar}")
                      || f.Contains($"{Path.DirectorySeparatorChar}Queries{Path.DirectorySeparatorChar}")))
            .ToArray();
        var violations = new List<string>();

        foreach (var file in requestFiles)
        {
            var content = RemoveComments(File.ReadAllText(file));

            var lines = content.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("public record") && !trimmed.StartsWith("public sealed record"))
                    continue;

                if (trimmed.Contains(": ICommand") || trimmed.Contains(": IQuery"))
                    continue;

                violations.Add($"{Path.GetFileName(file)}: {trimmed}");
            }
        }

        violations.Should().BeEmpty($"Request records must implement ICommand or IQuery: {string.Join(", ", violations)}");
    }

    [Fact]
    public void ApplicationFiles_ShouldNotReference_InfrastructureProject()
    {
        var appPath = GetApplicationPath();
        var files = Directory.GetFiles(appPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));

            if (content.Contains("using Notrelix.Infrastructure") ||
                content.Contains("using Notrelix.Api"))
            {
                violations.Add(Path.GetFileName(file));
            }
        }

        violations.Should().BeEmpty($"Application must not reference Infrastructure or Api projects: {string.Join(", ", violations)}");
    }

    private static string RemoveComments(string input)
    {
        var blockComments = @"/\*(.*?)\*/";
        var lineComments = @"//(.*?)\r?\n";
        var cleaned = Regex.Replace(input, blockComments, "", RegexOptions.Singleline);
        cleaned = Regex.Replace(cleaned, lineComments, "\n");
        return cleaned;
    }
}