using FluentValidation;

namespace Notrelix.Application.Features.Document.Commands.Pages.CreatePage;

public class CreatePageCommandValidator : AbstractValidator<CreatePageCommand>
{
    public CreatePageCommandValidator()
    {
    }
}
