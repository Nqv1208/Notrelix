namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklistItem;

public class UpdateChecklistItemCommandValidator : AbstractValidator<UpdateChecklistItemCommand>
{
    public UpdateChecklistItemCommandValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty();
        RuleFor(x => x.Title!)
            .Must(title => !string.IsNullOrWhiteSpace(title)).WithMessage("Title cannot be blank.")
            .MaximumLength(Checklist.MaxTitleLength)
            .When(x => x.Title is not null);
    }
}
