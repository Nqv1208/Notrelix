using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands.Labels.DeleteLabel;

public class DeleteLabelCommandValidator : AbstractValidator<DeleteLabelCommand>
{
    public DeleteLabelCommandValidator()
    {
    }
}
