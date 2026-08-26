using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.DTOs;

namespace Notrelix.Application.Features.Automation.Executions.Queries.GetAutomationExecutions;

public record GetAutomationExecutionsQuery(Guid AutomationRuleId, int Page = 1, int PageSize = 20)
    : IQuery<Result<IReadOnlyList<AutomationExecutionDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("automation.rule"), AutomationRuleId);
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
