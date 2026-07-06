namespace Notrelix.Architecture.Tests;

public abstract class ArchitectureTestBase
{
    protected static string GetSrcPath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }
        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");
        return Path.Combine(current, "src");
    }

    protected static string GetApplicationPath() => Path.Combine(GetSrcPath(), "Notrelix.Application");
    protected static string GetApiPath() => Path.Combine(GetSrcPath(), "Notrelix.API");
    protected static string GetDomainPath() => Path.Combine(GetSrcPath(), "Notrelix.Domain");
    protected static string GetInfrastructurePath() => Path.Combine(GetSrcPath(), "Notrelix.Infrastructure");

    protected static string[] GetApplicationFeatureFiles()
    {
        var appPath = GetApplicationPath();
        return Directory.GetFiles(Path.Combine(appPath, "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
    }

    protected static string[] GetApiEndpointFiles()
    {
        return Directory.GetFiles(GetApiPath(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
    }

    protected static string RemoveComments(string input)
    {
        var blockComments = @"/\*(.*?)\*/";
        var lineComments = @"//(.*?)\r?\n";
        var cleaned = Regex.Replace(input, blockComments, "", RegexOptions.Singleline);
        cleaned = Regex.Replace(cleaned, lineComments, "\n");
        return cleaned;
    }
}
