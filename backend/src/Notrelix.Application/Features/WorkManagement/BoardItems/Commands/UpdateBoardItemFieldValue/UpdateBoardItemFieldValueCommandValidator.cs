namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValue;

public class UpdateBoardItemFieldValueCommandValidator : AbstractValidator<UpdateBoardItemFieldValueCommand>
{
    public UpdateBoardItemFieldValueCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
