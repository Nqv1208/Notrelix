using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Notrelix.Architecture.Tests.PlatformMessaging;

/// <summary>
/// FZ-IDEM-06 (spec 3.4): idempotent Application commands are dispatched through
/// the typed AddApplicationConsumer registration (fresh scope, key from
/// EventEnvelope.Id, source Message, ISender dispatch). Raw AddConsumer may only
/// host Messaging-runtime handlers and must never dispatch an idempotent request
/// or bind an execution key manually.
/// </summary>
public class IdempotencyConsumerContractTests : ArchitectureTestBase
{
    private static readonly Assembly ApplicationAssembly =
        typeof(Notrelix.Application.Common.Behaviors.ValidationBehavior<,>).Assembly;

    private static HashSet<string> GetIdempotentTypeNames()
    {
        return ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => typeof(IIdempotentRequest).IsAssignableFrom(t))
            .Select(t => t.Name)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static List<string> ScanSource(string source, IReadOnlySet<string> idempotentTypeNames)
    {
        var violations = new List<string>();
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var name = invocation.Expression switch
            {
                MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                _ => null,
            };

            // Raw registration only — AddApplicationConsumer/AddScopedConsumer are
            // the typed/scoped paths.
            if (name != "AddConsumer")
            {
                continue;
            }

            foreach (var creation in invocation.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                var typeName = creation.Type switch
                {
                    IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                    QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                    GenericNameSyntax generic => generic.Identifier.ValueText,
                    _ => null,
                };

                if (typeName is not null && idempotentTypeNames.Contains(typeName))
                {
                    violations.Add(
                        $"raw AddConsumer dispatches idempotent command '{typeName}' — use AddApplicationConsumer");
                }
            }

            if (invocation.DescendantNodes().OfType<IdentifierNameSyntax>()
                .Any(i => i.Identifier.ValueText == "IdempotencyExecutionSource"))
            {
                violations.Add(
                    "raw AddConsumer handler binds an idempotency execution key manually — " +
                    "keys are bound by AddApplicationConsumer from EventEnvelope.Id");
            }
        }

        return violations;
    }

    [Fact]
    public void Production_Sources_Never_Dispatch_Idempotent_Commands_Through_Raw_AddConsumer()
    {
        var idempotentNames = GetIdempotentTypeNames();
        var apiFiles = GetApiEndpointFiles();
        var platformAndInfraFiles = Directory
            .EnumerateFiles(GetSrcPath(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

        var violations = apiFiles.Concat(platformAndInfraFiles)
            .Distinct(StringComparer.Ordinal)
            .SelectMany(file => ScanSource(File.ReadAllText(file), idempotentNames)
                .Select(v => $"{Path.GetRelativePath(GetSrcPath(), file)}: {v}"))
            .ToList();

        violations.Should().BeEmpty(
            "idempotent Application commands must be dispatched through AddApplicationConsumer, " +
            "never through raw AddConsumer with manual key binding");
    }

    [Fact]
    public void Scanner_Detects_Raw_AddConsumer_Dispatching_An_Idempotent_Command()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;

            namespace Fixture;

            public static class BadConsumerRegistration
            {
                public static void Register(IServiceCollection services)
                {
                    services.AddConsumer("fixture.event", async (envelope, ct) =>
                    {
                        writer.Set(envelope.Id.ToString(), IdempotencyExecutionSource.Message);
                        await sender.Send(new FixtureCommand());
                    });
                }
            }
            """;

        var violations = ScanSource(source, new HashSet<string> { "FixtureCommand" });

        violations.Should().HaveCount(2,
            "both the idempotent dispatch and the manual key binding must be flagged");
    }

    [Fact]
    public void Scanner_Ignores_Raw_AddConsumer_Without_Idempotent_Dispatch()
    {
        var source = """
            using Microsoft.Extensions.DependencyInjection;

            namespace Fixture;

            public static class RuntimeConsumerRegistration
            {
                public static void Register(IServiceCollection services)
                {
                    services.AddConsumer("runtime.event", (envelope, ct) =>
                    {
                        return Task.CompletedTask;
                    });
                }
            }
            """;

        var violations = ScanSource(source, new HashSet<string> { "FixtureCommand" });

        violations.Should().BeEmpty(
            "plain Messaging-runtime handlers remain allowed on raw AddConsumer");
    }
}
