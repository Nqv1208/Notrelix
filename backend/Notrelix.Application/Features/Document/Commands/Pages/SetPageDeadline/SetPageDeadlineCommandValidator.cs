using FluentValidation;

namespace Notrelix.Application.Features.Document.Commands.Pages.SetPageDeadline;

public class SetPageDeadlineCommandValidator : AbstractValidator<SetPageDeadlineCommand>
{
    public SetPageDeadlineCommandValidator()
    {
    }
}
