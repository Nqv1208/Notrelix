using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Domain.Accounts.Members;

namespace Notrelix.Application.Common.Security;

public class PermissionService : IPermissionService, IPermissionEvaluator, IAuthorizationDecisionStore
{
    private static readonly ResourceKind BoardKind = ResourceKind.Create("work-management.board");
    private static readonly ResourceKind WorkspaceKind = ResourceKind.Create("workspaces.workspace");

    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly IGovernanceDbContext _governanceContext;
    private readonly IAccountDbContext _accountContext;
    private readonly IResourceAuthorizationSnapshotStore _resourceSnapshots;
    private readonly IDateTimeProvider _clock;

    public PermissionService(
        IWorkspaceDbContext workspaceContext,
        IGovernanceDbContext governanceContext,
        IAccountDbContext accountContext,
        IResourceAuthorizationSnapshotStore resourceSnapshots,
        IDateTimeProvider clock)
    {
        _workspaceContext = workspaceContext;
        _governanceContext = governanceContext;
        _accountContext = accountContext;
        _resourceSnapshots = resourceSnapshots;
        _clock = clock;
    }

    public async Task<PermissionDecision> EvaluateAsync(
        PermissionContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.Scope is PermissionScope.Workspace or PermissionScope.Resource && !context.WorkspaceId.HasValue)
        {
            throw new SecurityMisconfigurationException(
                $"Permission evaluation requires WorkspaceId for scope {context.Scope} " +
                $"but WorkspaceId is null. ResourceKind={context.ResourceKind} Action={context.Action}");
        }

        if (context.Scope == PermissionScope.Account)
        {
            return await EvaluateAccountAsync(context, cancellationToken);
        }

        // Resolve AccountId from workspace if not provided
        if (context.AccountId == Guid.Empty && context.WorkspaceId.HasValue)
        {
            var accountId = await _workspaceContext.Workspaces
                .Where(w => w.Id == context.WorkspaceId.Value)
                .Select(w => (Guid?)w.AccountId)
                .FirstOrDefaultAsync(cancellationToken);

            if (accountId.HasValue)
            {
                context = context with { AccountId = accountId.Value };
            }
        }

        // 1. Check workspace membership
        var workspaceMember = await _workspaceContext.WorkspaceMembers
            .FirstOrDefaultAsync(
                m => m.AccountId == context.AccountId
                    && m.WorkspaceId == context.WorkspaceId
                    && m.UserId == context.UserId,
                cancellationToken);

        if (workspaceMember is null)
        {
            return new PermissionDecision(false, "not_workspace_member");
        }

        // 2. Owner has all rights
        if (workspaceMember.Role == WorkspaceRole.Owner)
        {
            return new PermissionDecision(true, null, PermissionLevel.Owner);
        }

        // 3. DeleteWorkspace is typically Owner only
        if (context.Action == PermissionAction.DeleteWorkspace)
        {
            return new PermissionDecision(false, "missing_permission");
        }

        // 4. Check PermissionRule (future source of truth)
        var ruleDecision = await EvaluateRulesAsync(context, cancellationToken);
        if (ruleDecision is not null)
        {
            return ruleDecision;
        }

        // 5. Resource specific permissions (legacy fallback)
        if (context.ResourceKind == BoardKind && context.ResourceId.HasValue)
        {
            var board = await _resourceSnapshots.ResolveAsync(
                context.ResourceKind,
                context.ResourceId.Value,
                context.UserId,
                cancellationToken);

            if (board is null || board.WorkspaceId != context.WorkspaceId)
            {
                return new PermissionDecision(false, "resource_not_found");
            }

            if (board.Audience == ResourceAudience.Restricted)
            {
                var hasExplicitPermission = await _governanceContext.ResourcePermissions
                    .AnyAsync(p => p.AccountId == context.AccountId &&
                                   p.WorkspaceId == context.WorkspaceId &&
                                   p.ResourceKind == BoardKind &&
                                   p.ResourceId == context.ResourceId.Value &&
                                   p.SubjectType == PermissionSubjectType.User &&
                                   p.SubjectId == context.UserId &&
                                   p.DeletedAt == null, cancellationToken);

                if (board.MemberAccess is null && !hasExplicitPermission)
                {
                    return new PermissionDecision(false, "resource_not_found");
                }

                if (board.MemberAccess is not null)
                {
                    if (context.Action == PermissionAction.UpdateItem && board.MemberAccess == ResourceMemberAccess.Viewer)
                    {
                        return new PermissionDecision(false, "missing_permission");
                    }
                    return new PermissionDecision(true, null, MapResourceAccess(board.MemberAccess.Value));
                }

                return new PermissionDecision(true, null, PermissionLevel.Viewer);
            }

            if (board.Audience == ResourceAudience.Workspace)
            {
                if (workspaceMember.Role == WorkspaceRole.Guest)
                {
                    if (board.MemberAccess is null)
                    {
                        return new PermissionDecision(false, "resource_not_found");
                    }
                }

                if (board.MemberAccess is not null)
                {
                    if (context.Action == PermissionAction.UpdateItem && board.MemberAccess == ResourceMemberAccess.Viewer)
                    {
                        return new PermissionDecision(false, "missing_permission");
                    }
                    return new PermissionDecision(true, null, MapResourceAccess(board.MemberAccess.Value));
                }

                return new PermissionDecision(true, null, PermissionLevel.Viewer);
            }
        }

