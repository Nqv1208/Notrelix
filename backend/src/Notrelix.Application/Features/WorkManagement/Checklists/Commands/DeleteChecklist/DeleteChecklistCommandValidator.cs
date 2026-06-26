using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.DeleteChecklist;

public class DeleteChecklistCommandValidator : AbstractValidator<DeleteChecklistCommand>
{
    public DeleteChecklistCommandValidator()
    {
        RuleFor(x => x.ChecklistId)
            .NotEmpty();
    }
}
