using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.CreateBoardItemLink;

public class CreateBoardItemLinkCommandValidator : AbstractValidator<CreateBoardItemLinkCommand>
{
    public CreateBoardItemLinkCommandValidator()
    {
        RuleFor(x => x.SourceBoardItemId)
            .NotEmpty();

        RuleFor(x => x.TargetBoardItemId)
            .NotEmpty();

        RuleFor(x => x.LinkType)
            .NotEmpty()
            .MaximumLength(50);
    }
}
