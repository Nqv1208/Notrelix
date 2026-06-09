using MediatR;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.Entities.Extensibility;

namespace Notrelix.Application.Features.Extensibility.Commands.CreateAutomationRule;

public record CreateAutomationRuleCommand(
    Guid WorkspaceId,
    string Name,
    string TriggerEvent,
    string ActionType,
    string Configuration) : IRequest<Result<Guid>>;

public class CreateAutomationRuleCommandHandler : IRequestHandler<CreateAutomationRuleCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public CreateAutomationRuleCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result<Guid>> Handle(CreateAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        await _permissions.EnsureCanManageWorkspaceAsync(request.WorkspaceId, _currentUser.UserId, cancellationToken);

        var rule = AutomationRule.Create(
            request.WorkspaceId,
            _currentUser.UserId,
            request.Name,
            request.TriggerEvent,
            request.ActionType,
            request.Configuration);

        _context.AutomationRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(rule.Id);
    }
}
