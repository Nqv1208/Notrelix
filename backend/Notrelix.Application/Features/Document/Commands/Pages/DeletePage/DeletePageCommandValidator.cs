using FluentValidation;

namespace Notrelix.Application.Features.Document.Commands.Pages.DeletePage;

public class DeletePageCommandValidator : AbstractValidator<DeletePageCommand>
{
    public DeletePageCommandValidator()
    {
    }
}
