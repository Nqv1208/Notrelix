using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Governance.Abstractions;

namespace Notrelix.Application.Common.Security;

public class PermissionService : IPermissionService, IPermissionEvaluator
{
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly IWorkManagementDbContext _workContext;
    private readonly IGovernanceDbContext _governanceContext;
    private readonly IDateTimeProvider _clock;

    public PermissionService(
        IWorkspaceDbContext workspaceContext,
        IWorkManagementDbContext workContext,
        IGovernanceDbContext governanceContext,
        IDateTimeProvider clock)
    {
        _workspaceContext = workspaceContext;
        _workContext = workContext;
        _governanceContext = governanceContext;
        _clock = clock;
    }

    public async Task<PermissionDecision> EvaluateAsync(
        PermissionContext context,
        CancellationToken cancellationToken = default)
    {
        // 1. Check workspace membership
        var workspaceMember = await _workspaceContext.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == context.WorkspaceId && m.UserId == context.UserId, cancellationToken);

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
        if (context.ResourceType == ResourceType.Board && context.ResourceId.HasValue)
        {
            var board = await _workContext.Boards
                .FirstOrDefaultAsync(b => b.Id == context.ResourceId.Value && b.WorkspaceId == context.WorkspaceId, cancellationToken);

            if (board is null || board.IsArchived)
            {
                return new PermissionDecision(false, "resource_not_found");
            }

            // Check if board is private
            if (board.Visibility == BoardVisibility.Private)
            {
                // Must be a board member or have explicit resource permission
                var boardMember = await _workContext.BoardMembers
                    .FirstOrDefaultAsync(m => m.BoardId == board.Id && m.UserId == context.UserId, cancellationToken);

                var hasExplicitPermission = await _governanceContext.ResourcePermissions
                    .AnyAsync(p => p.WorkspaceId == context.WorkspaceId &&
                                   p.ResourceType == ResourceType.Board &&
                                   p.ResourceId == board.Id &&
                                   p.SubjectType == PermissionSubjectType.User &&
                                   p.SubjectId == context.UserId &&
                                   p.DeletedAt == null, cancellationToken);

                if (boardMember is null && !hasExplicitPermission)
                {
                    return new PermissionDecision(false, "resource_not_found");
                }

                if (boardMember is not null)
                {
                    if (context.Action == PermissionAction.UpdateItem && boardMember.Role == BoardRole.Observer)
                    {
                        return new PermissionDecision(false, "missing_permission");
                    }
                    return new PermissionDecision(true, null, MapBoardRole(boardMember.Role));
                }

                return new PermissionDecision(true, null, PermissionLevel.Viewer);
            }

            // Workspace board
            if (board.Visibility == BoardVisibility.Workspace)
            {
                if (workspaceMember.Role == WorkspaceRole.Guest)
                {
                    var boardMember = await _workContext.BoardMembers
                        .FirstOrDefaultAsync(m => m.BoardId == board.Id && m.UserId == context.UserId, cancellationToken);

                    if (boardMember is null)
                    {
                        return new PermissionDecision(false, "resource_not_found");
                    }
                }

                var boardMemberCheck = await _workContext.BoardMembers
                    .FirstOrDefaultAsync(m => m.BoardId == board.Id && m.UserId == context.UserId, cancellationToken);

                if (boardMemberCheck is not null)
                {
                    if (context.Action == PermissionAction.UpdateItem && boardMemberCheck.Role == BoardRole.Observer)
                    {
                        return new PermissionDecision(false, "missing_permission");
                    }
                    return new PermissionDecision(true, null, MapBoardRole(boardMemberCheck.Role));
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

    private async Task<PermissionDecision?> EvaluateRulesAsync(
        PermissionContext context,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        var rules = await _governanceContext.PermissionRules
            .Where(r => r.WorkspaceId == context.WorkspaceId
                && r.Status == PermissionRuleStatus.Active
                && r.DeletedAt == null
                && (r.StartsAt == null || r.StartsAt <= now)
                && (r.ExpiresAt == null || r.ExpiresAt > now))
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            if (!MatchesScope(rule, context)) continue;
            if (!MatchesSubject(rule, context)) continue;
            if (!MatchesAction(rule, context.Action)) continue;

            if (rule.Effect == PermissionEffect.Deny)
            {
                return new PermissionDecision(false, "denied_by_rule", PermissionLevel.None);
            }

            return new PermissionDecision(true, null, PermissionLevel.Editor);
        }

        return null;
    }

    private static bool MatchesScope(PermissionRule rule, PermissionContext context)
    {
        if (rule.ScopeType == PermissionScopeType.Workspace)
            return true;

        if (rule.ResourceType.HasValue && rule.ResourceType.Value != context.ResourceType)
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

    private static PermissionLevel MapBoardRole(BoardRole role)
    {
        return role switch
        {
            BoardRole.Observer => PermissionLevel.Viewer,
            BoardRole.Member => PermissionLevel.Editor,
            BoardRole.Admin => PermissionLevel.Owner,
            _ => PermissionLevel.None
        };
    }

    public async Task<bool> AuthorizeAsync(
        Guid userId,
        Guid workspaceId,
        ResourceType resourceType,
        Guid resourceId,
        PermissionAction action,
        CancellationToken cancellationToken = default)
    {
        var decision = await EvaluateAsync(new PermissionContext(userId, workspaceId, resourceType, resourceId, action), cancellationToken);
        return decision.IsAllowed;
    }

    public async Task<bool> AuthorizeWorkspaceAsync(
        Guid userId,
        Guid workspaceId,
        PermissionAction action,
        CancellationToken cancellationToken = default)
    {
        var decision = await EvaluateAsync(new PermissionContext(userId, workspaceId, ResourceType.Workspace, null, action), cancellationToken);
        return decision.IsAllowed;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid workspaceId,
        ResourceType resourceType,
        Guid? resourceId,
        PermissionAction action,
        CancellationToken cancellationToken = default)
    {
        var decision = await EvaluateAsync(new PermissionContext(userId, workspaceId, resourceType, resourceId, action), cancellationToken);
        return decision.IsAllowed;
    }
}
