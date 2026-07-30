using System.Collections.Immutable;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Freeze.Architecture;

public class InfrastructureReflectionGateTests
{
    private static readonly string InfrastructureProjectDir = Path.GetFullPath(
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "..", "src", "Notrelix.Infrastructure"));

    private static readonly string[] AuditReflectionPatterns =
    [
        "SetAuditOnUpdate",
        "PrepareAuditUpdate",
        "ApplyAuditUpdate",
        "PrepareDeletion",
        "ApplyDeletion",
        "PrepareRestore",
        "ApplyRestore",
    ];

    private static readonly string[] ReflectionKeywords =
    [
        "GetMethod",
        "Invoke",
    ];

    private static readonly Lazy<IReadOnlyList<string>> InfrastructureFiles = new(LoadInfrastructureFiles);

    private static IReadOnlyList<string> LoadInfrastructureFiles()
    {
        if (!Directory.Exists(InfrastructureProjectDir))
            return Array.Empty<string>();

        return Directory.GetFiles(InfrastructureProjectDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/bin/") && !f.Contains("/obj/") && !f.Contains("GlobalUsings"))
            .OrderBy(f => f)
            .ToImmutableList();
    }

    [Fact]
    public void Infrastructure_ShouldNotReference_DomainAuditMethodNames()
    {
        var files = InfrastructureFiles.Value;
        if (files.Count == 0)
            return;

        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            foreach (var pattern in AuditReflectionPatterns)
            {
                var idx = content.IndexOf(pattern, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    var line = content[..idx].Count(c => c == '\n') + 1;
                    violations.Add($"{Path.GetRelativePath(InfrastructureProjectDir, file)}:{line} references '{pattern}'");
                }
            }
        }

        violations.Should().BeEmpty(
            "Infrastructure must not reference private Domain audit/soft-delete method names directly. Found:\n" +
            string.Join("\n", violations));
    }

    [Fact]
    public void Infrastructure_ShouldNotReflect_PrivateDomainMutationMethods()
    {
        var files = InfrastructureFiles.Value;
        if (files.Count == 0)
            return;

        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);

            foreach (var pattern in ReflectionKeywords)
            {
                var idx = content.IndexOf(pattern, StringComparison.Ordinal);
                if (idx < 0) continue;

                var line = content[..idx].Count(c => c == '\n') + 1;

                var start = Math.Max(0, idx - 100);
                var end = Math.Min(content.Length, idx + 100);
                var context = content[start..end];

                if (context.Contains("AuditableEntity", StringComparison.Ordinal) ||
                    context.Contains("SetAuditOn", StringComparison.Ordinal) ||
                    context.Contains("PrepareAudit", StringComparison.Ordinal) ||
                    context.Contains("PrepareDeletion", StringComparison.Ordinal) ||
                    context.Contains("ApplyDeletion", StringComparison.Ordinal) ||
                    context.Contains("PrepareRestore", StringComparison.Ordinal) ||
                    context.Contains("ApplyRestore", StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetRelativePath(InfrastructureProjectDir, file)}:{line} uses reflection on audit lifecycle methods");
                }
            }
        }

        violations.Should().BeEmpty(
            "Infrastructure must not reflect private Domain audit/soft-delete mutation methods. Found:\n" +
            string.Join("\n", violations));
    }

    [Fact]
    public void InfrastructureProjectDirectory_ShouldExist()
    {
        Directory.Exists(InfrastructureProjectDir)
            .Should().BeTrue($"Infrastructure project directory should exist at {InfrastructureProjectDir}");
    }

    [Fact]
    public void InfrastructureProjectFiles_ShouldNotBeEmpty()
    {
        var files = InfrastructureFiles.Value;
        files.Should().NotBeEmpty("Infrastructure project should contain .cs files");
    }
}
