namespace Notrelix.Application.Features.Documents.Pages.Commands.DuplicatePage;

public class DuplicatePageCommandValidator : AbstractValidator<DuplicatePageCommand>
{
    public DuplicatePageCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.PageId).NotEmpty();
        RuleFor(x => x.Title).MaximumLength(200);
    }
}
