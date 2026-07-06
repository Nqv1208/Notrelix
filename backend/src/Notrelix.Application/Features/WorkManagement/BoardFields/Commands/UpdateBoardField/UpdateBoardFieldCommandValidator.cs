namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateBoardField;

public class UpdateBoardFieldCommandValidator : AbstractValidator<UpdateBoardFieldCommand>
{
    public UpdateBoardFieldCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.ColumnId).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.FieldType).MaximumLength(50);
    }
}
