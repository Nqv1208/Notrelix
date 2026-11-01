namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.UpdateBoardViewConfig;

public class UpdateBoardViewConfigCommandValidator : AbstractValidator<UpdateBoardViewConfigCommand>
{
    public UpdateBoardViewConfigCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.ViewId).NotEmpty();
    }
}
