namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.RestoreBoard;

public class RestoreBoardCommandValidator : AbstractValidator<RestoreBoardCommand>
{
    public RestoreBoardCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
    }
}
