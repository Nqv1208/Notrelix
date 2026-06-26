using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Labels.Commands.CreateLabel;

public class CreateLabelCommandValidator : AbstractValidator<CreateLabelCommand>
{
    public CreateLabelCommandValidator()
    {
        RuleFor(x => x.BoardId)
            .NotEmpty();

        RuleFor(x => x.Color)
            .NotEmpty();

        RuleFor(x => x.Name)
            .MaximumLength(200);
    }
}
