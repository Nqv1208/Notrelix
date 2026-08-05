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

    /// <summary>
    /// Scans MassTransit <c>IConsumer&lt;T&gt;</c> implementations. A consumer that
    /// dispatches an idempotent Application command must bind the idempotency
    /// execution key from the message <c>EventId</c> with source
    /// <see cref="IdempotencyExecutionSource.Message"/> (spec 3.4) — never from
    /// business data, a hard-coded value, or a missing binding.
    /// </summary>
    private static List<string> ScanMassTransitConsumers(string source, IReadOnlySet<string> idempotentTypeNames)
    {
        var violations = new List<string>();
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

        foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var isMassTransitConsumer = classDecl.BaseList?.Types
                .Select(t => GetTypeName(t.Type))
                .Any(name => name == "IConsumer")
                ?? false;

            if (!isMassTransitConsumer)
            {
                continue;
            }

            var dispatchesIdempotent = classDecl.DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>()
                .Any(creation => idempotentTypeNames.Contains(GetTypeName(creation.Type)));

            if (!dispatchesIdempotent)
            {
                continue;
            }

            var setCalls = classDecl.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(i => i.Expression is MemberAccessExpressionSyntax member
                            && member.Name.Identifier.ValueText == "Set")
                .ToList();

            var bindsKeyFromEventId = setCalls.Any(call =>
            {
                var arguments = call.ArgumentList.Arguments;
                if (arguments.Count != 2)
                {
                    return false;
                }

                var keyExpression = arguments[0].Expression;
                var sourceExpression = arguments[1].Expression;

                return keyExpression.DescendantNodesAndSelf()
                           .OfType<IdentifierNameSyntax>()
                           .Any(i => i.Identifier.ValueText == "EventId")
                       && sourceExpression.ToString().Contains(
                           "IdempotencyExecutionSource.Message", StringComparison.Ordinal);
            });

            if (!bindsKeyFromEventId)
            {
                violations.Add(
                    $"MassTransit consumer '{classDecl.Identifier.ValueText}' dispatches an idempotent command " +
                    "without binding the execution key from the message EventId with source Message (spec 3.4)");
            }
        }

        return violations;
    }

    private static string GetTypeName(TypeSyntax type)
    {
        return type switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            GenericNameSyntax generic => generic.Identifier.ValueText,
            QualifiedNameSyntax qualified => GetTypeName(qualified.Right),
            AliasQualifiedNameSyntax alias => GetTypeName(alias.Name),
            _ => type.ToString(),
        };
    }

    [Fact]
    public void Production_MassTransit_Consumers_Dispatching_Idempotent_Commands_Bind_Key_From_EventId()
    {
        var idempotentNames = GetIdempotentTypeNames();
        var files = Directory
            .EnumerateFiles(GetSrcPath(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

        var violations = files
            .SelectMany(file => ScanMassTransitConsumers(File.ReadAllText(file), idempotentNames)
                .Select(v => $"{Path.GetRelativePath(GetSrcPath(), file)}: {v}"))
            .ToList();

        violations.Should().BeEmpty(
            "MassTransit consumers dispatching idempotent Application commands must bind the execution key " +
            "from the message EventId with source Message (spec 3.4)");
    }

    [Fact]
    public void MassTransit_Scanner_Flags_BusinessDerived_Key_Binding()
    {
        var source = """
            using MassTransit;

            namespace Fixture;

            public sealed class BadConsumer : IConsumer<SomeEvent>
            {
                private readonly ISender _sender;
                private readonly IIdempotencyExecutionContextWriter _writer;

                public async Task Consume(ConsumeContext<SomeEvent> context)
                {
                    var msg = context.Message;
                    _writer.Set(msg.OrderNumber.ToString("N"), IdempotencyExecutionSource.Message);
                    await _sender.Send(new FixtureCommand());
                }
            }
            """;

        var violations = ScanMassTransitConsumers(source, new HashSet<string> { "FixtureCommand" });

        violations.Should().HaveCount(1, "a business-derived key must be flagged");
    }

    [Fact]
    public void MassTransit_Scanner_Flags_Missing_Key_Binding()
    {
        var source = """
            using MassTransit;

            namespace Fixture;

            public sealed class NoKeyConsumer : IConsumer<SomeEvent>
            {
                private readonly ISender _sender;

                public async Task Consume(ConsumeContext<SomeEvent> context)
                {
                    await _sender.Send(new FixtureCommand());
                }
            }
            """;

        var violations = ScanMassTransitConsumers(source, new HashSet<string> { "FixtureCommand" });

        violations.Should().HaveCount(1, "a missing execution-key binding must be flagged");
    }

    [Fact]
    public void MassTransit_Scanner_Accepts_EventId_Key_Binding()
    {
        var source = """
            using MassTransit;

            namespace Fixture;

            public sealed class GoodConsumer : IConsumer<SomeEvent>
            {
                private readonly ISender _sender;
                private readonly IIdempotencyExecutionContextWriter _writer;

                public async Task Consume(ConsumeContext<SomeEvent> context)
                {
                    var msg = context.Message;
                    _writer.Set(msg.EventId.ToString("N"), IdempotencyExecutionSource.Message);
                    await _sender.Send(new FixtureCommand());
                }
            }
            """;

        var violations = ScanMassTransitConsumers(source, new HashSet<string> { "FixtureCommand" });

        violations.Should().BeEmpty("an EventId-derived key with source Message is the spec 3.4 contract");
    }

    [Fact]
    public void MassTransit_Scanner_Ignores_Consumers_Without_Idempotent_Dispatch()
    {
        var source = """
            using MassTransit;

            namespace Fixture;

            public sealed class RuntimeConsumer : IConsumer<SomeEvent>
            {
                public async Task Consume(ConsumeContext<SomeEvent> context)
                {
                    await Task.CompletedTask;
                }
            }
            """;

        var violations = ScanMassTransitConsumers(source, new HashSet<string> { "FixtureCommand" });

        violations.Should().BeEmpty("plain runtime consumers are outside the idempotency contract");
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
