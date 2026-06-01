using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Labels.CreateLabel;

public class CreateLabelCommandValidator : AbstractValidator<CreateLabelCommand>
{
    public CreateLabelCommandValidator()
    {
    }
}
