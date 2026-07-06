namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValues;

public class UpdateBoardItemFieldValuesCommandValidator : AbstractValidator<UpdateBoardItemFieldValuesCommand>
{
    public UpdateBoardItemFieldValuesCommandValidator()
    {
        RuleFor(x => x.BoardItemId).NotEmpty();
        RuleFor(x => x.Values).NotNull().Must(v => v.Count > 0);
    }
}
