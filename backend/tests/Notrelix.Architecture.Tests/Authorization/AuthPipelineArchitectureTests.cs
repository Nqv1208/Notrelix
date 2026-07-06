using Notrelix.Application.Common.CQRS;
using Notrelix.Application.Common.CQRS.Scoping;
using Notrelix.Application.Common.CQRS.Security;
using Notrelix.Application.Features.Identity.Auth.Commands.Login;
using Notrelix.Application.Features.Identity.Registration.Commands.Register;

namespace Notrelix.Architecture.Tests;

public class AuthPipelineArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void RegisterCommand_Must_Be_Anonymous_Global_Transactional()
    {
        var type = typeof(RegisterCommand);

        typeof(IAnonymousRequest).IsAssignableFrom(type).Should().BeTrue();
        typeof(IGlobalRequest).IsAssignableFrom(type).Should().BeTrue();
        typeof(ITransactionalRequest).IsAssignableFrom(type).Should().BeTrue();

        typeof(IRequirePermission).IsAssignableFrom(type).Should().BeFalse();
        typeof(IWorkspaceRequest).IsAssignableFrom(type).Should().BeFalse();
        typeof(IAccountRequest).IsAssignableFrom(type).Should().BeFalse();
        typeof(IResourceScopedRequest).IsAssignableFrom(type).Should().BeFalse();
    }

    [Fact]
    public void LoginCommand_Must_Be_Anonymous_Global_Transactional()
    {
        var type = typeof(LoginCommand);

        typeof(IAnonymousRequest).IsAssignableFrom(type).Should().BeTrue();
        typeof(IGlobalRequest).IsAssignableFrom(type).Should().BeTrue();
        typeof(ITransactionalRequest).IsAssignableFrom(type).Should().BeTrue();

        typeof(IRequirePermission).IsAssignableFrom(type).Should().BeFalse();
        typeof(IWorkspaceRequest).IsAssignableFrom(type).Should().BeFalse();
        typeof(IAccountRequest).IsAssignableFrom(type).Should().BeFalse();
        typeof(IResourceScopedRequest).IsAssignableFrom(type).Should().BeFalse();
    }
    [Fact]
    public void NoGuidEmptyActorFallback()
    {
        var content = File.ReadAllText(Path.Combine(GetApplicationPath(), "Common", "Behaviors", "ResourceScopeBehavior.cs"));
        content.Should().NotContain("Guid.Empty", "ResourceScopeBehavior must not fallback to Guid.Empty for missing actor");
    }

    [Fact]
    public void AllScopedRequestsHavePermissionOrSystemInternalMarker()
    {
        var files = GetApplicationFeatureFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (!content.Contains("IResourceScopedRequest")) continue;

            if (!content.Contains("IRequirePermission") && !content.Contains("ISystemInternalRequest"))
                violations.Add(Path.GetFileName(file));
        }

        violations.Should().BeEmpty(
            "All IResourceScopedRequest implementations must also implement IRequirePermission or ISystemInternalRequest. " +
            "Violations: " + string.Join(", ", violations));
    }

    [Fact]
    public void AllScopedRequestsHaveConsistentResourceAcrossInterfaces()
    {
        var files = GetApplicationFeatureFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            if (!content.Contains("IResourceScopedRequest") || !content.Contains("IRequirePermission")) continue;

            var source = File.ReadAllText(file);
            var matches = System.Text.RegularExpressions.Regex.Matches(
                source,
                @"(?:IResourceScopedRequest\.Resource|IRequirePermission\.Resource)\s*=>\s*ResourceRef\.Create\(([^)]+)\)");

            if (matches.Count <= 1) continue;

            var callSignatures = matches
                .Select(m => m.Groups[1].Value.Trim())
                .Distinct()
                .ToList();

            if (callSignatures.Count > 1)
            {
                violations.Add($"{Path.GetFileName(file)}: multiple distinct ResourceRef.Create calls ({string.Join(" | ", callSignatures)})");
            }
        }

        violations.Should().BeEmpty(
            "All classes implementing both IResourceScopedRequest and IRequirePermission must return the same ResourceRef from both interfaces. " +
            "Violations: " + string.Join(", ", violations));
    }

    [Fact]
    public void GovernanceCommands_NoWorkspaceId()
    {
        var files = Directory.GetFiles(Path.Combine(GetApplicationPath(), "Features", "Governance"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (!fileName.EndsWith("Command.cs") && !fileName.EndsWith("Query.cs")) continue;

            var content = File.ReadAllText(file);
            if (!content.Contains("IResourceScopedRequest")) continue;

            content.Should().NotContain("Guid WorkspaceId,",
                $"{fileName} implements IResourceScopedRequest but still declares WorkspaceId parameter");
        }
    }
}
