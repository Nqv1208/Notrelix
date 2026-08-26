using FluentValidation;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Common.Requests.Security;

namespace Notrelix.Architecture.Tests.Pipeline;

public class PipelineRuntimeOrderTests
{
    private record TestRequest : IRequest<TestResponse>, IAnonymousRequest, IGlobalRequest;
    private record TestResponse;

    [Fact]
    public void Pipeline_ShouldResolveBehaviorsInRegistrationOrder()
    {
        ParseBehaviorRegistrationOrder().Should().Equal(
            "ExceptionMappingBehavior",
            "ApplicationTracingBehavior",
            "RequestContractBehavior",
            "ExecutionContextBehavior",
            "DataSessionBehavior",
            "AccessControlBehavior",
            "IdempotencyBehavior");
    }

    private static List<string> ParseBehaviorRegistrationOrder()
    {
        var diFile = Path.Combine(GetApplicationPath(), "DependencyInjection.cs");
        var content = RemoveComments(File.ReadAllText(diFile));

        return content.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Contains("AddTransient(typeof(IPipelineBehavior<"))
            .Select(ExtractBehaviorName)
            .ToList();
    }

    private static string ExtractBehaviorName(string line)
    {
        var match = Regex.Match(line, @"typeof\(\w+<,>\),\s*typeof\((\w+)<,>\)");
        return match.Success
            ? match.Groups[1].Value
            : throw new InvalidOperationException($"Could not extract behavior name from: {line}");
    }

    private static Type FindBehaviorType(string name)
    {
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType($"Notrelix.Application.Common.Behaviors.{name}`2"))
            .FirstOrDefault(t => t is not null);

        return type ?? throw new InvalidOperationException($"Behavior type not found: {name}");
    }

    private static string GetApplicationPath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }
        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");
        return Path.Combine(current, "src", "Notrelix.Application");
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
