namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoardVisibility;

public class UpdateBoardVisibilityCommandValidator : AbstractValidator<UpdateBoardVisibilityCommand>
{
    public UpdateBoardVisibilityCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Visibility).IsInEnum().WithMessage("Visibility must be a valid board visibility value.");
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
