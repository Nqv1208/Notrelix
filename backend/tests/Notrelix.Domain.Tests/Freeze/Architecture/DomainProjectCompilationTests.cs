using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Notrelix.Domain.Tests.Freeze.Architecture;

/// <summary>
/// Validates the fail-closed behavior of <see cref="RepositoryRootLocator"/>
/// and <see cref="DomainProjectCompilation"/> helpers.
/// </summary>
public class DomainProjectCompilationTests
{
    [Fact]
    public void FindBackendRoot_FromBackendChild_ReturnsBackend()
    {
        using var temp = TempLayout.CreateFlat();
        var child = Path.Combine(temp.Root, "tests", "Some.Tests");

        var result = RepositoryRootLocator.FindBackendRoot(child);

        result.Should().Be(temp.Root);
    }

    [Fact]
    public void FindBackendRoot_FromRepositoryRoot_ReturnsNestedBackend()
    {
        using var temp = TempLayout.CreateNested();
        var repositoryRoot = temp.Root;
        var nested = Path.Combine(repositoryRoot, "backend");

        var result = RepositoryRootLocator.FindBackendRoot(repositoryRoot);

        result.Should().Be(nested);
    }

    [Fact]
    public void FindBackendRoot_WhenMissing_ThrowsWithInspectedPaths()
    {
        using var temp = TempLayout.CreateEmpty();

        var act = () => RepositoryRootLocator.FindBackendRoot(temp.Root);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage($"*Inspected*")
            .WithMessage($"*{temp.Root}*");
    }

    [Fact]
    public void FindDomainProject_ReturnsExistingCsproj()
    {
        using var temp = TempLayout.CreateNested();

        var projectPath = RepositoryRootLocator.FindDomainProject(temp.Root);

        File.Exists(projectPath).Should().BeTrue();
        projectPath.Should().EndWith(
            Path.Combine("src", "Notrelix.Domain", "Notrelix.Domain.csproj"));
    }

    [Fact]
    public void EnsureProjectHasDocuments_WhenEmpty_Throws()
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("Empty", LanguageNames.CSharp);

        var act = () => DomainProjectCompilation.EnsureProjectHasDocuments(project);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*zero regular source documents*");
    }

    [Fact]
    public void EnsureNoWorkspaceFailures_WhenFailure_Throws()
    {
        var failure = new WorkspaceDiagnostic(
            WorkspaceDiagnosticKind.Failure,
            "Project failed to load");

        var act = () =>
            DomainProjectCompilation.EnsureNoWorkspaceFailures(new[] { failure });

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*1 failure diagnostic*")
            .WithMessage("*Project failed to load*");
    }

    [Fact]
    public void EnsureNoWorkspaceFailures_WhenWarningOnly_DoesNotThrow()
    {
        var warning = new WorkspaceDiagnostic(
            WorkspaceDiagnosticKind.Warning,
            "Unrelated warning");

        var act = () =>
            DomainProjectCompilation.EnsureNoWorkspaceFailures(new[] { warning });

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCompilationHasNoErrors_WhenError_Throws()
    {
        var tree = CSharpSyntaxTree.ParseText(
            "class InvalidSyntax { void M() { var x = ; } }");

        var compilation = CSharpCompilation.Create(
            "Invalid.Compilation",
            new[] { tree },
            references: null);

        var act = () =>
            DomainProjectCompilation.EnsureCompilationHasNoErrors(compilation);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*error(s)*");
    }

    [Fact]
    public async Task LoadRealDomainProject_ReturnsExpectedProjectAndCompilation()
    {
        using var fixture = new DomainProjectCompilation();

        await fixture.InitializeAsync();

        try
        {
            fixture.Project.Should().NotBeNull();
            fixture.Project.FilePath.Should().NotBeNullOrWhiteSpace();
            fixture.Project.FilePath.Should().EndWith(
                Path.Combine("src", "Notrelix.Domain", "Notrelix.Domain.csproj"));
            fixture.Project.Language.Should().Be(LanguageNames.CSharp);

            fixture.Compilation.Should().NotBeNull();
            fixture.Compilation.AssemblyName.Should().Be("Notrelix.Domain");

            fixture.WorkspaceDiagnostics
                .Where(d => d.Kind == WorkspaceDiagnosticKind.Failure)
                .Should().BeEmpty();

            DomainProjectCompilation.GetRegularDocuments(fixture.Project)
                .Should().NotBeEmpty();
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    private sealed class TempLayout : IDisposable
    {
        private TempLayout(string root)
        {
            Root = root;
        }

        public string Root { get; }

        public static TempLayout CreateFlat()
        {
            var root = NewRoot();
            Directory.CreateDirectory(Path.Combine(root, "src", "Notrelix.Domain"));
            File.WriteAllText(Path.Combine(root, "backend.slnx"), "<Solution/>");
            File.WriteAllText(
                Path.Combine(root, "src", "Notrelix.Domain", "Notrelix.Domain.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"/>");
            return new TempLayout(root);
        }

        public static TempLayout CreateNested()
        {
            var root = NewRoot();
            Directory.CreateDirectory(
                Path.Combine(root, "backend", "src", "Notrelix.Domain"));
            File.WriteAllText(Path.Combine(root, "backend", "backend.slnx"), "<Solution/>");
            File.WriteAllText(
                Path.Combine(root, "backend", "src", "Notrelix.Domain", "Notrelix.Domain.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"/>");
            return new TempLayout(root);
        }

        public static TempLayout CreateEmpty()
        {
            var root = NewRoot();
            Directory.CreateDirectory(root);
            return new TempLayout(root);
        }

        private static string NewRoot()
        {
            return Path.Combine(
                Path.GetTempPath(),
                "nrx-domain-freeze-" + Guid.NewGuid().ToString("N"));
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup only.
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup only.
            }
        }
    }
}
