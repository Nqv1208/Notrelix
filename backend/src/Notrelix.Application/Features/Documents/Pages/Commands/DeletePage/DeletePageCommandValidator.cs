using FluentValidation;

namespace Notrelix.Application.Features.Documents.Pages.Commands.DeletePage;

public class DeletePageCommandValidator : AbstractValidator<DeletePageCommand>
{
    public DeletePageCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
    }
}
