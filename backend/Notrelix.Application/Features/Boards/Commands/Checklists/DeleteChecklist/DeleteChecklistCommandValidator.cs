using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Checklists.DeleteChecklist;

public class DeleteChecklistCommandValidator : AbstractValidator<DeleteChecklistCommand>
{
    public DeleteChecklistCommandValidator()
    {
    }
}
