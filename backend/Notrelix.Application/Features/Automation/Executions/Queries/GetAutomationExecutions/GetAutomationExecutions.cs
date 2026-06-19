using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Automation.DTOs;

namespace Notrelix.Application.Features.Automation.Executions.Queries.GetAutomationExecutions;

public record GetAutomationExecutionsQuery(Guid AutomationRuleId, int Page = 1, int PageSize = 20)
    : IQuery<Result<IReadOnlyList<AutomationExecutionDto>>>;

public class GetAutomationExecutionsQueryHandler : IRequestHandler<GetAutomationExecutionsQuery, Result<IReadOnlyList<AutomationExecutionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public GetAutomationExecutionsQueryHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result<IReadOnlyList<AutomationExecutionDto>>> Handle(GetAutomationExecutionsQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await _context.AutomationRules
            .AsNoTracking()
            .Where(item => item.Id == request.AutomationRuleId)
            .Select(item => item.WorkspaceId)
            .FirstOrDefaultAsync(cancellationToken);

        if (workspaceId == Guid.Empty ||
            !await _permissions.CanViewWorkspaceAsync(workspaceId, _currentUser.UserId, cancellationToken))
        {
            return Result<IReadOnlyList<AutomationExecutionDto>>.Failure("Automation not found or access denied.");
        }

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
