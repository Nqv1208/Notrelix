using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklistItem;

public class CreateChecklistItemCommandValidator : AbstractValidator<CreateChecklistItemCommand>
{
    public CreateChecklistItemCommandValidator()
    {
        RuleFor(x => x.ChecklistId)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);
    }
}
