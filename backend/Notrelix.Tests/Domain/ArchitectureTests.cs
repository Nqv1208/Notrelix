using FluentAssertions;
using System.IO;
using System.Linq;
using Xunit;

namespace Notrelix.Domain.Tests;

public class ArchitectureTests
{
    private static string GetDomainPath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !Directory.Exists(Path.Combine(current, "Notrelix.Domain")))
        {
            current = Path.GetDirectoryName(current);
        }
        if (current == null)
        {
            // Try fallback using parent steps
            var fallback = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Notrelix.Domain"));
            if (Directory.Exists(fallback)) return fallback;

            throw new DirectoryNotFoundException("Could not find Notrelix.Domain root folder.");
        }
        return Path.Combine(current, "Notrelix.Domain");
    }

    [Fact]
    public void DomainFiles_ShouldNotContain_RawUtcNow()
    {
        var domainPath = GetDomainPath();
        var files = Directory.GetFiles(domainPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            // Skip obj/bin folders
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || 
                file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            var cleanedContent = RemoveComments(content);

            cleanedContent.Should().NotContain("DateTime.UtcNow", $"File '{Path.GetFileName(file)}' violates the rule: Do not use DateTime.UtcNow in Domain.");
            cleanedContent.Should().NotContain("DateTimeOffset.UtcNow", $"File '{Path.GetFileName(file)}' violates the rule: Do not use DateTimeOffset.UtcNow in Domain.");
        }
    }

    [Fact]
    public void DomainFiles_ShouldNotReference_EntityFrameworkCore()
    {
        var domainPath = GetDomainPath();
        var files = Directory.GetFiles(domainPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || 
                file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            var cleanedContent = RemoveComments(content);

            cleanedContent.Should().NotContain("using Microsoft.EntityFrameworkCore", $"File '{Path.GetFileName(file)}' should not reference EF Core directly.");
        }
    }

    private static string RemoveComments(string input)
    {
        var blockComments = @"/\*(.*?)\*/";
        var lineComments = @"//(.*?)\r?\n";
        var cleaned = System.Text.RegularExpressions.Regex.Replace(input, blockComments, "", System.Text.RegularExpressions.RegexOptions.Singleline);
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, lineComments, "\n");
        return cleaned;
    }
}
