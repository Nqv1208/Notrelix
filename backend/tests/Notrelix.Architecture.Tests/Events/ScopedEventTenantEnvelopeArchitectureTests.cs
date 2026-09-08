using System.Reflection;
using Notrelix.Application.Common.Messaging;
using Notrelix.Infrastructure.Messaging;

namespace Notrelix.Architecture.Tests.Events;

/// <summary>
/// TAC-GATE-023 — scoped integration-event tenant envelope (TAC-FRZ-018).
///
/// Workspace-scoped business events must carry both authoritative AccountId and
/// WorkspaceId. Account-scoped events must carry AccountId. Global/System events
/// are allowed only by explicit semantic classification. Producer mappings must
/// not silently drop the account envelope to null.
/// </summary>
public class ScopedEventTenantEnvelopeArchitectureTests : ArchitectureTestBase
{
    private static readonly IReadOnlyList<ContractDefinition> Contracts =
        ContractRegistrySetup.GetContractDefinitions();

    [Fact]
    public void EveryIntegrationEvent_HasExplicitTenantScopeClassification()
    {
        var violations = Contracts
            .Select(c => c.IntegrationEventType)
            .Where(t => t.GetCustomAttribute<IntegrationEventTenantScopeAttribute>() is null)
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        violations.Should().BeEmpty(
            "TAC-FRZ-018: every public integration event must declare Workspace, Account, or None tenant scope. " +
            "Violations: " + string.Join("; ", violations));
    }

    [Fact]
    public void WorkspaceScopedEvents_CarryAuthoritativeAccountAndWorkspaceEnvelope()
    {
        var violations = Contracts
            .Select(c => c.IntegrationEventType)
            .Where(t => Scope(t) == IntegrationEventTenantScope.Workspace)
            .Select(t => ValidateWorkspaceEnvelope(t))
            .Where(v => v is not null)
            .Cast<string>()
            .ToList();

        violations.Should().BeEmpty(
            "TAC-FRZ-018: workspace-scoped events need non-null AccountId and WorkspaceId envelope fields. " +
            "Violations: " + string.Join("; ", violations));
    }

    [Fact]
    public void AccountScopedEvents_CarryAuthoritativeAccountEnvelope()
    {
        var violations = Contracts
            .Select(c => c.IntegrationEventType)
            .Where(t => Scope(t) == IntegrationEventTenantScope.Account)
            .Select(t => ValidateAccountEnvelope(t))
            .Where(v => v is not null)
            .Cast<string>()
            .ToList();

        violations.Should().BeEmpty(
            "TAC-FRZ-018: account-scoped events need an AccountId envelope field. " +
            "Violations: " + string.Join("; ", violations));
    }

