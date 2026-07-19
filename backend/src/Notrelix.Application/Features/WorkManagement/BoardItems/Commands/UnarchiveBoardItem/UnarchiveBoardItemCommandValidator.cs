namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnarchiveBoardItem;

public class UnarchiveBoardItemCommandValidator : AbstractValidator<UnarchiveBoardItemCommand>
{
    public UnarchiveBoardItemCommandValidator()
    {
        RuleFor(x => x.BoardItemId).NotEmpty();
    }
}
