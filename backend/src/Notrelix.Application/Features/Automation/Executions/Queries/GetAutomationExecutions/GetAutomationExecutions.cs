using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.DTOs;
using Notrelix.Application.Features.Automation.Executions.Services;

namespace Notrelix.Application.Features.Automation.Executions.Queries.GetAutomationExecutions;

/// <summary>
/// AUT-019: execution history (payload, failure evidence) is automation
/// management data and needs the permission that manages the Rule
/// (ManageWorkspaceSettings), never a board-view permission on the rule resource.
/// </summary>
public record GetAutomationExecutionsQuery(Guid AutomationRuleId, int Page = 1, int PageSize = 20, Guid? BoardId = null)
    : IQuery<Result<IReadOnlyList<AutomationExecutionDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => BoardId is null ? PermissionAction.ManageAutomation : PermissionAction.ManageBoard;
    public ResourceRef Resource => BoardId is { } board
        ? ResourceRef.Create(ResourceKind.Create("work-management.board"), board)
        : ResourceRef.Create(ResourceKind.Create("automation.rule"), AutomationRuleId);
}

public class GetAutomationExecutionsQueryHandler : IRequestHandler<GetAutomationExecutionsQuery, Result<IReadOnlyList<AutomationExecutionDto>>>
{
    /// <summary>How long a deleted rule's history stays readable.</summary>
    public static readonly TimeSpan DeletedRuleRetention = TimeSpan.FromDays(90);

    private readonly IAutomationDbContext _context;
    private readonly IDateTimeProvider? _clock;

    public GetAutomationExecutionsQueryHandler(IAutomationDbContext context, IDateTimeProvider? clock = null)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<AutomationExecutionDto>>> Handle(GetAutomationExecutionsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        // AU-6: authority on a named board covers only that board's rules.
        if (request.BoardId is { } claimedBoard
            && !await _context.AutomationRules.AsNoTracking()
                .AnyAsync(rule => rule.Id == request.AutomationRuleId && rule.BoardId == claimedBoard, cancellationToken))
        {
            throw new NotFoundException("AutomationRule", request.AutomationRuleId);
        }

        // A deleted rule's history stays readable for a while, then it is gone.
        var removed = await _context.AutomationRules.AsNoTracking()
            .Where(rule => rule.Id == request.AutomationRuleId && rule.Status == AutomationRuleStatus.Archived)
            .Select(rule => rule.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var now = _clock?.UtcNow ?? DateTimeOffset.UtcNow;
        if (removed is { } removedAt && removedAt < now - DeletedRuleRetention)
        {
            throw new NotFoundException("AutomationRule", request.AutomationRuleId);
        }

        var rows = await _context.AutomationExecutions
            .AsNoTracking()
            .Where(item => item.RuleId == request.AutomationRuleId)
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var executions = rows.Select(item => new AutomationExecutionDto(
                item.Id,
                item.WorkspaceId,
                item.RuleId,
                item.Status,
                item.AttemptCount,
                item.Payload,
                item.Error,
                item.CreatedAt.UtcDateTime,
                item.RuleVersion,
                AutomationExecutionProgress.Read(item.LastResponse)))
            .ToList();

        return Result<IReadOnlyList<AutomationExecutionDto>>.Success(executions);
    }
}
