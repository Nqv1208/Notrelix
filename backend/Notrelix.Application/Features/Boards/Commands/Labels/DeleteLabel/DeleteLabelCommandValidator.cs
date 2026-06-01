using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Labels.DeleteLabel;

public class DeleteLabelCommandValidator : AbstractValidator<DeleteLabelCommand>
{
    public DeleteLabelCommandValidator()
    {
    }
}
