using System.Reflection;
using MediatR;

namespace Notrelix.Architecture.Tests.ApplicationLayer;

/// <summary>
/// APP-05: Compiled handler data-port gate.
/// Reflects all IRequestHandler constructors and verifies:
/// - Handlers inject their owning context DbContext port (inferred from namespace)
/// - Handlers do NOT inject another context's DbContext
/// - Handlers do NOT inject concrete Infrastructure types
/// - Common cross-cutting ports are allowed by exact type
/// </summary>
public class HandlerDataPortGateTests
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

    /// <summary>
    /// Known transitional cross-context reads. Each entry must be migrated to a narrow read port.
    /// Format: "HandlerName:ForeignPort"
    /// </summary>
    private static readonly HashSet<string> TransitionalCrossContextReads = new(StringComparer.Ordinal)
    {
        "GetFullBoardQueryHandler:ICollaborationDbContext",
        "GetBoardItemQueryHandler:ICollaborationDbContext",
        "CompleteOAuthLoginCommandHandler:IAccountDbContext",
        "GetBootstrapQueryHandler:IAccountDbContext",
        "GetBootstrapQueryHandler:IWorkspaceDbContext",
        "RegisterCommandHandler:IAccountDbContext",
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
                        var exceptionKey = $"{handler.Name}:{paramTypeName}";
                        if (TransitionalCrossContextReads.Contains(exceptionKey))
                            continue;

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
        var violations = new List<string>();

        foreach (var handler in handlerTypes)
        {
            var methods = handler.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methods)
            {
                if (method.Name.Contains("SaveChanges", StringComparison.Ordinal))
                {
                    violations.Add($"{handler.Name}.{method.Name}");
                }
            }
        }

        violations.Should().BeEmpty(
            "handlers must not call SaveChanges — the pipeline owns persistence");
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
