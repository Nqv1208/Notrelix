namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SetBoardViewVisibility;

public class SetBoardViewVisibilityCommandValidator : AbstractValidator<SetBoardViewVisibilityCommand>
{
    public SetBoardViewVisibilityCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.ViewId).NotEmpty();
    }
}
