namespace Notrelix.Architecture.Tests;

/// <summary>
/// The owner database context (migrate, rls-apply, seed) must not become a second way for serving
/// code to reach the database: only its provider and the composition root may name it.
/// </summary>
public class OwnerConnectionBoundaryTests
{
    private static readonly string[] AllowedFiles =
    [
        Path.Combine("Notrelix.Infrastructure", "Data", "OwnerDbContextProvider.cs"),
        Path.Combine("Notrelix.Infrastructure", "DependencyInjection", "PersistenceRegistration.cs"),
    ];

    private static string SourceRoot()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }

        return Path.Combine(
            current ?? throw new DirectoryNotFoundException("Could not find backend.slnx root."),
            "src");
    }

    [Fact]
    public void OwnerDbContextProvider_IsReferencedOnlyByItselfAndTheCompositionRoot()
    {
        var root = SourceRoot();
        var violations = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(f => Path.GetRelativePath(root, f))
            .Where(relative => !AllowedFiles.Contains(relative))
            .Where(relative => File.ReadAllText(Path.Combine(root, relative))
                .Contains("OwnerDbContextProvider", StringComparison.Ordinal))
            .ToList();

        violations.Should().BeEmpty(
            "the owner connection is for migrate, rls-apply and seed; the initialiser and policy applier receive its context through composition, not by resolving it.");
    }

    [Fact]
    public void OwnerDbContextProvider_ProviderTypeExistsAtTheAllowedPath()
    {
        File.Exists(Path.Combine(SourceRoot(), AllowedFiles[0])).Should().BeTrue(
            "the boundary test names the provider by path; moving it requires updating this gate");
    }
}
