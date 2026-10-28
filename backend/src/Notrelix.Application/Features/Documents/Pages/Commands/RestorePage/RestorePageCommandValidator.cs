namespace Notrelix.Application.Features.Documents.Pages.Commands.RestorePage;

public class RestorePageCommandValidator : AbstractValidator<RestorePageCommand>
{
    public RestorePageCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
    }
}
