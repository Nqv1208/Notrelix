namespace Notrelix.Architecture.Tests;

public class ApplicationArchitectureTests
{
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

    private static string[] GetApplicationFeatureFiles()
    {
        var appPath = GetApplicationPath();
        return Directory.GetFiles(Path.Combine(appPath, "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
    }

    [Fact]
    public void RequestRecords_ShouldNotUse_RawIRequest()
    {
        var files = GetApplicationFeatureFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));

            var lines = content.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Contains(": IRequest<") || trimmed.Contains(": IRequest,") || trimmed == ": IRequest")
                {
                    violations.Add($"{Path.GetFileName(file)}: {trimmed}");
                }
            }
        }

        violations.Should().BeEmpty($"Request records must use ICommand/IQuery instead of raw IRequest: {string.Join(", ", violations)}");
    }

    [Fact]
    public void RequestRecords_ShouldImplement_ICommandOrIQuery()
    {
        var appPath = GetApplicationPath();
        var requestFiles = Directory.GetFiles(Path.Combine(appPath, "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && (f.Contains($"{Path.DirectorySeparatorChar}Commands{Path.DirectorySeparatorChar}")
                      || f.Contains($"{Path.DirectorySeparatorChar}Queries{Path.DirectorySeparatorChar}")))
            .ToArray();
        // Result DTOs that live in Commands/Queries folders but are not requests
        var resultDtoExclusions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SendWelcomeEmailResult.cs",
            "AcceptInvitation.cs",
            "ReorderBlocks.cs",
            "GetBoard.cs",
            "GetBoardSchemaQuery.cs",
        };

        var violations = new List<string>();

        foreach (var file in requestFiles)
        {
            if (resultDtoExclusions.Contains(Path.GetFileName(file)))
                continue;

            var content = RemoveComments(File.ReadAllText(file));

            var lines = content.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].Trim();
                if (!trimmed.StartsWith("public record") && !trimmed.StartsWith("public sealed record"))
                    continue;

                // Collect full declaration — handles multi-line records
                var declaration = trimmed;
                var parenDepth = trimmed.Count(c => c == '(') - trimmed.Count(c => c == ')');

                if (parenDepth != 0 || (!trimmed.Contains(';') && !trimmed.Contains('{') && !trimmed.Contains(':')))
                {
                    for (var j = i + 1; j < lines.Length && parenDepth >= 0; j++)
                    {
                        var nextLine = lines[j].Trim();
                        declaration += " " + nextLine;
                        parenDepth += nextLine.Count(c => c == '(') - nextLine.Count(c => c == ')');
                    }
                }

                if (declaration.Contains("class ") || declaration.Contains("static "))
                    continue;

                var hasICommand = declaration.Contains(": ICommand") || declaration.Contains(", ICommand");
                var hasIQuery = declaration.Contains(": IQuery") || declaration.Contains(", IQuery");

                if (!hasICommand && !hasIQuery)
                {
                    violations.Add($"{Path.GetFileName(file)}: {declaration}");
                }
            }
        }

        violations.Should().BeEmpty($"Request records must implement ICommand or IQuery: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Application_ShouldNotReference_InfrastructureOrApi()
    {
        var appPath = GetApplicationPath();
        var files = Directory.GetFiles(appPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));

            if (content.Contains("using Notrelix.Infrastructure") ||
                content.Contains("using Notrelix.Api"))
            {
                violations.Add(Path.GetFileName(file));
            }
        }

        violations.Should().BeEmpty($"Application must not reference Infrastructure or Api projects: {string.Join(", ", violations)}");
    }

    [Fact]
    public void PipelineBehaviorOrder_ShouldHaveCorrectOrder()
    {
        var diFile = Path.Combine(GetApplicationPath(), "DependencyInjection.cs");
        var content = RemoveComments(File.ReadAllText(diFile));

        var lines = content.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Contains("AddTransient(typeof(IPipelineBehavior<"))
            .ToList();

        lines.Should().HaveCount(17, "expected exactly 17 pipeline behaviors");

        var expectedOrder = new[]
        {
            "ExceptionMappingBehavior",
            "ApplicationTracingBehavior",
            "ValidationBehavior",
            "RequestContractGuardBehavior",
            "TenantBootstrapBehavior",
            "ResourceScopeBehavior",
            "PostCommitScopeBehavior",
            "PublicCacheBehavior",
            "DbRequestScopeBehavior",
            "AuthorizationBehavior",
            "VerifiedEmailBehavior",
            "ConcurrencyBehavior",
            "SubscriptionGateBehavior",
            "FeatureGateBehavior",
            "IdempotencyBehavior",
            "PostCommitEnqueueBehavior",
            "AuthorizedCacheBehavior",
        };

        for (var i = 0; i < expectedOrder.Length; i++)
        {
            lines[i].Should().Contain(expectedOrder[i], $"behavior at position {i} should be {expectedOrder[i]}");
        }
    }

    [Fact]
    public void CommandHandlers_ShouldNotCall_SaveChangesAsync()
    {
        var appPath = GetApplicationPath();
        var files = Directory.GetFiles(Path.Combine(appPath, "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
        var violations = new List<string>();

        var allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "N8nAutomationEventHandlers.cs",
        };

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (allowedFiles.Contains(fileName)) continue;

            var content = RemoveComments(File.ReadAllText(file));
            if (!content.Contains("IRequestHandler<")) continue;

            if (content.Contains("SaveChangesAsync"))
            {
                violations.Add(fileName);
            }
        }

        violations.Should().BeEmpty($"Command/query handlers must not call SaveChangesAsync directly. TransactionalBehavior handles it. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void NoOldBehaviorFilesExist()
    {
        var behaviorsPath = Path.Combine(GetApplicationPath(), "Common", "Behaviors");
        var deletedBehaviors = new[]
        {
            "RlsSessionBehavior.cs",
            "TransactionalBehavior.cs",
            "CacheInvalidationBehavior.cs",
            "RealtimeBehavior.cs",
            "PostCommitActionBehavior.cs",
            "EntitlementBehavior.cs",
        };

        var violations = new List<string>();
        foreach (var behavior in deletedBehaviors)
        {
            var fullPath = Path.Combine(behaviorsPath, behavior);
            if (File.Exists(fullPath))
                violations.Add(behavior);
        }

        violations.Should().BeEmpty($"Old behavior files must be deleted from {behaviorsPath}: {string.Join(", ", violations)}");
    }

    [Fact]
    public void OnlyDbRequestScopeBehaviorCanCallRlsApply()
    {
        var behaviorsPath = Path.Combine(GetApplicationPath(), "Common", "Behaviors");
        var files = Directory.GetFiles(behaviorsPath, "*.cs")
            .Where(f => !f.EndsWith("DbRequestScopeBehavior.cs"))
            .ToArray();

        var violations = new List<string>();
        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            if (content.Contains("ApplyAsync") || content.Contains(".CommandText"))
                violations.Add(Path.GetFileName(file));
        }

        violations.Should().BeEmpty($"Only DbRequestScopeBehavior should call RLS ApplyAsync. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void OnlyDbRequestScopeBehaviorCanBeginTransaction()
    {
        var behaviorsPath = Path.Combine(GetApplicationPath(), "Common", "Behaviors");
        var files = Directory.GetFiles(behaviorsPath, "*.cs")
            .Where(f => !f.EndsWith("DbRequestScopeBehavior.cs"))
            .ToArray();

        var violations = new List<string>();
        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (content.Contains("BeginTransaction") || content.Contains("CommitAsync"))
                violations.Add(Path.GetFileName(file));
        }

        violations.Should().BeEmpty($"Only DbRequestScopeBehavior should begin transactions/commits. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void NoSeparatePostCommitBehaviorsExist()
    {
        var behaviorsPath = Path.Combine(GetApplicationPath(), "Common", "Behaviors");
        var forbidden = new[] { "CacheInvalidation", "Realtime" };

        var files = Directory.GetFiles(behaviorsPath, "*.cs");
        var violations = new List<string>();
        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (forbidden.Any(f => name.StartsWith(f, StringComparison.OrdinalIgnoreCase)))
                violations.Add(Path.GetFileName(file));
        }

        violations.Should().BeEmpty($"No separate CacheInvalidation/Realtime behaviors should exist. PostCommitEnqueueBehavior replaces both. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void NoWorkspaceRequestImplementsPublicCacheQuery()
    {
        var appPath = GetApplicationPath();
        var files = Directory.GetFiles(Path.Combine(appPath, "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (content.Contains("IWorkspaceRequest") && content.Contains("IPublicCacheableQuery"))
                violations.Add(Path.GetFileName(file));
        }

        violations.Should().BeEmpty("No request should implement both IWorkspaceRequest and IPublicCacheableQuery — workspace-scoped data must not be publicly cached: " + string.Join(", ", violations));
    }

    [Fact]
    public void RlsSessionContextIsRequiredInDbRequestScope()
    {
        var behaviorPath = Path.Combine(GetApplicationPath(), "Common", "Behaviors", "DbRequestScopeBehavior.cs");
        var content = File.ReadAllText(behaviorPath);

        content.Should().Contain("IRlsSessionContext", "DbRequestScopeBehavior must inject IRlsSessionContext for RLS enforcement");
    }

    [Fact]
    public void CommandHandlers_ShouldNotInjectWorkspacePermissionService()
    {
        var appPath = GetApplicationPath();
        var handlerFiles = Directory.GetFiles(Path.Combine(appPath, "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
        var violations = new List<string>();

        foreach (var file in handlerFiles)
        {
            var fileName = Path.GetFileName(file);

            var content = RemoveComments(File.ReadAllText(file));
            if (content.Contains("WorkspacePermissionService") || content.Contains("IWorkspacePermissionService"))
                violations.Add(fileName);
        }

        violations.Should().BeEmpty("No handlers should inject IWorkspacePermissionService. Use pipeline authorization (IRequirePermission) instead. Violations: " + string.Join(", ", violations));
    }

    [Fact]
    public void SubscriptionGateBehavior_UsesAccountId()
    {
        var behaviorPath = Path.Combine(GetApplicationPath(), "Common", "Behaviors", "SubscriptionGateBehavior.cs");
        var content = File.ReadAllText(behaviorPath);

        content.Should().Contain("ISubscriptionChecker", "SubscriptionGateBehavior must use ISubscriptionChecker");
        content.Should().Contain("AccountId", "SubscriptionGateBehavior must use AccountId, not WorkspaceId");
    }

    [Fact]
    public void FeatureGateBehavior_UsesAccountId()
    {
        var behaviorPath = Path.Combine(GetApplicationPath(), "Common", "Behaviors", "FeatureGateBehavior.cs");
        var content = File.ReadAllText(behaviorPath);

        content.Should().Contain("IFeatureGateChecker", "FeatureGateBehavior must use IFeatureGateChecker");
        content.Should().Contain("AccountId", "FeatureGateBehavior must use AccountId, not WorkspaceId");
    }

    [Fact]
    public void NoCrossBoundedContextNotificationService()
    {
        var appPath = GetApplicationPath();
        var files = Directory.GetFiles(appPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (content.Contains("INotificationService"))
                violations.Add(Path.GetFileName(file));
        }

        violations.Should().BeEmpty("No production code should reference INotificationService. Use domain events for cross-BC communication. Violations: " + string.Join(", ", violations));
    }

    [Fact]
    public void RateLimitingMiddleware_ExistsInPipeline()
    {
        var srcDir = Path.GetDirectoryName(GetApplicationPath())!;
        var programPath = Path.Combine(srcDir, "Notrelix.API", "Program.cs");
        var content = File.ReadAllText(programPath);

        content.Should().Contain("PreAuthenticationRateLimitMiddleware", "Pre-auth rate limiting middleware must be registered");
        content.Should().Contain("AuthenticatedRateLimitMiddleware", "Authenticated rate limiting middleware must be registered");
    }

    [Fact]
    public void RateLimitingOptions_AreConfigured()
    {
        var srcDir = Path.GetDirectoryName(GetApplicationPath())!;
        var diPath = Path.Combine(srcDir, "Notrelix.API", "DependencyInjection.cs");
        var content = File.ReadAllText(diPath);

        content.Should().Contain("RateLimitingOptions", "Rate limiting options must be configured in the API DI");
        content.Should().Contain("IRateLimitPolicyProvider", "Rate limit policy provider must be registered");
    }

    [Fact]
    public void NoCommandImplementsPublicCacheQuery()
    {
        var appPath = GetApplicationPath();
        var commandFiles = Directory.GetFiles(Path.Combine(appPath, "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && f.Contains($"{Path.DirectorySeparatorChar}Commands{Path.DirectorySeparatorChar}"))
            .ToArray();
        var violations = new List<string>();

        foreach (var file in commandFiles)
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (content.Contains("IPublicCacheableQuery"))
                violations.Add(Path.GetFileName(file));
        }

        violations.Should().BeEmpty("Commands must not implement IPublicCacheableQuery — cache is query-only: " + string.Join(", ", violations));
    }

    [Fact]
    public void SubscriptionGateBehavior_ThrowsOnMissingAccountId()
    {
        var behaviorPath = Path.Combine(GetApplicationPath(), "Common", "Behaviors", "SubscriptionGateBehavior.cs");
        var content = File.ReadAllText(behaviorPath);

        content.Should().Contain("SecurityMisconfigurationException",
            "SubscriptionGateBehavior must throw on missing AccountId, not skip (fail-closed)");
        content.Should().NotContain("gate skipped",
            "SubscriptionGateBehavior must not use 'skipped' wording (fail-closed)");
    }

    [Fact]
    public void ReadScope_SetsReadOnlyTransaction()
    {
        var behaviorPath = Path.Combine(GetApplicationPath(), "Common", "Behaviors", "DbRequestScopeBehavior.cs");
        var content = File.ReadAllText(behaviorPath);

        content.Should().Contain("SET TRANSACTION READ ONLY",
            "DbRequestScopeBehavior must set READ ONLY for non-write scopes to prevent accidental writes");
    }

    [Fact]
    public void IExecutionContextAccessor_Extends_IExecutionContextReader()
    {
        var contextPath = Path.Combine(GetApplicationPath(), "Common", "Context", "IExecutionContextAccessor.cs");
        var content = File.ReadAllText(contextPath);

        content.Should().Contain("IExecutionContextReader",
            "IExecutionContextAccessor must extend IExecutionContextReader");
    }

    [Fact]
    public void Handlers_InjectOnlyIExecutionContextReader()
    {
        var appPath = GetApplicationPath();
        var handlerFiles = Directory.GetFiles(Path.Combine(appPath, "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f => File.ReadAllText(f).Contains("IRequestHandler<"))
            .ToArray();
        var violations = new List<string>();

        foreach (var file in handlerFiles)
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (content.Contains("IExecutionContextAccessor"))
                violations.Add(Path.GetFileName(file));
        }

        violations.Should().BeEmpty("Handlers must not inject IExecutionContextAccessor (read-write). Use IExecutionContextReader (read-only) instead. Violations: " + string.Join(", ", violations));
    }

    [Fact]
    public void EntityFrameworkCoreRelational_IsReferencedByApplication()
    {
        var csprojPath = Path.Combine(GetApplicationPath(), "Notrelix.Application.csproj");
        var content = File.ReadAllText(csprojPath);

        content.Should().Contain("Microsoft.EntityFrameworkCore.Relational",
            "Application must reference EF Core Relational to use ExecuteSqlRawAsync for SET TRANSACTION READ ONLY");
    }

    [Fact]
    public void FeatureGateBehavior_ThrowsOnMissingAccountId()
    {
        var behaviorPath = Path.Combine(GetApplicationPath(), "Common", "Behaviors", "FeatureGateBehavior.cs");
        var content = File.ReadAllText(behaviorPath);

        content.Should().Contain("SecurityMisconfigurationException",
            "FeatureGateBehavior must throw on missing AccountId, not skip (fail-closed)");
        content.Should().NotContain("gate skipped",
            "FeatureGateBehavior must not use 'skipped' wording (fail-closed)");
    }

    [Fact]
    public void PostCommitAction_InterfaceExists()
    {
        var actionPath = Path.Combine(GetApplicationPath(), "Common", "PostCommit", "IPostCommitAction.cs");
        var content = File.ReadAllText(actionPath);

        content.Should().Contain("interface IPostCommitAction",
            "IPostCommitAction must exist for generic post-commit extensibility");
        content.Should().Contain("ExecuteAsync",
            "IPostCommitAction must define ExecuteAsync method");
    }

    [Fact]
    public void RateLimitMiddleware_UsesProblemDetails()
    {
        var middlewareDir = Path.GetDirectoryName(GetApplicationPath())!;
        middlewareDir = Path.Combine(middlewareDir, "Notrelix.API", "Middleware");
        var files = Directory.GetFiles(middlewareDir, "*RateLimit*.cs");

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            content.Should().Contain("ProblemDetailsWriter",
                $"{Path.GetFileName(file)} must use ProblemDetailsWriter instead of writing raw JSON");
            content.Should().NotContain("WriteAsJsonAsync",
                $"{Path.GetFileName(file)} must not use WriteAsJsonAsync directly");
        }
    }

    [Fact]
    public void RateLimitPolicy_UsesPartitionKeyEnum()
    {
        var policyPath = Path.Combine(GetApplicationPath(), "..", "Notrelix.API", "RateLimiting", "RateLimitPolicyProvider.cs");
        var content = File.ReadAllText(policyPath);

        content.Should().Contain("PartitionKey PartitionBy",
            "RateLimitPolicy must use the PartitionKey enum, not a string");
    }

    [Fact]
    public void PlaceholderRegistrations_AreSplit()
    {
        var infraDiDir = Path.Combine(Path.GetDirectoryName(GetApplicationPath())!, "Notrelix.Infrastructure", "DependencyInjection");
        var files = Directory.GetFiles(infraDiDir, "*Registration.cs")
            .Select(Path.GetFileName)
            .OrderBy(f => f)
            .ToArray();

        files.Should().Contain("StorageRegistration.cs", "Storage registration must be in its own file");
        files.Should().Contain("BillingRegistration.cs", "Billing registration must be in its own file");
        files.Should().Contain("OperationsRegistration.cs", "Operations registration must be in its own file");
        files.Should().Contain("ObservabilityRegistration.cs", "Observability registration must be in its own file");
        files.Should().NotContain(f => f.Contains("Placeholder"),
            "No PlaceholderRegistrations.cs file should exist");
    }

    [Fact]
    public void DatabaseSubscriptionChecker_Exists()
    {
        var infraPath = Path.Combine(Path.GetDirectoryName(GetApplicationPath())!, "Notrelix.Infrastructure", "Billing");
        var files = Directory.GetFiles(infraPath, "DatabaseSubscriptionChecker.cs");
        files.Should().NotBeEmpty("DatabaseSubscriptionChecker must exist for production billing checks");
    }

    [Fact]
    public void DatabaseFeatureGateChecker_Exists()
    {
        var infraPath = Path.Combine(Path.GetDirectoryName(GetApplicationPath())!, "Notrelix.Infrastructure", "Billing");
        var files = Directory.GetFiles(infraPath, "DatabaseFeatureGateChecker.cs");
        files.Should().NotBeEmpty("DatabaseFeatureGateChecker must exist for production feature gate checks");
    }

    [Fact]
    public void PostCommitActionQueue_SupportsGenericActions()
    {
        var queueInterfacePath = Path.Combine(GetApplicationPath(), "Common", "PostCommit", "IPostCommitActionQueue.cs");
        var content = File.ReadAllText(queueInterfacePath);

        content.Should().Contain("Enqueue(IPostCommitAction",
            "IPostCommitActionQueue must support generic IPostCommitAction enqueue");
        content.Should().Contain("IReadOnlyList<IPostCommitAction> Actions",
            "IPostCommitActionQueue must expose generic actions list");
    }

    [Fact]
    public void RlsSessionContext_ThrowsWhenSetSessionContextDisabledForNonSystem()
    {
        var path = Path.Combine(
            Path.GetDirectoryName(GetApplicationPath())!,
            "Notrelix.Infrastructure", "Data", "Rls", "RlsSessionContext.cs");
        var content = File.ReadAllText(path);

        content.Should().Contain("throw new InvalidOperationException",
            "RlsSessionContext must throw when SetSessionContext is disabled for non-system requests");
        content.Should().Contain("SetSessionContext",
            "Exception message must mention SetSessionContext being disabled");
    }

    [Fact]
    public void RlsSessionContext_ThrowsWhenAccountIdMissingForNonSystem()
    {
        var path = Path.Combine(
            Path.GetDirectoryName(GetApplicationPath())!,
            "Notrelix.Infrastructure", "Data", "Rls", "RlsSessionContext.cs");
        var content = File.ReadAllText(path);

        content.Should().Contain("InvalidOperationException",
            "Must throw InvalidOperationException when AccountId missing for non-system context");
    }

    [Fact]
    public void IntegrationEventScope_EnumExists()
    {
        var path = Path.Combine(GetApplicationPath(), "Common", "Messaging", "IntegrationEventScope.cs");
        var content = File.ReadAllText(path);

        content.Should().Contain("enum IntegrationEventScope", "IntegrationEventScope enum must exist");
        content.Should().Contain("SystemEvent", "IntegrationEventScope must include SystemEvent");
        content.Should().Contain("AppEvent", "IntegrationEventScope must include AppEvent");
    }

    [Fact]
    public void TenantContextConsumeFilter_Exists()
    {
        var infraPath = Path.Combine(Path.GetDirectoryName(GetApplicationPath())!,
            "Notrelix.Infrastructure", "Messaging", "TenantContextConsumeFilter.cs");
        var content = File.ReadAllText(infraPath);

        content.Should().Contain("class TenantContextConsumeFilter",
            "TenantContextConsumeFilter must exist for consumer tenant isolation");
        content.Should().Contain("IFilter<ConsumeContext<T>>",
            "TenantContextConsumeFilter must implement MassTransit IFilter");
    }

    [Fact]
    public void MessagingRegistration_RegistersConsumeFilter()
    {
        var path = Path.Combine(Path.GetDirectoryName(GetApplicationPath())!,
            "Notrelix.Infrastructure", "DependencyInjection", "MessagingRegistration.cs");
        var content = File.ReadAllText(path);

        content.Should().Contain("UseConsumeFilter(typeof(TenantContextConsumeFilter<>),",
            "MessagingRegistration must register TenantContextConsumeFilter in MassTransit pipeline");
    }

    [Fact]
    public void PublicCacheableQueries_ShouldNotBeTenantOrPermissionScoped()
    {
        var files = GetApplicationFeatureFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (!content.Contains("IPublicCacheableQuery")) continue;

            if (content.Contains("IWorkspaceRequest")
                || content.Contains("IAccountRequest")
                || content.Contains("IResourceScopedRequest")
                || content.Contains("IRequirePermission")
                || content.Contains("ISystemInternalRequest"))
            {
                violations.Add(Path.GetFileName(file));
            }
        }

        violations.Should().BeEmpty(
            "Public cacheable queries must not be tenant or permission scoped: " +
            string.Join(", ", violations));
    }

    [Fact]
    public void AuthorizedCacheableRequests_ShouldNotExposeRawCacheKeyProperty()
    {
        var files = GetApplicationFeatureFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (!content.Contains("IAuthorizedCacheableRequest")) continue;

            if (content.Contains("string AuthorizedCacheKey") || content.Contains("string CacheKey"))
                violations.Add(Path.GetFileName(file));
        }

        violations.Should().BeEmpty(
            "Authorized cacheable requests must not expose raw cache key properties: " +
            string.Join(", ", violations));
    }

    [Fact]
    public void AuthorizedCacheableRequests_ShouldDeclareCacheIdentity()
    {
        var files = GetApplicationFeatureFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (!content.Contains("IAuthorizedCacheableRequest")) continue;

            if (!content.Contains("CacheIdentity"))
                violations.Add(Path.GetFileName(file));
        }

        violations.Should().BeEmpty(
            "All authorized cacheable requests must declare CacheIdentity: " +
            string.Join(", ", violations));
    }

    [Fact]
    public void WorkspaceAuthorizedCacheRequests_ShouldImplementWorkspaceOrResourceScope()
    {
        var files = GetApplicationFeatureFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (!content.Contains("IAuthorizedCacheableRequest")) continue;
            if (!content.Contains("CacheScope.Workspace") && !content.Contains("CacheScope.User"))
                continue;

            if (!content.Contains("IWorkspaceRequest") && !content.Contains("IResourceScopedRequest"))
                violations.Add(Path.GetFileName(file));
        }

        violations.Should().BeEmpty(
            "Workspace/User cache scope requests must also implement IWorkspaceRequest or IResourceScopedRequest: " +
            string.Join(", ", violations));
    }

    [Fact]
    public void PermissionedCacheScope_ShouldBeDisallowed()
    {
        var files = GetApplicationFeatureFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (!content.Contains("CacheScope.Permissioned") && !content.Contains("AuthorizedCacheScope.Permissioned"))
                continue;

            if (!content.Contains("IRequirePermission") && !content.Contains("IWorkspaceRequest") && !content.Contains("IResourceScopedRequest"))
                violations.Add(Path.GetFileName(file));
        }

        violations.Should().BeEmpty(
            "Permissioned cache scope queries must implement IRequirePermission and IResourceScopedRequest: " +
            string.Join(", ", violations));
    }

    [Fact]
    public void QueryTypes_ShouldNotReference_AuthorizedCacheKeyBuilder()
    {
        var files = GetApplicationFeatureFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (content.Contains("AuthorizedCacheKeyBuilder"))
                violations.Add(Path.GetFileName(file));
        }

        violations.Should().BeEmpty(
            "No files should reference deleted AuthorizedCacheKeyBuilder: " +
            string.Join(", ", violations));
    }

    [Fact]
    public void QueryTypes_ShouldNotConstructCacheKeyStrings()
    {
        var files = GetApplicationFeatureFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (!content.Contains("IAuthorizedCacheableRequest") && !content.Contains("IPublicCacheableQuery"))
                continue;

            if (content.Contains("CacheKey =>") || content.Contains("AuthorizedCacheKey =>")
                || content.Contains("=> $\"") || content.Contains("=> string."))
                violations.Add(Path.GetFileName(file));
        }

        violations.Should().BeEmpty(
            "Cacheable queries must not construct raw cache key strings: " +
            string.Join(", ", violations));
    }

    [Fact]
    public void CacheBehaviors_ShouldUse_CacheKeyFactory()
    {
        var behaviorsPath = Path.Combine(GetApplicationPath(), "Common", "Behaviors");
        var cacheBehaviors = new[] { "AuthorizedCacheBehavior.cs", "PublicCacheBehavior.cs" };

        var violations = new List<string>();
        foreach (var behavior in cacheBehaviors)
        {
            var fullPath = Path.Combine(behaviorsPath, behavior);
            if (!File.Exists(fullPath))
            {
                violations.Add($"{behavior} (not found)");
                continue;
            }

            var content = RemoveComments(File.ReadAllText(fullPath));
            if (!content.Contains("CacheKeyFactory"))
                violations.Add($"{behavior} (missing CacheKeyFactory dependency)");
        }

        violations.Should().BeEmpty(
            "Cache behaviors must use CacheKeyFactory: " + string.Join(", ", violations));
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
