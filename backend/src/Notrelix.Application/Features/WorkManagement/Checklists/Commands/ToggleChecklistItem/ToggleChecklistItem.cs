using MediatR;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.ToggleChecklistItem;

public record ToggleChecklistItemCommand(Guid ChecklistItemId) : ICommand<Result>;

public class ToggleChecklistItemCommandHandler : IRequestHandler<ToggleChecklistItemCommand, Result>
{
    public Task<Result> Handle(ToggleChecklistItemCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
