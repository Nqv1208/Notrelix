namespace Notrelix.Architecture.Tests;

/// <summary>
/// Enforces handler injection rules:
/// - No IApplicationDbContext in handlers
/// - No ApplicationDbContext in handlers
/// - No Guid.Empty accountId in factory calls
/// </summary>
public class HandlerBoundaryArchitectureTests
{
    private static readonly string FeaturesPath = Path.Combine(
        FindProjectRoot(), "src", "Notrelix.Application", "Features");

    [Fact]
    public void Handlers_ShouldNotInject_IApplicationDbContext()
    {
        var handlerFiles = Directory.GetFiles(FeaturesPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in handlerFiles)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(FindProjectRoot(), file);

            // Skip DTOs, validators, commands, queries — only check handlers
            if (!relativePath.Contains("Handler") && !content.Contains("IRequestHandler"))
                continue;

            content.Should().NotContain("IApplicationDbContext _",
                $"Handler {relativePath} must not inject IApplicationDbContext");
        }
    }

    [Fact]
    public void Handlers_ShouldNotInject_ApplicationDbContext()
    {
        var handlerFiles = Directory.GetFiles(FeaturesPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in handlerFiles)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(FindProjectRoot(), file);

            if (!relativePath.Contains("Handler") && !content.Contains("IRequestHandler"))
                continue;

            content.Should().NotContain("ApplicationDbContext _",
                $"Handler {relativePath} must not inject ApplicationDbContext");
        }
    }

    [Fact]
    public void InlineDomainEventHandlers_MustNotDoIO()
    {
        var featureFiles = Directory.GetFiles(FeaturesPath, "*.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (var file in featureFiles)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(FindProjectRoot(), file);

            if (!content.Contains("INotificationHandler<DomainEventNotification<"))
                continue;

            // Inline handlers must not inject IO services
            var ioServicePatterns = new[]
            {
                "IApplicationDbContext", "IAutomationDbContext",
                "DbContext", "IJobQueue",
                "IEmailService", "IHttpClientFactory",
                "HttpClient", "ISender"
            };

            foreach (var pattern in ioServicePatterns)
            {
                if (content.Contains(pattern))
                {
                    violations.Add($"{relativePath}: injects {pattern}");
                }
            }
        }

        violations.Should().BeEmpty(
            "Inline domain event handlers must not perform IO. Use post-commit actions or outbox instead.");
    }

    private static readonly HashSet<string> AllowlistedHandlersWithTenantContext = new()
    {
        "src/Notrelix.Application/Features/Collaboration/Comments/Commands/CreateComment/CreateComment.cs",
        "src/Notrelix.Application/Features/Collaboration/Attachments/Commands/CreateBoardItemAttachment/CreateBoardItemAttachment.cs",
        "src/Notrelix.Application/Features/WorkManagement/BoardGroups/Commands/DuplicateBoardGroup/DuplicateBoardGroup.cs",
        "src/Notrelix.Application/Features/WorkManagement/BoardGroups/Commands/CreateBoardGroup/CreateBoardGroup.cs",
        "src/Notrelix.Application/Features/WorkManagement/Checklists/Commands/CreateChecklist/CreateChecklist.cs",
        "src/Notrelix.Application/Features/WorkManagement/BoardViews/Commands/CreateBoardView/CreateBoardView.cs",
        "src/Notrelix.Application/Features/WorkManagement/BoardViews/Commands/SaveBoardView/SaveBoardView.cs",
        "src/Notrelix.Application/Features/WorkManagement/Labels/Commands/CreateLabel/CreateLabel.cs",
        "src/Notrelix.Application/Features/WorkManagement/Labels/Commands/AddLabelToBoardItem/AddLabelToBoardItem.cs",
        "src/Notrelix.Application/Features/WorkManagement/BoardItems/Commands/DuplicateBoardItem/DuplicateBoardItem.cs",
        "src/Notrelix.Application/Features/WorkManagement/BoardItems/Commands/CreateBoardItem/CreateBoardItem.cs",
        "src/Notrelix.Application/Features/WorkManagement/BoardItems/Commands/LinkPageToBoardItem/LinkPageToBoardItem.cs",
        "src/Notrelix.Application/Features/WorkManagement/BoardItems/Commands/AssignBoardItemMember/AssignBoardItemMember.cs",
        "src/Notrelix.Application/Features/WorkManagement/BoardItems/Commands/UpdateBoardItemFieldValues/UpdateBoardItemFieldValues.cs",
        "src/Notrelix.Application/Features/WorkManagement/Boards/Commands/CreateBoardBySlug/CreateBoardBySlug.cs",
        "src/Notrelix.Application/Features/WorkManagement/Boards/Commands/CreateBoardInWorkspace/CreateBoardInWorkspace.cs",
        "src/Notrelix.Application/Features/WorkManagement/BoardFields/Commands/CreateBoardField/CreateBoardField.cs",
        "src/Notrelix.Application/Features/Workspaces/Workspaces/Commands/CreateWorkspace/CreateWorkspace.cs",
        "src/Notrelix.Application/Features/Workspaces/Invitations/Commands/AcceptInvitation/AcceptInvitation.cs",
        "src/Notrelix.Application/Features/Governance/ShareLinks/Commands/CreateShareLink/CreateShareLinkCommand.cs",
        "src/Notrelix.Application/Features/Governance/ResourcePermissions/Queries/GetResourcePermissions/GetResourcePermissionsQuery.cs",
        "src/Notrelix.Application/Features/Governance/ResourcePermissions/Commands/GrantResourcePermission/GrantResourcePermissionCommand.cs",
        "src/Notrelix.Application/Features/Governance/ResourcePermissions/Commands/RevokeResourcePermission/RevokeResourcePermissionCommand.cs",
        "src/Notrelix.Application/Features/Automation/Rules/Commands/CreateAutomationRule/CreateAutomationRule.cs",
        "src/Notrelix.Application/Features/Documents/Blocks/Commands/CreateBlock/CreateBlock.cs",
        "src/Notrelix.Application/Features/Documents/Pages/Commands/CreatePage/CreatePage.cs",
    };

    [Fact]
    public void Handlers_ShouldNotInject_ICurrentTenantContext()
    {
        var handlerFiles = Directory.GetFiles(FeaturesPath, "*.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (var file in handlerFiles)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(FindProjectRoot(), file);

            if (!relativePath.Contains("Handler") && !content.Contains("IRequestHandler"))
                continue;

            if (content.Contains("ICurrentTenantContext") && !AllowlistedHandlersWithTenantContext.Contains(relativePath))
                violations.Add(relativePath);
        }

        violations.Should().BeEmpty(
            $"Handlers must not inject ICurrentTenantContext directly. Use ICurrentUser for user identity. " +
            $"Add new handlers to AllowlistedHandlersWithTenantContext only if absolutely necessary. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Handlers_ShouldNotUse_AccountIdEmpty_ForFactoryCalls()
    {
        var handlerFiles = Directory.GetFiles(FeaturesPath, "*.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (var file in handlerFiles)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(FindProjectRoot(), file);

            if (!relativePath.Contains("Handler") && !content.Contains("IRequestHandler"))
                continue;

            // Check for Guid.Empty used as accountId in factory Create calls
            // Pattern: .Create(Guid.Empty, ... or Create(\n            Guid.Empty,
            if (content.Contains(".Create(Guid.Empty,") ||
                content.Contains("Create(\n            Guid.Empty,"))
            {
                violations.Add(relativePath);
            }
        }

        violations.Should().BeEmpty(
            "Handlers must use _tenant.RequireAccountId(), not Guid.Empty, for factory calls");
    }

    private static string FindProjectRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null && !File.Exists(Path.Combine(dir, "backend.slnx")))
            dir = Directory.GetParent(dir)?.FullName;
        return dir ?? throw new InvalidOperationException("Could not find project root");
    }
}