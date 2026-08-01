using FluentAssertions;

namespace Notrelix.Architecture.Tests.EndpointContracts;

/// <summary>
/// API source must not reference provider-specific namespaces.
/// Provider exception translation belongs in Infrastructure.
/// </summary>
public sealed class ApiProviderIndependenceTests
{
    [Fact]
    public void Api_source_must_not_reference_Npgsql()
    {
        var apiPath = FindApiSourcePath();
        var files = Directory.GetFiles(apiPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            if (content.Contains("using Npgsql") || content.Contains("PostgresException"))
            {
                violations.Add(Path.GetRelativePath(apiPath, file));
            }
        }

        violations.Should().BeEmpty(
            "API must not reference Npgsql types. Provider exception translation belongs in Infrastructure. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Api_source_must_not_use_DbUpdateException_for_error_mapping()
    {
        var apiPath = FindApiSourcePath();
        var errorHandlingPath = Path.Combine(apiPath, "ErrorHandling");

        if (!Directory.Exists(errorHandlingPath)) return;

        var files = Directory.GetFiles(errorHandlingPath, "*.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            if (content.Contains("DbUpdateException") || content.Contains("DbUpdateConcurrencyException"))
            {
                violations.Add(Path.GetFileName(file));
            }
        }

        violations.Should().BeEmpty(
            "API error handling must not reference EF exception types. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    private static string FindApiSourcePath()
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
}
