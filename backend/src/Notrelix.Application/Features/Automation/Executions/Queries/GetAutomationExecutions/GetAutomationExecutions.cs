using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.DTOs;

namespace Notrelix.Application.Features.Automation.Executions.Queries.GetAutomationExecutions;

/// <summary>
/// AUT-019: execution history (payload, failure evidence) is automation
/// management data and needs the permission that manages the Rule
/// (ManageWorkspaceSettings), never a board-view permission on the rule resource.
/// </summary>
public record GetAutomationExecutionsQuery(Guid AutomationRuleId, int Page = 1, int PageSize = 20, Guid? BoardId = null)
    : IQuery<Result<IReadOnlyList<AutomationExecutionDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => BoardId is null ? PermissionAction.ManageWorkspaceSettings : PermissionAction.ManageBoard;
    public ResourceRef Resource => BoardId is { } board
        ? ResourceRef.Create(ResourceKind.Create("work-management.board"), board)
        : ResourceRef.Create(ResourceKind.Create("automation.rule"), AutomationRuleId);
}

public class GetAutomationExecutionsQueryHandler : IRequestHandler<GetAutomationExecutionsQuery, Result<IReadOnlyList<AutomationExecutionDto>>>
{
    private readonly IAutomationDbContext _context;

    public GetAutomationExecutionsQueryHandler(IAutomationDbContext context)
    {
        _context = context;
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

        var executions = await _context.AutomationExecutions
            .AsNoTracking()
            .Where(item => item.RuleId == request.AutomationRuleId)
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new AutomationExecutionDto(
                item.Id,
                item.WorkspaceId,
                item.RuleId,
                item.Status,
                item.AttemptCount,
                item.Payload,
                item.Error,
                item.CreatedAt.DateTime))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<AutomationExecutionDto>>.Success(executions);
    }
}
