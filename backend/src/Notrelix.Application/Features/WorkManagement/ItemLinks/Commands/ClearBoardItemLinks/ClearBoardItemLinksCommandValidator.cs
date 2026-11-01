namespace Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.ClearBoardItemLinks;

public class ClearBoardItemLinksCommandValidator : AbstractValidator<ClearBoardItemLinksCommand>
{
    public ClearBoardItemLinksCommandValidator() => RuleFor(x => x.BoardItemId).NotEmpty();
}
