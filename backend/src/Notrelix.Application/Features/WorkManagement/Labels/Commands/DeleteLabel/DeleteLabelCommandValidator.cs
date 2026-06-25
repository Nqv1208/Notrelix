using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Labels.Commands.DeleteLabel;

public class DeleteLabelCommandValidator : AbstractValidator<DeleteLabelCommand>
{
    public DeleteLabelCommandValidator()
    {
        RuleFor(x => x.LabelId)
            .NotEmpty();
    }
}
