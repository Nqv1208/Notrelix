using FluentValidation;

namespace Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;

public class CreatePageCommandValidator : AbstractValidator<CreatePageCommand>
{
    public CreatePageCommandValidator()
    {
    }
}
