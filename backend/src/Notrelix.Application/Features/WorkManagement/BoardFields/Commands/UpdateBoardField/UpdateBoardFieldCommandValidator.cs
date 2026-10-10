namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateBoardField;

public class UpdateBoardFieldCommandValidator : AbstractValidator<UpdateBoardFieldCommand>
{
    public UpdateBoardFieldCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.ColumnId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).When(x => x.Name is not null);
    }
}
