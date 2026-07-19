namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.UnarchiveBoardView;

public class UnarchiveBoardViewCommandValidator : AbstractValidator<UnarchiveBoardViewCommand>
{
    public UnarchiveBoardViewCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.ViewId).NotEmpty();
    }
}
