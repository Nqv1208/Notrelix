using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Extensibility.DTOs;

namespace Notrelix.Application.Features.Extensibility.Queries.GetWorkspaceAutomations;

public record GetWorkspaceAutomationsQuery(Guid WorkspaceId) : IRequest<Result<IReadOnlyList<AutomationRuleDto>>>;

public class GetWorkspaceAutomationsQueryHandler : IRequestHandler<GetWorkspaceAutomationsQuery, Result<IReadOnlyList<AutomationRuleDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public GetWorkspaceAutomationsQueryHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result<IReadOnlyList<AutomationRuleDto>>> Handle(GetWorkspaceAutomationsQuery request, CancellationToken cancellationToken)
    {
        if (!await _permissions.CanViewWorkspaceAsync(request.WorkspaceId, _currentUser.UserId, cancellationToken))
        {
            return Result<IReadOnlyList<AutomationRuleDto>>.Failure("Workspace not found or access denied.");
        }

        var automations = await _context.AutomationRules
            .AsNoTracking()
            .Where(item => item.WorkspaceId == request.WorkspaceId)
            .OrderBy(item => item.Name)
            .Select(item => new AutomationRuleDto(
                item.Id,
                item.WorkspaceId,
                item.Name,
                item.TriggerEvent,
                item.ActionType,
                item.Configuration,
                item.IsEnabled,
                item.CreatedAt,
                item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<AutomationRuleDto>>.Success(automations);
    }
}
