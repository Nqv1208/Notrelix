namespace Notrelix.Domain.Tests.Freeze.Architecture;

/// <summary>
/// Locates the backend repository root and the Domain project without
/// fixed-depth path assumptions.
///
/// Fail-closed: never returns null; throws <see cref="InvalidOperationException"/>
/// with the inspected directories when the backend root cannot be found.
/// </summary>
internal static class RepositoryRootLocator
{
    private const string SolutionFileName = "backend.slnx";
    private const string DomainProjectRelativePath =
        "src/Notrelix.Domain/Notrelix.Domain.csproj";

    internal static string FindBackendRoot(
        string? startDirectory = null)
    {
        var start = Path.GetFullPath(
            startDirectory ?? AppContext.BaseDirectory);

        var inspected = new List<string>();

        for (var current = new DirectoryInfo(start);
             current is not null;
             current = current.Parent)
        {
            inspected.Add(current.FullName);

            if (IsBackendRoot(current.FullName))
                return current.FullName;

            var nested = Path.Combine(current.FullName, "backend");

            if (IsBackendRoot(nested))
                return nested;
        }

        throw new InvalidOperationException(
            $"Unable to locate backend root from '{start}'. " +
            $"Inspected: {string.Join(", ", inspected)}. " +
            $"Expected solution file: {SolutionFileName}. " +
            $"Expected Domain project relative path: {DomainProjectRelativePath}.");
    }

    internal static string FindDomainProject(
        string? startDirectory = null)
    {
        var backendRoot = FindBackendRoot(startDirectory);

        return Path.Combine(
            backendRoot,
            "src",
            "Notrelix.Domain",
            "Notrelix.Domain.csproj");
    }

    private static bool IsBackendRoot(string path)
    {
        return File.Exists(Path.Combine(path, SolutionFileName))
            && File.Exists(Path.Combine(
                path,
                "src",
                "Notrelix.Domain",
                "Notrelix.Domain.csproj"));
    }
}
