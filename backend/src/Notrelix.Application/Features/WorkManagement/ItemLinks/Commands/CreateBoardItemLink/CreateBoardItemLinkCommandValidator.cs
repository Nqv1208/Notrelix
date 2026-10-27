namespace Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.CreateBoardItemLink;

public class CreateBoardItemLinkCommandValidator : AbstractValidator<CreateBoardItemLinkCommand>
{
    public CreateBoardItemLinkCommandValidator()
    {
        RuleFor(x => x.BoardItemId)
            .NotEmpty();

        RuleFor(x => x.OtherBoardItemId)
            .NotEmpty();

        RuleFor(x => x.Relation)
            .NotEmpty()
            .MaximumLength(50);
    }
}
