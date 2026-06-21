using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.CreateBoardItemLink;

public class CreateCardLinkCommandValidator : AbstractValidator<CreateBoardItemLinkCommand>
{
    public CreateCardLinkCommandValidator()
    {
    }
}
