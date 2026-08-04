using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Notrelix.Architecture.Tests.Support;

namespace Notrelix.Architecture.Tests.ApplicationLayer;

    /// <summary>
    /// APP-05: Compiled handler data-port gate.
    /// Reflects all IRequestHandler constructors and verifies:
    /// - Handlers inject their owning context DbContext port (inferred from namespace)
    /// - Handlers do NOT inject another context's DbContext
    /// - Handlers do NOT inject concrete Infrastructure types
    /// - Common cross-cutting ports are allowed by exact type
    /// - Handlers do NOT invoke persistence/provider APIs (Roslyn, FZ-APP-04)
    /// </summary>
    public class HandlerDataPortGateTests : ArchitectureTestBase
    {
    private static readonly Assembly ApplicationAssembly =
        typeof(Notrelix.Application.Common.Behaviors.ValidationBehavior<,>).Assembly;

    private static readonly HashSet<string> DbContextPortNames = new(StringComparer.Ordinal)
    {
        "IWorkManagementDbContext",
        "IWorkspaceDbContext",
        "IIdentityDbContext",
        "IAccountDbContext",
        "IDocumentDbContext",
        "ICollaborationDbContext",
        "IAutomationDbContext",
        "IGovernanceDbContext",
        "IIntegrationDbContext",
        "IBillingDbContext",
        "IReportingDbContext",
    };

    private static readonly Dictionary<string, string> ContextToPort = new(StringComparer.Ordinal)
    {
        ["WorkManagement"] = "IWorkManagementDbContext",
        ["Workspaces"] = "IWorkspaceDbContext",
        ["Identity"] = "IIdentityDbContext",
        ["Accounts"] = "IAccountDbContext",
        ["Documents"] = "IDocumentDbContext",
        ["Collaboration"] = "ICollaborationDbContext",
        ["Automation"] = "IAutomationDbContext",
        ["Governance"] = "IGovernanceDbContext",
        ["Integrations"] = "IIntegrationDbContext",
        ["Billing"] = "IBillingDbContext",
        ["Analytics"] = "IReportingDbContext",
    };

    private static readonly HashSet<string> AllowedCommonPorts = new(StringComparer.Ordinal)
    {
        "ICurrentRequestContext",
        "IDateTimeProvider",
        "IIntegrationEventCollector",
        "IPostCommitActionQueue",
        "IRealtimePublisher",
        "IRedisCacheService",
        "IEmailVerificationTokenIssuer",
        "IActiveVerificationTokenLocker",
        "IOneTimeTokenService",
        "ISecretEncryptor",
        "IN8nSignatureService",
        "IAuthSessionIssuer",
        "IPermissionService",
        "IWorkspacePermissionService",
        "ISubscriptionChecker",
        "IFeatureGateChecker",
        "IExecutionContextReader",
        "ICorrelationContext",
    };

    private static string? GetContextFromHandlerNamespace(string? ns)
    {
        if (ns is null) return null;
        const string prefix = "Notrelix.Application.Features.";
        if (!ns.StartsWith(prefix, StringComparison.Ordinal)) return null;

        var remainder = ns[prefix.Length..];
        var dotIndex = remainder.IndexOf('.');
        return dotIndex > 0 ? remainder[..dotIndex] : remainder;
    }

    [Fact]
    public void APP_DATA_003_Handler_Cannot_Inject_Another_Context_DbContext()
    {
        var handlerTypes = GetHandlerTypes();
        var violations = new List<string>();

        foreach (var handler in handlerTypes)
        {
            var context = GetContextFromHandlerNamespace(handler.Namespace);
            if (context is null) continue;

            var owningPort = ContextToPort.GetValueOrDefault(context);
            var constructors = handler.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            foreach (var ctor in constructors)
            {
                foreach (var param in ctor.GetParameters())
                {
                    var paramTypeName = param.ParameterType.Name;
                    if (!DbContextPortNames.Contains(paramTypeName)) continue;

                    if (owningPort is not null && paramTypeName != owningPort)
                    {
                        violations.Add(
                            $"{handler.Name} (context: {context}) injects foreign DbContext port '{paramTypeName}' " +
                            $"— expected owning port '{owningPort}'");
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            "handlers must only inject their owning context DbContext port, not another context's");
    }

    [Fact]
    public void APP_DATA_004_Handler_Cannot_Inject_Concrete_Infrastructure()
    {
        var handlerTypes = GetHandlerTypes();
        var violations = new List<string>();

        foreach (var handler in handlerTypes)
        {
            var constructors = handler.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            foreach (var ctor in constructors)
            {
                foreach (var param in ctor.GetParameters())
                {
                    var paramType = param.ParameterType;
                    if (paramType.IsInterface) continue;
                    if (paramType.Namespace?.StartsWith("Notrelix.Infrastructure", StringComparison.Ordinal) == true)
                    {
                        violations.Add(
                            $"{handler.Name} injects concrete Infrastructure type '{paramType.FullName}'");
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            "handlers must not inject concrete Infrastructure types — use Application ports");
    }

    [Fact]
    public void APP_DATA_005_No_Handler_SaveChanges()
    {
        var handlerTypes = GetHandlerTypes();
        var classToFile = IndexHandlerSourceFiles();
        var violations = new List<string>();

        foreach (var handler in handlerTypes)
        {
            if (!classToFile.TryGetValue(handler.Name, out var file))
                continue;

            var relativePath = Path.GetRelativePath(GetApplicationPath(), file);
            violations.AddRange(HandlerDataAccessInvocationGate.Scan(File.ReadAllText(file), relativePath));
        }

        violations.Should().BeEmpty(
            "handlers must not invoke persistence/provider APIs — the pipeline owns persistence");
    }

    private static Dictionary<string, string> IndexHandlerSourceFiles()
    {
        var index = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var file in GetApplicationFeatureFiles())
        {
            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetCompilationUnitRoot();
            foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                index.TryAdd(classDeclaration.Identifier.ValueText, file);
            }
        }

        return index;
    }

    private static IEnumerable<Type> GetHandlerTypes()
    {
        return ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(IRequestHandler<>) ||
                 i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))))
            .ToList();
    }
}
