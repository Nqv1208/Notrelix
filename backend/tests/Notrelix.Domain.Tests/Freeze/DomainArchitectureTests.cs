using System.Text.RegularExpressions;
using FluentAssertions;

namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Architecture freeze gate: ensures the Domain layer does not leak
/// infrastructure or framework concerns into its source files.
/// </summary>
public class DomainArchitectureTests
{
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

    private static List<string> GetDomainSourceFiles()
    {
        var root = GetDomainSourceRoot();
        return Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                     && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .ToList();
    }

    [Fact]
    public void DomainSource_ShouldNotContain_DateTimeUtcNow()
    {
        var files = GetDomainSourceFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            if (content.Contains("DateTime.UtcNow") || content.Contains("DateTimeOffset.UtcNow"))
                violations.Add(file);
        }

        violations.Should().BeEmpty(
            "Domain must not use DateTime.UtcNow or DateTimeOffset.UtcNow; timestamps are supplied by Application");
    }

    [Fact]
    public void DomainSource_ShouldNotReference_EntityFrameworkCore()
    {
        var files = GetDomainSourceFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            if (content.Contains("Microsoft.EntityFrameworkCore"))
                violations.Add(file);
        }

        violations.Should().BeEmpty(
            "Domain must not reference Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void DomainSource_ShouldNotReference_AspNetCore()
    {
        var files = GetDomainSourceFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            if (content.Contains("Microsoft.AspNetCore"))
                violations.Add(file);
        }

        violations.Should().BeEmpty(
            "Domain must not reference Microsoft.AspNetCore");
    }

    [Fact]
    public void DomainSource_ShouldNotReference_MediatR()
    {
        var files = GetDomainSourceFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            if (content.Contains("MediatR"))
                violations.Add(file);
        }

        violations.Should().BeEmpty(
            "Domain must not reference MediatR");
    }

    [Fact]
    public void DomainSource_ShouldNotHave_PublicMutableCollections()
    {
        var files = GetDomainSourceFiles();
        var violations = new List<string>();

        // Matches public properties/fields of mutable collection types
        var pattern = new Regex(
            @"public\s+(List|ICollection|IList|HashSet|Dictionary|ISet)\s*<",
            RegexOptions.Compiled);

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            if (pattern.IsMatch(content))
                violations.Add(file);
        }

        violations.Should().BeEmpty(
            "Domain must not expose public mutable collections; use IReadOnlyCollection<T> or IReadOnlyList<T>");
    }

    [Fact]
    public void DomainSource_ShouldHaveFiles_ToScan()
    {
        var files = GetDomainSourceFiles();

        files.Should().HaveCountGreaterThan(50,
            "sanity check: the Domain project should have a substantial number of source files");
    }
}
