namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklist;

public class UpdateChecklistCommandValidator : AbstractValidator<UpdateChecklistCommand>
{
    public UpdateChecklistCommandValidator()
    {
        RuleFor(x => x.ChecklistId)
            .NotEmpty();
        RuleFor(x => x.Title!)
            .Must(title => !string.IsNullOrWhiteSpace(title)).WithMessage("Title cannot be blank.")
            .MaximumLength(Checklist.MaxTitleLength)
            .When(x => x.Title is not null);
    }
}
