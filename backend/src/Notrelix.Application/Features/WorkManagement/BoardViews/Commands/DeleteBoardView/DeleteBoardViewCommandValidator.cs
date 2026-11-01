namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.DeleteBoardView;

public class DeleteBoardViewCommandValidator : AbstractValidator<DeleteBoardViewCommand>
{
    public DeleteBoardViewCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.ViewId).NotEmpty();
    }
}
