using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.DeleteBoardItemLink;

public class DeleteCardLinkCommandValidator : AbstractValidator<DeleteBoardItemLinkCommand>
{
    public DeleteCardLinkCommandValidator()
    {
    }
}
