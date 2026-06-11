using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class DeleteCardLinkCommandValidator : AbstractValidator<DeleteBoardItemLinkCommand>
{
    public DeleteCardLinkCommandValidator()
    {
    }
}
