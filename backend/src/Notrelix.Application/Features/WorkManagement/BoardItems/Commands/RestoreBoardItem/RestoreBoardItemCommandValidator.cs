namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.RestoreBoardItem;

public class RestoreBoardItemCommandValidator : AbstractValidator<RestoreBoardItemCommand>
{
    public RestoreBoardItemCommandValidator()
    {
        RuleFor(x => x.BoardItemId).NotEmpty();
    }
}
