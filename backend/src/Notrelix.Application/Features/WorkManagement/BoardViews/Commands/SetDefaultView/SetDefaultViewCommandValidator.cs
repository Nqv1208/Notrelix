namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SetDefaultView;

public class SetDefaultViewCommandValidator : AbstractValidator<SetDefaultViewCommand>
{
    public SetDefaultViewCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.ViewId).NotEmpty();
    }
}
