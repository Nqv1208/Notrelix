using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class CreateCardLinkCommandValidator : AbstractValidator<CreateBoardItemLinkCommand>
{
    public CreateCardLinkCommandValidator()
    {
    }
}
