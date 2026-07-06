namespace Notrelix.Architecture.Tests;

public class DomainArchitectureTests
{
    private static string GetDomainPath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }
        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");
        return Path.Combine(current, "src", "Notrelix.Domain");
    }

    private static string[] GetDomainFiles()
    {
        var domainPath = GetDomainPath();
        return Directory.GetFiles(domainPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
    }

    [Fact]
    public void DomainFiles_ShouldNotContain_RawUtcNow()
    {
        var files = GetDomainFiles();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var cleaned = RemoveComments(content);

            cleaned.Should().NotContain("DateTime.UtcNow", $"File '{Path.GetFileName(file)}' must not use DateTime.UtcNow.");
            cleaned.Should().NotContain("DateTimeOffset.UtcNow", $"File '{Path.GetFileName(file)}' must not use DateTimeOffset.UtcNow.");
        }
    }

    [Fact]
    public void DomainFiles_ShouldNotReference_EntityFrameworkCore()
    {
        var files = GetDomainFiles();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));

            content.Should().NotContain("using Microsoft.EntityFrameworkCore", $"File '{Path.GetFileName(file)}' must not reference EF Core.");
        }
    }

    public static readonly TheoryData<string> ForbiddenDomainPatterns = new()
    {
        "using Microsoft.Extensions.",
        "using Microsoft.AspNetCore.",
        "using System.Net.",
        "using MediatR",
        "using Newtonsoft.Json",
    };

    [Fact]
    public void DomainFiles_ShouldNotReference_InfrastructureNamespaces()
    {
        var files = GetDomainFiles();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));

            foreach (var pattern in ForbiddenDomainPatterns.Cast<string>())
            {
                content.Should().NotContain(pattern, $"File '{Path.GetFileName(file)}' must not use '{pattern}'.");
            }
        }
    }

    private static readonly HashSet<string> JsonAllowlist =
    [
        "JsonValue.cs",
        "FieldSettingsValidator.cs",
        "FieldValueValidator.cs",
        "BoardItem.cs",
        "FormQuestionConfig.cs",
        "ApiTokenScopes.cs",
        "UserProfile.cs",
        "AutomationActionValidator.cs",
        "AutomationActionDefinition.cs",
        "AutomationTriggerValidator.cs",
        "AutomationTriggerDefinition.cs",
        "AutomationConditionDefinition.cs",
        "AiAgentInstruction.cs",
        "AiAgentModelPolicy.cs",
        "AiAgentToolPermissions.cs",
        "WidgetConfigValidator.cs",
    ];

    [Fact]
    public void DomainFiles_ShouldNotUse_JsonSerializationAttributes()
    {
        var files = GetDomainFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (JsonAllowlist.Contains(fileName)) continue;

            var content = RemoveComments(File.ReadAllText(file));

            if (content.Contains("[JsonPropertyName") ||
                content.Contains("[JsonIgnore") ||
                content.Contains("[JsonConverter") ||
                content.Contains("[JsonProperty") ||
                content.Contains("[DataMember") ||
                content.Contains("[DataContract") ||
                content.Contains("[IgnoreDataMember"))
            {
                violations.Add(fileName);
            }
        }

        violations.Should().BeEmpty($"Files must not use serialization attributes in Domain: {string.Join(", ", violations)}");
    }

    private static string RemoveComments(string input)
    {
        var blockComments = @"/\*(.*?)\*/";
        var lineComments = @"//(.*?)\r?\n";
        var cleaned = Regex.Replace(input, blockComments, "", RegexOptions.Singleline);
        cleaned = Regex.Replace(cleaned, lineComments, "\n");
        return cleaned;
    }
}
