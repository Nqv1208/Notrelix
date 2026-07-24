using FluentAssertions;

namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Ensures experimental WorkManagement features (Formulas, Rollups, Workload, Approvals)
/// are isolated and not referenced from stable Domain code outside their own directories.
/// </summary>
public class ExperimentalIsolationTests
{
    private static readonly string[] ExperimentalNamespaces =
    {
        "using Notrelix.Domain.WorkManagement.Formulas",
        "using Notrelix.Domain.WorkManagement.Rollups",
        "using Notrelix.Domain.WorkManagement.Workload",
        "using Notrelix.Domain.WorkManagement.Approvals"
    };

    private static readonly string[] ExperimentalDirectorySegments =
    {
        Path.DirectorySeparatorChar + "Formulas" + Path.DirectorySeparatorChar,
        Path.DirectorySeparatorChar + "Rollups" + Path.DirectorySeparatorChar,
        Path.DirectorySeparatorChar + "Workload" + Path.DirectorySeparatorChar,
        Path.DirectorySeparatorChar + "Approvals" + Path.DirectorySeparatorChar
    };

    private static string GetDomainSourceRoot()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Notrelix.Domain");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate src/Notrelix.Domain by walking up from " +
            AppDomain.CurrentDomain.BaseDirectory);
    }

    [Fact]
    public void StableDomainCode_ShouldNotReference_ExperimentalNamespaces()
    {
        var root = GetDomainSourceRoot();
        var allFiles = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                     && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .ToList();

        // Exclude files that live inside experimental directories
        var stableFiles = allFiles
            .Where(f => !ExperimentalDirectorySegments.Any(seg => f.Contains(seg)))
            .ToList();

        var violations = new List<(string File, string Namespace)>();

        foreach (var file in stableFiles)
        {
            var content = File.ReadAllText(file);
            foreach (var ns in ExperimentalNamespaces)
            {
                if (content.Contains(ns))
                    violations.Add((file, ns));
            }
        }

        violations.Should().BeEmpty(
            "stable Domain code must not reference experimental WorkManagement namespaces; violations: " +
            string.Join("; ", violations.Select(v => $"{Path.GetFileName(v.File)} -> {v.Namespace}")));
    }

    [Fact]
    public void ExperimentalDirectories_ShouldExist()
    {
        var root = GetDomainSourceRoot();
        var workManagementRoot = Path.Combine(root, "WorkManagement");

        // At least the Approvals directory should exist as it has active code
        Directory.Exists(Path.Combine(workManagementRoot, "Approvals"))
            .Should().BeTrue("the Approvals experimental directory should exist");
    }
}
