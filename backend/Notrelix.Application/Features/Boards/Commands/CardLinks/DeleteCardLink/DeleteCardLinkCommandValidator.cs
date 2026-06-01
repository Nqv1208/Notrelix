using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.CardLinks.DeleteCardLink;

public class DeleteCardLinkCommandValidator : AbstractValidator<DeleteCardLinkCommand>
{
    public DeleteCardLinkCommandValidator()
    {
    }
}
