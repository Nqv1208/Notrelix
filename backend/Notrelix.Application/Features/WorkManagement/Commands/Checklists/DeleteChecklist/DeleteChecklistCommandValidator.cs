using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands.Checklists.DeleteChecklist;

public class DeleteChecklistCommandValidator : AbstractValidator<DeleteChecklistCommand>
{
    public DeleteChecklistCommandValidator()
    {
    }
}
