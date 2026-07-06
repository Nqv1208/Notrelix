namespace Notrelix.Application.Features.WorkManagement.Labels.Commands.AddLabelToBoardItem;

public class AddLabelToBoardItemCommandValidator : AbstractValidator<AddLabelToBoardItemCommand>
{
    public AddLabelToBoardItemCommandValidator()
    {
        RuleFor(x => x.BoardItemId)
            .NotEmpty();

        RuleFor(x => x.LabelId)
            .NotEmpty();
    }
}
