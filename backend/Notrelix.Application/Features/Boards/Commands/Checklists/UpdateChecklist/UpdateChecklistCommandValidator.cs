using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Checklists.UpdateChecklist;

public class UpdateChecklistCommandValidator : AbstractValidator<UpdateChecklistCommand>
{
    public UpdateChecklistCommandValidator()
    {
    }
}
