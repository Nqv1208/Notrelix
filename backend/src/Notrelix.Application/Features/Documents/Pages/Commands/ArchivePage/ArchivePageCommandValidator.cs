namespace Notrelix.Application.Features.Documents.Pages.Commands.ArchivePage;

public class ArchivePageCommandValidator : AbstractValidator<ArchivePageCommand>
{
    public ArchivePageCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
    }
}
