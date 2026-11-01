namespace Notrelix.Application.Features.Documents.Pages.Commands.MovePage;

public class MovePageCommandValidator : AbstractValidator<MovePageCommand>
{
    public MovePageCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
        RuleFor(x => x.NewParentId).NotEqual(Guid.Empty).When(x => x.NewParentId.HasValue);
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
