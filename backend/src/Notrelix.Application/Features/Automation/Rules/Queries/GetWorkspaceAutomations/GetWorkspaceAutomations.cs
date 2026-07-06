using System.Text.Json;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.DTOs;

namespace Notrelix.Application.Features.Automation.Rules.Queries.GetWorkspaceAutomations;

public record GetWorkspaceAutomationsQuery(Guid WorkspaceId) : IQuery<Result<IReadOnlyList<AutomationRuleDto>>>;

public class GetWorkspaceAutomationsQueryHandler : IRequestHandler<GetWorkspaceAutomationsQuery, Result<IReadOnlyList<AutomationRuleDto>>>
{
    private readonly IAutomationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetWorkspaceAutomationsQueryHandler(
        IAutomationDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<AutomationRuleDto>>> Handle(GetWorkspaceAutomationsQuery request, CancellationToken cancellationToken)
    {
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
