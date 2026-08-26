using Notrelix.Application.Common.Security;

namespace Notrelix.Architecture.Tests.Pipeline;

/// <summary>
/// IA-TST-FREEZE — canonical executable specification of ADR-006 (freeze file 05).
/// Proves the production pipeline registers exactly the seven frozen behaviors in
/// order, that no legacy orchestration implementation survives, and that durable
/// automation has no process-local accepted-work path.
/// </summary>
public sealed class PipelineFreezeArchitectureTests
{
    private static readonly string[] FrozenOrder =
    [
        "ExceptionMappingBehavior",
        "ApplicationTracingBehavior",
        "RequestContractBehavior",
        "ExecutionContextBehavior",
        "DataSessionBehavior",
        "AccessControlBehavior",
        "IdempotencyBehavior",
    ];

    private static readonly string[] ForbiddenLegacyBehaviors =
    [
        "AuthorizationBehavior",
        "FeatureGateBehavior",
        "SubscriptionGateBehavior",
        "VerifiedEmailBehavior",
        "ConcurrencyBehavior",
        "DbRequestScopeBehavior",
        "TransactionalBehavior",
        "RealtimeBehavior",
        "CacheInvalidationBehavior",
        "PostCommitEnqueueBehavior",
        "PostCommitActionBehavior",
        "AuthorizedCacheBehavior",
        "PublicCacheBehavior",
    ];

    [Fact]
    public void Pipeline_RegistersExactlySevenBehaviors_InFrozenOrder()
    {
        var source = ReadApplicationSource("DependencyInjection.cs");

        var registrations = Regex.Matches(source,
                @"AddTransient\(typeof\(IPipelineBehavior<,>\),\s*typeof\((?<name>[A-Za-z]+)Behavior<,>\)\)")
            .Select(match => match.Groups["name"].Value + "Behavior")
            .ToArray();

        registrations.Should().Equal(FrozenOrder,
            "ADR-006 freezes both the behavior set and its outermost-to-innermost order");
    }

    [Fact]
    public void ProductionApplicationAssembly_ContainsNoEighthOrchestrationBehavior()
    {
        var behaviorTypes = typeof(Notrelix.Application.Common.Behaviors.ExceptionMappingBehavior<,>)
            .Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false }
                && type.GetInterfaces()
                    .Any(interfaceType => interfaceType.IsGenericType
                        && interfaceType.GetGenericTypeDefinition().Name.StartsWith("IPipelineBehavior", StringComparison.Ordinal)))
            .Select(type => type.Name)
            .OrderBy(name => name)
            .ToArray();

        behaviorTypes.Should().BeEquivalentTo(
        [
            "AccessControlBehavior`2",
            "ApplicationTracingBehavior`2",
            "DataSessionBehavior`2",
            "ExceptionMappingBehavior`2",
            "ExecutionContextBehavior`2",
            "IdempotencyBehavior`2",
            "RequestContractBehavior`2",
        ]);
    }

    [Fact]
    public void ProductionSource_ContainsNoForbiddenLegacyOrchestration()
    {
        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(FindBackendRoot(), "src", ApplicationRoot), "*.cs", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            foreach (var forbidden in ForbiddenLegacyBehaviors)
            {
                if (content.Contains($"class {forbidden}<", StringComparison.Ordinal) ||
                    content.Contains($"class {forbidden}:", StringComparison.Ordinal) ||
                    content.Contains($"class {forbidden} ", StringComparison.Ordinal))
                {
                    offenders.Add($"{file}: {forbidden}");
                }
            }
        }

        offenders.Should().BeEmpty("legacy orchestration implementations are forbidden after freeze");
    }

    [Fact]
    public void Automation_HasNoProcessLocalAcceptedWorkQueuePath()
    {
        var offenders = new List<string>();
        var automationRoot = Path.Combine(FindBackendRoot(), "src", ApplicationRoot, "Features", "Automation");

        foreach (var file in Directory.EnumerateFiles(automationRoot, "*.cs", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            foreach (var banned in new[] { "IJobQueue", "InMemoryJobQueue", "QueuedJobWorker", "N8nDispatchJob" })
            {
                if (content.Contains(banned, StringComparison.Ordinal))
                {
                    offenders.Add($"{file}: {banned}");
                }
            }
        }

        offenders.Should().BeEmpty("accepted automation work must flow only through the transactional outbox");
    }

    [Fact]
    public void AccessPolicyEngine_RemainsPure()
    {
        typeof(AccessPolicyEngine).GetConstructors()
            .Should().OnlyContain(constructor => constructor.GetParameters().Length == 0,
                "the policy engine must not acquire I/O dependencies");
    }

    // --- helpers -------------------------------------------------------------

    private const string ApplicationRoot = "Notrelix.Application";

    private static string FindBackendRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", ApplicationRoot)))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new DirectoryNotFoundException("Could not locate backend/src root from test directory");
        }

        return dir.FullName;
    }

    private static string ReadApplicationSource(string fileName)
    {
        var path = Path.Combine(FindBackendRoot(), "src", ApplicationRoot, fileName);
        return File.ReadAllText(path);
    }
}
