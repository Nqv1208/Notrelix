using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Labels.Commands.RemoveLabelFromBoardItem;

public class RemoveLabelFromBoardItemCommandValidator : AbstractValidator<RemoveLabelFromBoardItemCommand>
{
    public RemoveLabelFromBoardItemCommandValidator()
    {
        RuleFor(x => x.BoardItemId)
            .NotEmpty();

        RuleFor(x => x.LabelId)
            .NotEmpty();
    }
}
