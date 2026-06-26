using FluentValidation;

namespace Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;

public class CreatePageCommandValidator : AbstractValidator<CreatePageCommand>
{
    public CreatePageCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}