        return context.Action switch
        {
            PermissionAction.ViewWorkspace or PermissionAction.ViewBoard or PermissionAction.ViewMembers
                => new PermissionDecision(true, null, PermissionLevel.Viewer),
            _ => new PermissionDecision(false, "missing_permission")
        };
    }

    private async Task<PermissionDecision> EvaluateAccountAsync(
        PermissionContext context,
        CancellationToken cancellationToken)
    {
        var member = await _accountContext.AccountMembers
            .FirstOrDefaultAsync(
                m => m.AccountId == context.AccountId
                    && m.UserId == context.UserId
                    && m.Status == AccountMemberStatus.Active,
                cancellationToken);

        if (member is null)
        {
            return new PermissionDecision(false, "not_account_member");
        }

        if (member.Role == AccountRole.Owner)
        {
            return new PermissionDecision(true, null, PermissionLevel.Owner);
        }

        var ruleDecision = await EvaluateRulesAsync(context, cancellationToken);
        if (ruleDecision is not null)
        {
            return ruleDecision;
        }

        return context.Action switch
        {
            PermissionAction.ViewWorkspace => new PermissionDecision(true, null, PermissionLevel.Viewer),
            _ => new PermissionDecision(false, "missing_permission")
        };
    }

    private async Task<PermissionDecision?> EvaluateRulesAsync(
        PermissionContext context,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        var rules = await _governanceContext.PermissionRules
            .Where(r => r.AccountId == context.AccountId
                && r.WorkspaceId == context.WorkspaceId
                && r.Status == PermissionRuleStatus.Active
                && r.DeletedAt == null
                && (r.StartsAt == null || r.StartsAt <= now)
                && (r.ExpiresAt == null || r.ExpiresAt > now))
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);

        foreach (var priorityGroup in rules.GroupBy(x => x.Priority))
        {
            var applicable = priorityGroup
                .Where(rule => MatchesScope(rule, context))
                .Where(rule => MatchesSubject(rule, context))
                .Where(rule => MatchesAction(rule, context.Action))
                .ToArray();

            if (applicable.Any(rule => rule.Effect == PermissionEffect.Deny))
            {
                return new PermissionDecision(false, "denied_by_rule", PermissionLevel.None);
            }

            if (applicable.Any(rule => rule.Effect == PermissionEffect.Allow))
            {
                return new PermissionDecision(true, null, PermissionLevel.Editor);
            }
        }

        return null;
    }

    private static bool MatchesScope(PermissionRule rule, PermissionContext context)
    {
        if (rule.ScopeType == PermissionScopeType.Workspace)
            return true;

        if (rule.ResourceKind.HasValue && rule.ResourceKind.Value != context.ResourceKind)
            return false;

        if (rule.ResourceId.HasValue && rule.ResourceId.Value != context.ResourceId)
            return false;

        return true;
    }

    private static bool MatchesSubject(PermissionRule rule, PermissionContext context)
    {
        return rule.SubjectType switch
        {
            PermissionSubjectType.User => rule.SubjectId == context.UserId,
            _ => false
        };
    }

    private static bool MatchesAction(PermissionRule rule, PermissionAction action)
    {
        return rule.Action == action;
    }

    public async Task EnsureAllowedAsync(
        PermissionContext context,
        CancellationToken cancellationToken = default)
    {
        var decision = await EvaluateAsync(context, cancellationToken);
        if (!decision.IsAllowed)
        {
            throw new AppForbidden(decision.ReasonCode ?? "missing_permission");
        }
    }

    private static PermissionLevel MapResourceAccess(ResourceMemberAccess access)
    {
        return access switch
        {
            ResourceMemberAccess.Viewer => PermissionLevel.Viewer,
            ResourceMemberAccess.Editor => PermissionLevel.Editor,
            ResourceMemberAccess.Manager => PermissionLevel.Owner,
            _ => PermissionLevel.None
        };
    }

    public async Task<bool> AuthorizeAsync(
        Guid userId,
        Guid workspaceId,
        ResourceKind resourceKind,
        Guid resourceId,
        PermissionAction action,
        CancellationToken cancellationToken = default)
    {
        var decision = await EvaluateAsync(new PermissionContext(userId, Guid.Empty, workspaceId, resourceKind, resourceId, action, PermissionScope.Resource), cancellationToken);
        return decision.IsAllowed;
    }

    public async Task<bool> AuthorizeWorkspaceAsync(
        Guid userId,
        Guid workspaceId,
        PermissionAction action,
        CancellationToken cancellationToken = default)
    {
        var decision = await EvaluateAsync(new PermissionContext(userId, Guid.Empty, workspaceId, WorkspaceKind, null, action, PermissionScope.Workspace), cancellationToken);
        return decision.IsAllowed;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid workspaceId,
        ResourceKind resourceKind,
        Guid? resourceId,
        PermissionAction action,
        CancellationToken cancellationToken = default)
    {
        var decision = await EvaluateAsync(new PermissionContext(userId, Guid.Empty, workspaceId, resourceKind, resourceId, action, PermissionScope.Resource), cancellationToken);
        return decision.IsAllowed;
    }
}
