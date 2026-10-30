namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.CopyChecklist;

public class CopyChecklistCommandValidator : AbstractValidator<CopyChecklistCommand>
{
    public CopyChecklistCommandValidator()
    {
        RuleFor(x => x.BoardItemId).NotEmpty();
        RuleFor(x => x.SourceChecklistId).NotEmpty();
        RuleFor(x => x.Title).MaximumLength(Checklist.MaxTitleLength);
    }
}
