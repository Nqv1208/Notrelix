using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands.Checklists.CreateChecklist;

public class CreateChecklistCommandValidator : AbstractValidator<CreateChecklistCommand>
{
    public CreateChecklistCommandValidator()
    {
    }
}
