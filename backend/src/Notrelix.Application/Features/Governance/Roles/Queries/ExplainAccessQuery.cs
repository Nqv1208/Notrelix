using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Common.Context;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.Authorization;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Governance.Roles.Queries;

public sealed record ExplainStepDto(int Order, string Code, string Outcome, string Detail);

public sealed record AccessExplanationDto(
    Guid UserId,
    string Action,
    string? ResourceKind,
    Guid? ResourceId,
    bool Allowed,
    string Decision,
    string StoredRole,
    string EffectiveRole,
    string? CustomRoleName,
    IReadOnlyList<string> Sources,
    IReadOnlyList<ExplainStepDto> Steps);

/// <summary>
/// "Why can this person do this?" (capability E): the decision for one person, one workspace, one action and an
/// optional resource, with the sources behind it and the steps in the order the engine reads them. The decision is
/// the engine's own result over the same facts a real request would use; the steps explain it and never decide.
/// Open to whoever may read the audit trail (Owner and Admin by default); every check is audited.
/// </summary>
public sealed record ExplainAccessQuery(Guid WorkspaceId, Guid UserId, string Action, string? ResourceKind, Guid? ResourceId)
    : IQuery<Result<AccessExplanationDto>>, IAuthenticatedRequest, IWorkspaceRequest, IWriteRequest, IRequirePermission
{
    public PermissionAction Permission => PermissionAction.ReadAudit;
    PermissionAction IRequirePermission.Action => Permission;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind_("workspaces.workspace"), WorkspaceId, WorkspaceId);
    ResourceRef? IRequirePermission.Resource => Resource;
    private static ResourceKind ResourceKind_(string value) => Notrelix.Domain.SharedKernel.ResourceKind.Create(value);
}

public sealed class ExplainAccessQueryValidator : AbstractValidator<ExplainAccessQuery>
{
    public ExplainAccessQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Action).Must(a => Enum.TryParse<PermissionAction>(a, out _)).WithMessage("'Action' must be a permission of the catalog.");
        RuleFor(x => x).Must(x => (x.ResourceKind is null) == (x.ResourceId is null))
            .WithMessage("Give both 'ResourceKind' and 'ResourceId', or neither.");
        RuleFor(x => x.ResourceKind).Must(k => k is null || Notrelix.Domain.SharedKernel.ResourceKind.TryCreate(k, out _))
            .WithMessage("'ResourceKind' must be a canonical resource kind.");
    }
}

