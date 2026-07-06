namespace Notrelix.Application.Features.Documents.Pages.Commands.MovePage;

public class MovePageCommandValidator : AbstractValidator<MovePageCommand>
{
    public MovePageCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
    }
}
