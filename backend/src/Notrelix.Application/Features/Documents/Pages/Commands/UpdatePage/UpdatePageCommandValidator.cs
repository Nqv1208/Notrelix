using FluentValidation;

namespace Notrelix.Application.Features.Documents.Pages.Commands.UpdatePage;

public class UpdatePageCommandValidator : AbstractValidator<UpdatePageCommand>
{
    public UpdatePageCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
        RuleFor(x => x.Title).MaximumLength(200);
    }
}
