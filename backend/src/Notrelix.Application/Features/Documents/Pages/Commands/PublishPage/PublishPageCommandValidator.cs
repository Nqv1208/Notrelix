using FluentValidation;

namespace Notrelix.Application.Features.Documents.Pages.Commands.PublishPage;

public class PublishPageCommandValidator : AbstractValidator<PublishPageCommand>
{
    public PublishPageCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
    }
}
