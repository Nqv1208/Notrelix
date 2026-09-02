using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Common.Requests.Security;
using Notrelix.Application.Features.Identity.Auth.Commands.Login;
using Notrelix.Application.Features.Identity.Registration.Commands.Register;
using Notrelix.Application.Features.Identity.Registration.Commands.SendWelcomeEmail;
using Notrelix.Application.Features.Workspaces.Provisioning.Commands.ProvisionPersonalWorkspace;

namespace Notrelix.Architecture.Tests;

public class AuthPipelineArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void RegisterCommand_Must_Be_Anonymous_Global_Write()
    {
        var type = typeof(RegisterCommand);

        typeof(IAnonymousRequest).IsAssignableFrom(type).Should().BeTrue();
        typeof(IGlobalRequest).IsAssignableFrom(type).Should().BeTrue();
        typeof(IWriteRequest).IsAssignableFrom(type).Should().BeTrue();

        typeof(IRequirePermission).IsAssignableFrom(type).Should().BeFalse();
        typeof(IWorkspaceRequest).IsAssignableFrom(type).Should().BeFalse();
        typeof(IAccountRequest).IsAssignableFrom(type).Should().BeFalse();
        typeof(IResourceScopedRequest).IsAssignableFrom(type).Should().BeFalse();
    }

    [Fact]
    public void LoginCommand_Must_Be_Anonymous_Global_Write()
    {
        var type = typeof(LoginCommand);

        typeof(IAnonymousRequest).IsAssignableFrom(type).Should().BeTrue();
        typeof(IGlobalRequest).IsAssignableFrom(type).Should().BeTrue();
        typeof(IWriteRequest).IsAssignableFrom(type).Should().BeTrue();

        typeof(IRequirePermission).IsAssignableFrom(type).Should().BeFalse();
        typeof(IWorkspaceRequest).IsAssignableFrom(type).Should().BeFalse();
        typeof(IAccountRequest).IsAssignableFrom(type).Should().BeFalse();
        typeof(IResourceScopedRequest).IsAssignableFrom(type).Should().BeFalse();
    }
    [Fact]
    public void NoGuidEmptyActorFallback()
    {
        var content = File.ReadAllText(Path.Combine(GetApplicationPath(), "Common", "Behaviors", "ExecutionContextBehavior.cs"));
        content.Should().Contain("RequireUser", "ExecutionContextBehavior must fail closed for a missing actor");
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
    public void ConsumerTriggeredCommands_Must_Be_SystemInternal()
    {
        typeof(SendWelcomeEmailCommand).Should().Implement<ISystemInternalRequest>(
            "SendWelcomeEmailCommand is sent from MassTransit consumer (no HTTP context) and must bypass user auth via ISystemInternalRequest.");
        typeof(ProvisionPersonalWorkspaceCommand).Should().Implement<ISystemInternalRequest>(
            "ProvisionPersonalWorkspaceCommand is sent from MassTransit consumer (no HTTP context) and must bypass user auth via ISystemInternalRequest.");
    }

    [Fact]
    public void SystemInternalRequests_Must_Not_Be_Anonymous()
    {
        var systemInternalTypes = GetSystemInternalRequestTypes();

        foreach (var type in systemInternalTypes)
        {
            var isAnonymous = typeof(IAnonymousRequest).IsAssignableFrom(type);
            isAnonymous.Should().BeFalse(
                $"{type.Name} implements ISystemInternalRequest but must NOT also implement IAnonymousRequest. " +
                "System-internal and anonymous are distinct security categories.");
        }
    }

    [Fact]
    public void SystemInternalRequests_Must_Not_RequireUserPermission()
    {
        var systemInternalTypes = GetSystemInternalRequestTypes();

        foreach (var type in systemInternalTypes)
        {
            var requiresPermission = typeof(IRequirePermission).IsAssignableFrom(type);
            requiresPermission.Should().BeFalse(
                $"{type.Name} implements ISystemInternalRequest but must NOT also implement IRequirePermission. " +
                "System-internal requests bypass user auth and cannot require user-granted permissions.");
        }
    }

    [Fact]
    public void SystemInternalRequests_Must_Not_Be_Exposed_By_Api_Endpoints()
    {
        var systemInternalTypeNames = GetSystemInternalRequestTypes()
            .Select(t => t.Name)
            .ToHashSet();

        if (systemInternalTypeNames.Count == 0)
        {
            // No system-internal types found — nothing to check, skip
            return;
        }

        var endpointFiles = Directory.GetFiles(GetApiPath(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        var violations = new List<string>();

        foreach (var file in endpointFiles)
        {
            var content = RemoveComments(File.ReadAllText(file));
            foreach (var typeName in systemInternalTypeNames)
            {
                if (content.Contains(typeName, StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetFileName(file)} references {typeName} which implements ISystemInternalRequest. " +
                        "System-internal requests must not be exposed through HTTP endpoints.");
                }
            }
        }

        violations.Should().BeEmpty(
            "System-internal requests must not be sent from API endpoints. " +
            "Violations: " + string.Join(", ", violations));
    }

    private static Type[] GetSystemInternalRequestTypes()
    {
        var applicationAssembly = typeof(ISystemInternalRequest).Assembly;
        return applicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                     && typeof(ISystemInternalRequest).IsAssignableFrom(t))
            .ToArray();
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

    [Fact]
    public void AuthorizationPolicyEngine_MustNotReadPersistence()
    {
        // The pure AccessPolicyEngine owns authorization decisions. It must not
        // depend on any DbContext/persistence port — access facts are resolved
        // separately by IAccessFactsProvider, then evaluated with zero I/O.
        var policyPath = Path.Combine(GetApplicationPath(), "Common", "Security", "AccessPolicyEngine.cs");
        var content = RemoveComments(File.ReadAllText(policyPath));

        content.Should().NotContain("DbContext", "AccessPolicyEngine must be pure — no persistence access");
        content.Should().NotContain("IWorkManagementDbContext", "AccessPolicyEngine must not couple to persistence ports");
        content.Should().NotContain("IAccessFactsProvider", "AccessPolicyEngine consumes AccessFacts, never the facts provider");
    }

    [Fact]
    public void SharedAuthzSql_MustNotReadWorkManagementTablesDirectly()
    {
        // The neutral resource-authorization facts query must not couple the shared
        // authorization boundary to WorkManagement persistence. Board owner facts are
        // resolved through the WorkManagement-owned SPI (WG-WM-004), never via raw
        // work.boards / work.board_members SQL in the canonical AccessFactsQuery.
        var queryPath = Path.Combine(GetInfrastructurePath(), "Data", "Authz", "AccessFactsQuery.cs");
        var content = RemoveComments(File.ReadAllText(queryPath));

        content.Should().NotContain("work.boards", "shared authorization SQL must not read WorkManagement-owned work.boards");
        content.Should().NotContain("work.board_members", "shared authorization SQL must not read WorkManagement-owned work.board_members");
    }

    [Fact]
    public void ResourceAuthorizationSpi_MustRemainTransportAndPersistenceNeutral()
    {
        // The neutral SPI (WG-WM-004) is a work-management-owned facts boundary. It must
        // expose only resource-owned facts and must not leak EF/HTTP/gRPC/broker/policy
        // concepts into the shared application contract.
        var spiPath = Path.Combine(GetApplicationPath(), "Common", "Security", "IResourceAuthorizationFactsProvider.cs");
        var content = RemoveComments(File.ReadAllText(spiPath));

        content.Should().NotContain("DbContext", "the neutral resource-authorization SPI must not reference persistence");
        content.Should().NotContain("Npgsql", "the neutral resource-authorization SPI must not reference Npgsql");
        content.Should().NotContain("HttpClient", "the neutral resource-authorization SPI must not reference HTTP");
        content.Should().NotContain("Grpc", "the neutral resource-authorization SPI must not reference gRPC");
        content.Should().NotContain("IBus", "the neutral resource-authorization SPI must not reference the message broker");
        content.Should().NotContain("AccessDecision", "the SPI must expose facts, never an authorization decision");
    }

    [Fact]
    public void WorkManagementFactsAdapter_MustOwnTheWorkManagementDbContext()
    {
        // The WorkManagement-owned facts adapter (WG-WM-004) is the only place that reads
        // WorkManagement persistence to resolve resource-owner facts for the board handshake.
        // It must therefore own IWorkManagementDbContext, not a generic Application context.
        var adapterPath = Path.Combine(
            GetInfrastructurePath(), "Data", "ReadPorts", "WorkManagement", "WorkManagementResourceAuthorizationFactsProvider.cs");
        var content = RemoveComments(File.ReadAllText(adapterPath));

        content.Should().Contain("IWorkManagementDbContext", "the WorkManagement facts adapter must own IWorkManagementDbContext");
        content.Should().Contain("IResourceAuthorizationFactsProvider", "the WorkManagement facts adapter must implement the neutral SPI");
    }

    [Fact]
    public void PostgresAccessFactsProvider_MustNotEmitPolicyDecisions()
    {
        // The facts provider composes neutral resource-owner facts onto the tenant/account/
        // Governance query snapshot and returns AccessFacts. It must not decide policy; that
        // authority stays with AccessPolicyEngine.
        var providerPath = Path.Combine(GetInfrastructurePath(), "Data", "Authz", "PostgresAccessFactsProvider.cs");
        var content = RemoveComments(File.ReadAllText(providerPath));

        content.Should().NotContain("Allow", "the facts provider resolves facts; the policy engine decides Allow");
        content.Should().NotContain("Deny", "the facts provider resolves facts; the policy engine decides Deny");
    }
}
