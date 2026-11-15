namespace Notrelix.Application.Features.Documents.Pages.Commands.UpdatePage;

public class UpdatePageCommandValidator : AbstractValidator<UpdatePageCommand>
{
    public UpdatePageCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500).When(x => x.Title is not null);
        RuleFor(x => x.Icon).MaximumLength(32).When(x => x.Icon is not null);
        RuleFor(x => x).Must(x => x.Title is not null || x.Icon is not null).WithMessage("A title or an icon is required.");
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
