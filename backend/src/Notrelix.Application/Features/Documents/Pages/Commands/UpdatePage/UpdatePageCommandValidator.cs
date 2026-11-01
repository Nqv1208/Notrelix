namespace Notrelix.Application.Features.Documents.Pages.Commands.UpdatePage;

public class UpdatePageCommandValidator : AbstractValidator<UpdatePageCommand>
{
    public UpdatePageCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
