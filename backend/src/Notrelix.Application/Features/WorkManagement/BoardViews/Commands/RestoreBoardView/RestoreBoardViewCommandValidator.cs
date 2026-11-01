namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.RestoreBoardView;

public class RestoreBoardViewCommandValidator : AbstractValidator<RestoreBoardViewCommand>
{
    public RestoreBoardViewCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.ViewId).NotEmpty();
    }
}
