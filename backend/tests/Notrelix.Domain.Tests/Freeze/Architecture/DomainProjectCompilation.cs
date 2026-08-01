using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Notrelix.Domain.Tests.Freeze.Architecture;

/// <summary>
/// Loads the actual <c>backend/src/Notrelix.Domain/Notrelix.Domain.csproj</c>
/// through <see cref="MSBuildWorkspace"/> once per test class.
///
/// Fail-closed: every failure (missing project, workspace failure, null
/// compilation, compilation errors, zero documents) throws instead of
/// returning a permissive result.
/// </summary>
public sealed class DomainProjectCompilation :
    IAsyncLifetime,
    IDisposable
{
    private static readonly object RegistrationLock = new();

    private MSBuildWorkspace? _workspace;

    public Project Project { get; private set; } = null!;

    public Compilation Compilation { get; private set; } = null!;

    public IReadOnlyList<WorkspaceDiagnostic> WorkspaceDiagnostics { get; private set; }
        = Array.Empty<WorkspaceDiagnostic>();

    public async Task InitializeAsync()
    {
        EnsureMSBuildRegistered();

        var properties = new Dictionary<string, string>
        {
            ["Configuration"] = "Release"
        };

        var diagnostics = new List<WorkspaceDiagnostic>();

        var workspace = MSBuildWorkspace.Create(properties);
        workspace.WorkspaceFailed += (_, args) =>
            diagnostics.Add(args.Diagnostic);
        _workspace = workspace;

        var projectPath = RepositoryRootLocator.FindDomainProject();

        var project = await workspace.OpenProjectAsync(projectPath);

        if (project is null)
        {
            throw new InvalidOperationException(
                $"MSBuildWorkspace returned a null project for: {projectPath}. " +
                "Determinism gate cannot verify Domain source.");
        }

        var expectedPath = Path.GetFullPath(projectPath);
        var actualPath = Path.GetFullPath(project.FilePath!);
        if (!string.Equals(expectedPath, actualPath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Project file mismatch. Expected: {expectedPath}; Actual: {actualPath}.");
        }

        if (!string.Equals(project.Language, LanguageNames.CSharp, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Domain project must be C#. Actual language: {project.Language}.");
        }

        EnsureProjectHasDocuments(project);
        EnsureNoWorkspaceFailures(diagnostics);

        var compilation = await project.GetCompilationAsync();

        if (compilation is null)
        {
            throw new InvalidOperationException(
                "GetCompilationAsync returned a null compilation for the Domain project. " +
                "Determinism gate cannot verify Domain source.");
        }

        EnsureCompilationHasNoErrors(compilation);

        if (!string.Equals(compilation.AssemblyName, "Notrelix.Domain", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Domain compilation assembly name must be Notrelix.Domain. " +
                $"Actual: {compilation.AssemblyName}.");
        }

        Project = project;
        WorkspaceDiagnostics = diagnostics;
        Compilation = compilation;
    }

    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _workspace?.Dispose();
        _workspace = null;
    }

    internal static IReadOnlyList<Document> GetRegularDocuments(Project project)
    {
        return project.Documents
            .Where(d => d.FilePath is not null)
            .Where(d => !IsGeneratedPath(d.FilePath!))
            .OrderBy(d => d.FilePath, StringComparer.Ordinal)
            .ToList();
    }

    internal static void EnsureProjectHasDocuments(Project project)
    {
        var documents = GetRegularDocuments(project);

        if (documents.Count == 0)
        {
            throw new InvalidOperationException(
                $"Domain project '{project.FilePath}' contains zero regular source documents. " +
                "Determinism gate cannot verify Domain source.");
        }
    }

    internal static void EnsureNoWorkspaceFailures(
        IEnumerable<WorkspaceDiagnostic> diagnostics)
    {
        var failures = diagnostics
            .Where(d => d.Kind == WorkspaceDiagnosticKind.Failure)
            .ToList();

        if (failures.Count > 0)
        {
            var summary = string.Join("\n", failures.Select(f => f.Message));
            throw new InvalidOperationException(
                $"MSBuildWorkspace reported {failures.Count} failure diagnostic(s):\n{summary}");
        }
    }

    internal static void EnsureCompilationHasNoErrors(
        Compilation compilation)
    {
        var errors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        if (errors.Length > 0)
        {
            var summary = string.Join("\n", errors.Take(20).Select(e => e.ToString()));
            throw new InvalidOperationException(
                $"Domain compilation has {errors.Length} error(s):\n{summary}");
        }
    }

    private static void EnsureMSBuildRegistered()
    {
        lock (RegistrationLock)
        {
            if (!MSBuildLocator.IsRegistered)
                MSBuildLocator.RegisterDefaults();
        }
    }

    private static bool IsGeneratedPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/obj/", StringComparison.Ordinal)
            || normalized.Contains("/bin/", StringComparison.Ordinal);
    }
}
