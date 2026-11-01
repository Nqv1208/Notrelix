using Notrelix.Application.Events.Governance;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.Authorization;
using Notrelix.Domain.Governance;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Features.Governance.Roles;

public sealed record CustomRoleDto(
    Guid Id,
    string Name,
    string? Description,
    string BaseRole,
    string Status,
    IReadOnlyList<string> AddedActions,
    IReadOnlyList<string> RemovedActions,
    long Version,
    int MemberCount);

public sealed record CustomRoleListDto(IReadOnlyList<CustomRoleDto> Roles);

/// <summary>A built-in or custom role as the workspace Roles page lists it.</summary>
public sealed record WorkspaceRoleDto(
    string Kind,
    Guid? Id,
    string Key,
    string Name,
    string? Description,
    string? BaseRole,
    string Status,
    IReadOnlyList<string> AddedActions,
    IReadOnlyList<string> RemovedActions,
    int MemberCount);

public sealed record WorkspaceRoleListDto(IReadOnlyList<WorkspaceRoleDto> Roles);

public sealed record PermissionCatalogDto(IReadOnlyList<PermissionCatalogEntry> Actions);

public sealed record MyEffectiveAccessDto(
    Guid? CustomRoleId,
    string? CustomRoleName,
    string Role,
    IReadOnlyList<string> AddedActions,
    IReadOnlyList<string> RemovedActions);

internal static class CustomRoleRank
{
    /// <summary>Guest below Member below Admin; an archive moves people only to an equal or lower base.</summary>
    public static int Of(CustomRoleBase role) => role switch
    {
        CustomRoleBase.Guest => 1,
        CustomRoleBase.Member => 2,
        _ => 3,
    };
}

internal static class CustomRoleRules
{
    public static CustomRoleBase ParseBase(string? value)
    {
        if (!Enum.TryParse<CustomRoleBase>(value?.Trim(), ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
        {
            throw new DomainBusinessRuleException(GovernanceRuleCodes.Governance_Role_BaseInvalid, "A role is based on Admin, Member or Guest.");
        }

        return parsed;
    }

    /// <summary>
    /// What a role may add and remove, judged against its fixed base and the product catalog
    /// (owner decision 2026-10-31): adds are workspace-level actions the base lacks, never Owner-locked
    /// and never on a Guest base (Guest-capped); removals are actions the base holds and the catalog lets go.
    /// </summary>
    public static void ValidateActions(CustomRoleBase baseRole, IReadOnlyCollection<string> added, IReadOnlyCollection<string> removed)
    {
        foreach (var name in added)
        {
            var entry = Find(name);
            if (entry.Locked)
                throw new DomainBusinessRuleException(GovernanceRuleCodes.Governance_Role_ActionLocked, $"'{name}' stays with the workspace Owner.");
            if (baseRole == CustomRoleBase.Guest)
                throw new DomainBusinessRuleException(GovernanceRuleCodes.Governance_Role_GuestCannotAdd, "A Guest-based role cannot add actions.");
            if (!entry.CanAdd)
                throw new DomainBusinessRuleException(GovernanceRuleCodes.Governance_Role_ActionNotAddable, $"'{name}' is decided on the board or page and can only be removed.");
            if (Holds(baseRole, name))
                throw new DomainBusinessRuleException(GovernanceRuleCodes.Governance_Role_ActionInBase, $"The {baseRole} base already holds '{name}'.");
        }

        foreach (var name in removed)
        {
            var entry = Find(name);
            if (entry.Locked || !entry.CanRemove)
                throw new DomainBusinessRuleException(GovernanceRuleCodes.Governance_Role_ActionNotRemovable, $"'{name}' cannot be removed by a role.");
            if (!Holds(baseRole, name))
                throw new DomainBusinessRuleException(GovernanceRuleCodes.Governance_Role_ActionNotInBase, $"The {baseRole} base does not hold '{name}'.");
        }
    }

    private static PermissionCatalogEntry Find(string name) =>
        PermissionCatalog.TryGet(name.Trim(), out var entry)
            ? entry
            : throw new DomainBusinessRuleException(GovernanceRuleCodes.Governance_Role_ActionUnknown, $"'{name}' is not a permission of the catalog.");

    private static bool Holds(CustomRoleBase baseRole, string name) =>
        Enum.TryParse<PermissionAction>(name.Trim(), out var action)
        && WorkspaceRoleBaseline.Decide(baseRole.ToString(), action) != BaselineDecision.Deny;

    public static CustomRoleDto ToDto(CustomRole role, int memberCount) => new(
        role.Id, role.Name, role.Description, role.BaseRole.ToString(), role.Status.ToString(),
        role.AddedActions.ToArray(), role.RemovedActions.ToArray(), role.Version, memberCount);

    /// <summary>Custom roles are an Enterprise capability; a plan without it freezes them (still effective, not editable).</summary>
    public static async Task EnsurePlanAsync(IBillingCapabilityFacts billing, Guid accountId, CancellationToken cancellationToken)
    {
        var fact = await billing.GetCapabilityAsync(accountId, Guid.Empty, BillingCapabilityCode.CustomRoles, 0, cancellationToken);
        if (fact is not { IsAvailable: true })
        {
            throw new ForbiddenException("Custom roles are part of the Enterprise plan.", Common.Security.AccessDenialCodes.FeatureNotEnabled);
        }
    }

    public static void EnsureVersion(CustomRole role, long expected)
    {
        if (role.Version != expected)
        {
            throw new PreconditionFailedException(
                "The role changed since you loaded it. Reload and try again.",
                expectedVersion: expected,
                currentVersion: role.Version);
        }
    }
}

/// <summary>
/// Writes the audit fact (<see cref="CustomRoleChange"/>), the outbox event and, when a workspace is involved,
/// the audit log line of one role change. Everything joins the request's unit of work, so a rolled-back
/// change leaves none of them.
/// </summary>
public sealed class CustomRoleChangeRecorder
{
    private readonly IGovernanceDbContext _context;
    private readonly ICurrentRequestContext _request;
    private readonly IDateTimeProvider _clock;
    private readonly IIntegrationEventCollector _events;
    private readonly IAuditService _audit;

    public CustomRoleChangeRecorder(
        IGovernanceDbContext context,
        ICurrentRequestContext request,
        IDateTimeProvider clock,
        IIntegrationEventCollector events,
        IAuditService audit)
    {
        _context = context;
        _request = request;
        _clock = clock;
        _events = events;
        _audit = audit;
    }

    public async Task RecordAsync(
        string kind,
        CustomRole role,
        Guid? workspaceId,
        Guid? subjectUserId,
        string summary,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var accountId = _request.RequireAccountId();
        _context.CustomRoleChanges.Add(CustomRoleChange.Record(accountId, workspaceId, role.Id, kind, _request.UserId, subjectUserId, summary, now));

        _events.Add(new CustomRoleChangedIntegrationEvent(
            Guid.CreateVersion7(), accountId, workspaceId, role.Id, role.Name, kind, subjectUserId,
            CorrelationId: Guid.CreateVersion7(), ActorUserId: _request.UserId, OccurredAt: now));

        if (workspaceId is { } workspace)
        {
            await _audit.RecordAsync(workspace, _request.UserId, "CustomRole." + kind,
                ResourceRef.Create(ResourceKind.Create("governance.custom-role"), role.Id),
                AuditMetadata.Create(), AuditSeverity.Info, cancellationToken: cancellationToken);
        }
    }
}
