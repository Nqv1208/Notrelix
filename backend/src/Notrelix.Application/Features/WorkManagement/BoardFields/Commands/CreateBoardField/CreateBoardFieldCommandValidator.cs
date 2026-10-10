namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.CreateBoardField;

public class CreateBoardFieldCommandValidator : AbstractValidator<CreateBoardFieldCommand>
{
    public CreateBoardFieldCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FieldType).NotEmpty().MaximumLength(50);
    }
}
