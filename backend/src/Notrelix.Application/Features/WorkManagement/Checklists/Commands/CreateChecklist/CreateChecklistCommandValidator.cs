using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklist;

public class CreateChecklistCommandValidator : AbstractValidator<CreateChecklistCommand>
{
    public CreateChecklistCommandValidator()
    {
    }
}
