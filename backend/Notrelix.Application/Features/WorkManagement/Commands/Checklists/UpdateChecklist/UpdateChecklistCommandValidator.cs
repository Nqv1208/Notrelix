using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands.Checklists.UpdateChecklist;

public class UpdateChecklistCommandValidator : AbstractValidator<UpdateChecklistCommand>
{
    public UpdateChecklistCommandValidator()
    {
    }
}
