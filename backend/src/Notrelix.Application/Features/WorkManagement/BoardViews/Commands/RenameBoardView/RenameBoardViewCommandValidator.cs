namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.RenameBoardView;

public class RenameBoardViewCommandValidator : AbstractValidator<RenameBoardViewCommand>
{
    public RenameBoardViewCommandValidator()
    {
        RuleFor(x => x.ViewId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255);
    }
}