    [Fact]
    public void ProducerMappings_DoNotDropScopedEventAccountEnvelope()
    {
        var scopedTypes = Contracts
            .Select(c => c.IntegrationEventType)
            .Where(t => Scope(t) is IntegrationEventTenantScope.Workspace or IntegrationEventTenantScope.Account)
            .ToList();

        var violations = new List<string>();

        foreach (var file in Directory.GetFiles(Path.Combine(GetApplicationPath(), "EventMappers"), "*.cs", SearchOption.AllDirectories))
        {
            var source = RemoveComments(File.ReadAllText(file));

            foreach (var type in scopedTypes)
            {
                foreach (var args in FindConstructorArguments(source, type.Name))
                {
                    if (args.Contains("AccountId: null", StringComparison.Ordinal) ||
                        args.Contains("accountId: null", StringComparison.Ordinal))
                    {
                        violations.Add($"{Path.GetFileName(file)}: {type.Name} maps AccountId to null");
                        continue;
                    }

                    if (!args.Contains("AccountId:", StringComparison.OrdinalIgnoreCase) &&
                        !args.Contains("AccountIdValue", StringComparison.Ordinal) &&
                        !args.Contains("domainEvent.AccountId", StringComparison.Ordinal))
                    {
                        violations.Add($"{Path.GetFileName(file)}: {type.Name} construction does not preserve domainEvent.AccountId");
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            "TAC-FRZ-018: producer mappers must preserve the authoritative account envelope. " +
            "Violations: " + string.Join("; ", violations));
    }

    [Fact]
    public void Gate_Detects_WorkspaceEvent_WithoutAccountId()
    {
        ValidateWorkspaceEnvelope(typeof(TestWorkspaceEventWithoutAccount)).Should().NotBeNull();
    }

    [Fact]
    public void Gate_Detects_AccountEvent_WithoutAccountId()
    {
        ValidateAccountEnvelope(typeof(TestAccountEventWithoutAccount)).Should().NotBeNull();
    }

    [Fact]
    public void Gate_Allows_GlobalEvent_WithoutTenantEnvelope()
    {
        Scope(typeof(TestGlobalEvent)).Should().Be(IntegrationEventTenantScope.None);
        ValidateAccountEnvelope(typeof(TestGlobalEvent)).Should().BeNull();
    }

    [Fact]
    public void RegistrationEvents_HaveCorrectSemanticClassification()
    {
        Scope(typeof(Notrelix.Application.Events.Identity.UserRegisteredIntegrationEvent))
            .Should().Be(IntegrationEventTenantScope.None);
        Scope(typeof(Notrelix.Application.Events.Identity.UserDeactivatedIntegrationEvent))
            .Should().Be(IntegrationEventTenantScope.None);
        Scope(typeof(Notrelix.Application.Events.Identity.IdentityRegistrationCompletedIntegrationEventV1))
            .Should().Be(IntegrationEventTenantScope.Account);
    }

    [Fact]
    public void UserRegisteredConsumer_DoesNotOwnRegistrationWorkflow()
    {
        var source = RemoveComments(File.ReadAllText(Path.Combine(
            GetInfrastructurePath(), "Messaging", "Consumers", "Identity", "UserRegistered", "UserRegisteredConsumer.cs")));

        source.Should().NotContain("ProvisionPersonalWorkspaceCommand",
            "TAC-FRZ-018: workspace provisioning belongs to IdentityRegistrationCompleted");
        source.Should().NotContain("QueueRenderedEmailRequest",
            "TAC-FRZ-018: welcome delivery belongs to IdentityRegistrationCompleted");
        source.Should().NotContain("ISender",
            "TAC-FRZ-018: UserRegistered is a non-tenant telemetry consumer");
    }

    [Fact]
    public void Gate_Detects_WorkspaceEvent_WithOptionalAccountId()
    {
        ValidateWorkspaceEnvelope(typeof(TestWorkspaceEventWithOptionalAccount))
            .Should().NotBeNull();
    }

    [Fact]
    public void Gate_Detects_WorkspaceEvent_WithOptionalWorkspaceId()
    {
        ValidateWorkspaceEnvelope(typeof(TestWorkspaceEventWithOptionalWorkspace))
            .Should().NotBeNull();
    }

    [Fact]
    public void Gate_Detects_AccountEvent_WithOptionalAccountId()
    {
        ValidateAccountEnvelope(typeof(TestAccountEventWithOptionalAccount))
            .Should().NotBeNull();
    }

    private static IntegrationEventTenantScope? Scope(Type type)
        => type.GetCustomAttribute<IntegrationEventTenantScopeAttribute>()?.Scope;

    private static string? ValidateWorkspaceEnvelope(Type type)
    {
        if (Scope(type) != IntegrationEventTenantScope.Workspace)
            return null;

        var accountId = AccountIdParameter(type);
        var workspaceId = WorkspaceIdParameter(type);

        if (accountId is null)
            return $"{type.FullName}: missing AccountId envelope parameter";

        if (workspaceId is null)
            return $"{type.FullName}: missing WorkspaceId envelope parameter";

        if (accountId.HasDefaultValue)
            return $"{type.FullName}: Workspace-scoped AccountId must be required, not optional";

        if (workspaceId.HasDefaultValue)
            return $"{type.FullName}: Workspace-scoped WorkspaceId must be required, not optional";

        return null;
    }

    private static string? ValidateAccountEnvelope(Type type)
    {
        if (Scope(type) != IntegrationEventTenantScope.Account)
            return null;

        var accountId = AccountIdParameter(type);

        if (accountId is null)
            return $"{type.FullName}: missing AccountId envelope parameter";

        if (accountId.HasDefaultValue)
            return $"{type.FullName}: Account-scoped AccountId must be required, not optional";

        return null;
    }

    private static ParameterInfo? AccountIdParameter(Type type)
    {
        var parameters = PrimaryConstructor(type)?.GetParameters() ?? [];
        return parameters.FirstOrDefault(p =>
            p.Name is "AccountId" or "AccountIdValue" ||
            (p.Name?.Contains("AccountId", StringComparison.OrdinalIgnoreCase) ?? false));
    }

    private static ParameterInfo? WorkspaceIdParameter(Type type)
    {
        var parameters = PrimaryConstructor(type)?.GetParameters() ?? [];
        return parameters.FirstOrDefault(p =>
            p.Name is "WorkspaceId" or "WorkspaceIdValue" ||
            (p.Name?.Contains("WorkspaceId", StringComparison.OrdinalIgnoreCase) ?? false));
    }

    private static ConstructorInfo? PrimaryConstructor(Type type)
        => type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

    private static IEnumerable<string> FindConstructorArguments(string source, string typeName)
    {
        var search = $"new {typeName}(";
        var index = 0;

        while ((index = source.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
        {
            var start = index + search.Length;
            var depth = 1;
            var i = start;

            while (i < source.Length && depth > 0)
            {
                if (source[i] == '(')
                    depth++;
                else if (source[i] == ')')
                    depth--;

                if (depth > 0)
                    i++;
            }

            yield return source[start..i];
            index = i;
        }
    }

    [IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
    private sealed record TestWorkspaceEventWithoutAccount(
        Guid EventId,
        Guid? WorkspaceId,
        Guid CorrelationId);

    [IntegrationEventTenantScope(IntegrationEventTenantScope.Account)]
    private sealed record TestAccountEventWithoutAccount(
        Guid EventId,
        Guid CorrelationId);

    [IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
    private sealed record TestWorkspaceEventWithOptionalAccount(
        Guid EventId,
        Guid? WorkspaceId,
        Guid CorrelationId,
        Guid? AccountId = null);

    [IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
    private sealed record TestWorkspaceEventWithOptionalWorkspace(
        Guid EventId,
        Guid? AccountId,
        Guid CorrelationId,
        Guid? WorkspaceId = null);

    [IntegrationEventTenantScope(IntegrationEventTenantScope.Account)]
    private sealed record TestAccountEventWithOptionalAccount(
        Guid EventId,
        Guid CorrelationId,
        Guid? AccountId = null);

    [IntegrationEventTenantScope(IntegrationEventTenantScope.None)]
    private sealed record TestGlobalEvent(
        Guid EventId,
        Guid CorrelationId);
}
