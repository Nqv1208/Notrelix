using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Automation.DTOs;

namespace Notrelix.Application.Features.Automation.Rules.Queries.GetWorkspaceAutomations;

public record GetWorkspaceAutomationsQuery(Guid WorkspaceId) : IQuery<Result<IReadOnlyList<AutomationRuleDto>>>;

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
            .ToListAsync(cancellationToken);

        var dtos = automations.Select(item => new AutomationRuleDto(
                item.Id,
                item.WorkspaceId,
                item.Name,
                item.Configuration.Trigger.Type,
                item.Configuration.Action.Type,
                JsonSerializer.Serialize(item.Configuration),
                item.IsEnabled,
                item.CreatedAt.DateTime,
                item.UpdatedAt.HasValue ? item.UpdatedAt.Value.DateTime : null))
            .ToList();

        return Result<IReadOnlyList<AutomationRuleDto>>.Success(dtos);
    }
}
