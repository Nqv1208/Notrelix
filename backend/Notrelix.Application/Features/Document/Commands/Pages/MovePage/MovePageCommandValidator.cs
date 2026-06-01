using FluentValidation;

namespace Notrelix.Application.Features.Document.Commands.Pages.MovePage;

public class MovePageCommandValidator : AbstractValidator<MovePageCommand>
{
    public MovePageCommandValidator()
    {
    }
}
