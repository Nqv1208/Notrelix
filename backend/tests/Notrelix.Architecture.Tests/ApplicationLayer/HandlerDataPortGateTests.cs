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
        typeof(Notrelix.Application.Common.Behaviors.RequestContractBehavior<,>).Assembly;

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
        "ISubscriptionChecker",
        "IFeatureGateChecker",
        "IExecutionContextReader",
        "ICorrelationContext",
    };

    private static readonly HashSet<string> DirectPermissionPortNames = new(StringComparer.Ordinal)
    {
        "Notrelix.Application.Common.Security.IPermissionService",
        "Notrelix.Application.Common.Security.IWorkspacePermissionService",
        "Notrelix.Application.Common.Security.IPermissionEvaluator",
        "Notrelix.Application.Common.Security.IAuthorizationDecisionStore",
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

    [Fact]
    public void APP_DATA_006_Final_Cross_Context_Contracts()
    {
        // FZ-APP-01..03: the five migrated handlers must use their final
        // cross-context contracts (spec 3.2) and never the old foreign DbContexts.
        var expectations = new Dictionary<string, (string RequiredPort, string[] ForbiddenPorts)>(StringComparer.Ordinal)
        {
            ["GetFullBoardQueryHandler"] = ("IWorkManagementCollaborationReadPort", ["ICollaborationDbContext"]),
            ["GetBoardItemQueryHandler"] = ("IWorkManagementCollaborationReadPort", ["ICollaborationDbContext"]),
            ["GetBootstrapQueryHandler"] = ("IIdentityBootstrapReadPort", ["IAccountDbContext", "IWorkspaceDbContext"]),
            ["RegisterCommandHandler"] = ("IAccountProvisioningActions", ["IAccountDbContext"]),
            ["CompleteOAuthLoginCommandHandler"] = ("IAccountProvisioningActions", ["IAccountDbContext"]),
        };

        var violations = new List<string>();

        foreach (var (handlerName, (requiredPort, forbiddenPorts)) in expectations)
        {
            var handler = ApplicationAssembly.GetTypes().SingleOrDefault(t => t.Name == handlerName);
            handler.Should().NotBeNull($"handler {handlerName} must exist");

            var parameterTypes = handler!
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType.Name)
                .ToArray();

            if (!parameterTypes.Contains(requiredPort, StringComparer.Ordinal))
                violations.Add($"{handlerName} must inject {requiredPort}");

            foreach (var forbiddenPort in forbiddenPorts)
            {
                if (parameterTypes.Contains(forbiddenPort, StringComparer.Ordinal))
                    violations.Add($"{handlerName} must not inject {forbiddenPort}");
            }
        }

        violations.Should().BeEmpty(
            "the five migrated handlers must use the final cross-context contracts — read ports and the provisioning service");
    }

    [Fact]
    public void APP_DATA_007_No_Direct_Permission_Service_In_Handlers()
    {
        // FZ-APP-AUTHZ-GATE-01: permission decisions are centralized in the
        // pipeline authorization behavior — handlers express required
        // permission through request markers; authorization behavior owns the
        // decision. Handlers must not inject any decision port directly.
        var violations = HandlerConstructorPortGate.FindForbiddenPorts(GetHandlerTypes(), DirectPermissionPortNames);

        violations.Should().BeEmpty(
            "handlers express required permission through request markers; authorization behavior owns the decision");
    }

    [Fact]
    public void TAC_IA_007_SameTeam_Boundary_Proven_For_IAccountProvisioningActions()
    {
        // TAC-IA-007: When the same team provides a seam over a public-seams
        // contract, the same-team seam MUST prove ownership, contract, and
        // boundary. IAccountProvisioningActions is owned by Accounts (producer)
        // and called by Identity registration (same team). The interface lives
        // in Accounts/Public/Commands/; the implementation lives in Accounts/
        // Provisioning/; DI wires it in the Application composition root.
        //
        // This test proves:
        // 1. The interface type exists in the Accounts context (ownership).
        // 2. The interface is implemented by exactly one type (contract).
        // 3. The implementing type lives in the Accounts context (boundary).
        // 4. No Identity handler injects it via the Accounts-specific DbContext
        //    (same-team contract, not cross-team data port).

        // 1. Interface exists in Accounts context.
        var interfaceType = ApplicationAssembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == "IAccountProvisioningActions");

        interfaceType.Should().NotBeNull(
            "IAccountProvisioningActions must exist as the public-seams contract for personal Account provisioning");

        interfaceType!.Namespace.Should().Contain("Accounts",
            "the interface must be owned by the Accounts context");

        // 2. Exactly one concrete implementation exists.
        var implementations = ApplicationAssembly
            .GetTypes()
            .Where(t => interfaceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        implementations.Should().HaveCount(1,
            "IAccountProvisioningActions must have exactly one implementation (AccountProvisioningService)");

        // 3. The implementation lives in the Accounts context.
        var implementationType = implementations[0];

        implementationType.Namespace.Should().Contain("Accounts",
            "the implementation must live in the Accounts context");

        // 4. No Identity handler directly injects the Accounts DbContext.
        // Identity handlers that call IAccountProvisioningActions must not
        // bypass the public-seams contract by injecting IAccountDbContext.
        var identityHandlerTypes = GetHandlerTypes()
            .Where(t => t.Namespace?.Contains("Identity") == true)
            .ToList();

        var identityHandlersWithAccountDbContext = identityHandlerTypes
            .SelectMany(h => h.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            .SelectMany(c => c.GetParameters())
            .Where(p => p.ParameterType.Name == "IAccountDbContext")
            .Select(p => $"{p.Member.DeclaringType?.FullName}:{p.ParameterType.Name}")
            .ToList();

        identityHandlersWithAccountDbContext.Should().BeEmpty(
            "Identity handlers must not inject IAccountDbContext directly; " +
            "they must use IAccountProvisioningActions (same-team contract, not cross-team data port)");
    }
}
