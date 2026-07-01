namespace Notrelix.Application.Features.Documents.Pages.Commands.SetPageDeadline;

public class SetPageDeadlineCommandValidator : AbstractValidator<SetPageDeadlineCommand>
{
    public SetPageDeadlineCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
    }
}
