namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.CompleteBoardItem;

public class CompleteBoardItemCommandValidator : AbstractValidator<CompleteBoardItemCommand>
{
    public CompleteBoardItemCommandValidator()
    {
        RuleFor(x => x.BoardItemId).NotEmpty();
    }
}
