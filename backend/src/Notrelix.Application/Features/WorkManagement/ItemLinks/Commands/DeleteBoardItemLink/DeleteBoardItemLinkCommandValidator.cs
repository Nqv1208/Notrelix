namespace Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.DeleteBoardItemLink;

public class DeleteBoardItemLinkCommandValidator : AbstractValidator<DeleteBoardItemLinkCommand>
{
    public DeleteBoardItemLinkCommandValidator()
    {
        RuleFor(x => x.BoardItemLinkId)
            .NotEmpty();
    }
}