internal sealed record ExplainProbe(Guid WorkspaceId, PermissionAction Action)
    : IAuthenticatedRequest, IWorkspaceRequest, IReadRequest, IRequirePermission
{
    // A workspace-level action is asked of the workspace itself, as the real requests do.
    public ResourceRef? Resource => ResourceRef.Create(
        Notrelix.Domain.SharedKernel.ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

internal sealed record ExplainResourceProbe(PermissionAction Action, ResourceRef Target)
    : IAuthenticatedRequest, IResourceScopedRequest, IReadRequest, IRequirePermission
{
    public ResourceRef Resource => Target;
    ResourceRef? IRequirePermission.Resource => Target;
}

public sealed class ExplainAccessQueryHandler : IRequestHandler<ExplainAccessQuery, Result<AccessExplanationDto>>
{
    private readonly IAccessFactsProvider _facts;
    private readonly IAccessPolicyEvaluator _engine;
    private readonly IWorkspaceMemberFacts _members;
    private readonly IGovernanceDbContext _context;
    private readonly ICurrentRequestContext _request;
    private readonly IAuditService _audit;

    public ExplainAccessQueryHandler(
        IAccessFactsProvider facts, IAccessPolicyEvaluator engine, IWorkspaceMemberFacts members,
        IGovernanceDbContext context, ICurrentRequestContext request, IAuditService audit)
    {
        _facts = facts;
        _engine = engine;
        _members = members;
        _context = context;
        _request = request;
        _audit = audit;
    }

    public async Task<Result<AccessExplanationDto>> Handle(ExplainAccessQuery query, CancellationToken cancellationToken)
    {
        var accountId = _request.RequireAccountId();
        var member = await _members.FindAsync(query.WorkspaceId, query.UserId, cancellationToken);
        if (member is not { IsActive: true })
        {
            throw new NotFoundException("WorkspaceMember", query.UserId);
        }

        var action = Enum.Parse<PermissionAction>(query.Action);
        ResourceRef? target = query.ResourceKind is { } kind && query.ResourceId is { } id
            ? ResourceRef.Create(Notrelix.Domain.SharedKernel.ResourceKind.Create(kind), id, query.WorkspaceId)
            : null;

        object probe = target is null ? new ExplainProbe(query.WorkspaceId, action) : new ExplainResourceProbe(action, target);
        var descriptor = RequestDescriptorValidator.Create(probe.GetType());
        var snapshot = new ExecutionContextSnapshot(
            query.UserId, accountId, query.WorkspaceId, target ?? ((ExplainProbe)probe).Resource,
            ApplicationPrincipalKind.Authenticated,
            target is null ? ApplicationScopeKind.Workspace : ApplicationScopeKind.Resource,
            Guid.NewGuid().ToString());

        var facts = await _facts.ResolveAsync(descriptor, snapshot, probe, cancellationToken);
        var decision = _engine.Evaluate(descriptor, snapshot, facts, probe);

        string? roleName = null;
        if (facts.CustomRole is { } custom)
        {
            roleName = await _context.CustomRoles.AsNoTracking().Where(r => r.Id == custom.RoleId).Select(r => r.Name).FirstOrDefaultAsync(cancellationToken);
        }

        var dto = Build(query, member.Role, action, target, facts, decision, roleName);
        await _audit.RecordAsync(
            query.WorkspaceId, _request.UserId, $"ExplainAccess:{query.Action}",
            ResourceRef.Create(Notrelix.Domain.SharedKernel.ResourceKind.Create("workspaces.member"), query.UserId, query.WorkspaceId),
            AuditMetadata.Create(), AuditSeverity.Info, cancellationToken: cancellationToken);
        return Result<AccessExplanationDto>.Success(dto);
    }

    private static AccessExplanationDto Build(
        ExplainAccessQuery query, string storedRole, PermissionAction action, ResourceRef? target,
        AccessFacts facts, AccessDecision decision, string? roleName)
    {
        var steps = new List<ExplainStepDto>();
        var sources = new List<string>();
        void Step(string code, string outcome, string detail) => steps.Add(new ExplainStepDto(steps.Count + 1, code, outcome, detail));

        var custom = facts.CustomRole;
        var effective = custom?.Base ?? storedRole;
        var name = action.ToString();

        Step("membership", "applies", $"Active member with the built-in role {storedRole}.");
        if (storedRole == "Owner")
        {
            sources.Add("workspace-owner");
            Step("owner", "allows", "A workspace Owner passes every check except an unreachable resource.");
        }

        if (custom is not null)
        {
            sources.Add("custom-role");
            Step("custom-role", "applies",
                $"Custom role '{roleName ?? custom.RoleId.ToString()}' replaces the built-in role: base {custom.Base}, adds [{string.Join(", ", custom.Added.Order())}], removes [{string.Join(", ", custom.Removed.Order())}].");
        }

        var first = facts.PermissionRules.Count == 0 ? [] : facts.PermissionRules.Where(r => r.Priority == facts.PermissionRules.Min(c => c.Priority)).ToArray();
        if (first.Any(r => string.Equals(r.Effect, "Deny", StringComparison.OrdinalIgnoreCase)))
        {
            sources.Add("deny-rule");
            Step("rule", "denies", "An explicit Deny rule outranks everything else.");
        }
        else if (first.Any(r => string.Equals(r.Effect, "Allow", StringComparison.OrdinalIgnoreCase)))
        {
            sources.Add("allow-rule");
            Step("rule", "allows", "An explicit Allow rule applies.");
        }

        if (storedRole != "Owner")
        {
            var cell = WorkspaceRoleBaseline.Decide(effective, action);
            if (cell != BaselineDecision.Deny) sources.Add("role-baseline");
            Step("baseline", cell == BaselineDecision.Deny ? "denies" : "allows",
                $"The {effective} baseline for {name} is {cell}" + (cell == BaselineDecision.LevelBased ? " (decided on the board or page)." : "."));
            if (custom is not null && custom.Removed.Contains(name))
                Step("role-removes", "denies", $"The custom role removes {name}; only an explicit grant on the resource still applies.");
            else if (custom is not null && custom.Added.Contains(name))
                Step("role-adds", "allows", $"The custom role adds {name}.");
        }

        if (target is not null)
        {
            if (facts.ActiveResourcePermissionRank > 0 || facts.HasExplicitResourcePermission)
            {
                sources.Add("resource-grant");
                Step("resource-grant", "allows", $"An explicit grant (level rank {facts.ActiveResourcePermissionRank ?? 1}) applies on this resource.");
            }

            if (facts.ResourceMemberRole is { } boardRole)
            {
                sources.Add("board-membership");
                Step("board-membership", "allows", $"Board member with the role {boardRole}.");
            }

            if (facts.ResourceSpaceId is not null && facts.SpaceRole is { } spaceRole)
            {
                sources.Add("space");
                Step("space", "allows", $"Space role {spaceRole}.");
            }

            if (string.Equals(facts.ResourceAudience, "Workspace", StringComparison.Ordinal) && effective != "Guest")
            {
                sources.Add("workspace-default");
                Step("workspace-default", "allows", "The resource is visible to the workspace and the baseline gives the default level.");
            }

            if (string.Equals(facts.ResourceAudience, "Private", StringComparison.Ordinal))
                Step("private", "applies", "The resource is Private: a role never reaches it by itself, only membership or a grant does.");
        }

        Step("result", decision.Kind == AccessDecisionKind.Allowed ? "allows" : "denies",
            decision.Kind == AccessDecisionKind.Allowed ? "Allowed." : $"Not allowed ({decision.Kind}).");

        return new AccessExplanationDto(
            query.UserId, name, target?.Kind.Value, target?.ResourceId,
            decision.Kind == AccessDecisionKind.Allowed, decision.Kind.ToString(),
            storedRole, effective, roleName, sources.Distinct().ToArray(), steps);
    }
}
