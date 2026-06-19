using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklist;

public class UpdateChecklistCommandValidator : AbstractValidator<UpdateChecklistCommand>
{
    public UpdateChecklistCommandValidator()
    {
    }
}
