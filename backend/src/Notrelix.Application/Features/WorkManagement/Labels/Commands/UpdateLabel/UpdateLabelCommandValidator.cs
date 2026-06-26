using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Labels.Commands.UpdateLabel;

public class UpdateLabelCommandValidator : AbstractValidator<UpdateLabelCommand>
{
    public UpdateLabelCommandValidator()
    {
        RuleFor(x => x.LabelId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .MaximumLength(200);

        RuleFor(x => x.Color)
            .MaximumLength(50);
    }
}
