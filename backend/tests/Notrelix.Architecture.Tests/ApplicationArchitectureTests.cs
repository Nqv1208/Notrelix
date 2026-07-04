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

        lines.Should().HaveCount(13, "expected exactly 13 pipeline behaviors");

        var expectedOrder = new[]
        {
            "ExceptionMappingBehavior",
            "ApplicationTracingBehavior",
            "ValidationBehavior",
            "TenantBootstrapBehavior",
            "PostCommitScopeBehavior",
            "PublicCacheBehavior",
            "DbRequestScopeBehavior",
            "AuthorizationBehavior",
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

        var allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CreateBoardField.cs",
            "UpdateBoardField.cs",
            "ReorderBoardFields.cs",
            "DeleteBoardField.cs",
            "UpdateMemberRole.cs",
            "RemoveMember.cs",
            "AddBoardMember.cs",
            "RemoveBoardMember.cs",
            "CreateAutomationRule.cs",
            "SetAutomationRuleEnabled.cs",
            "GetWorkspaceAutomations.cs",
            "GetAutomationExecutions.cs",
            "UnarchiveBoard.cs",
            "ArchiveBoardGroup.cs",
            "ReorderBoardGroups.cs",
            "UnarchiveBoardGroup.cs",
            "CreateBoardGroup.cs",
            "UpdateBoardGroup.cs",
            "UpdateBoardItemFieldValues.cs",
            "SetBoardItemDueDate.cs",
            "UpdateBoardItemStatus.cs",
            "UpdateBoardItem.cs",
            "LinkPageToBoardItem.cs",
            "ArchiveBoardItem.cs",
            "UnlinkPageFromBoardItem.cs",
            "WorkspacePermissionService.cs",
        };

        foreach (var file in handlerFiles)
        {
            var fileName = Path.GetFileName(file);
            if (allowedFiles.Contains(fileName)) continue;

            var content = RemoveComments(File.ReadAllText(file));
            if (content.Contains("WorkspacePermissionService") || content.Contains("IWorkspacePermissionService"))
                violations.Add(fileName);
        }

        violations.Should().BeEmpty("No new handlers should inject IWorkspacePermissionService. Use pipeline authorization (IRequirePermission) instead. Violations: " + string.Join(", ", violations));
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

        content.Should().Contain("RateLimitingMiddleware", "Rate limiting middleware must be registered in the API pipeline");
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

    private static string RemoveComments(string input)
    {
        var blockComments = @"/\*(.*?)\*/";
        var lineComments = @"//(.*?)\r?\n";
        var cleaned = Regex.Replace(input, blockComments, "", RegexOptions.Singleline);
        cleaned = Regex.Replace(cleaned, lineComments, "\n");
        return cleaned;
    }
}
