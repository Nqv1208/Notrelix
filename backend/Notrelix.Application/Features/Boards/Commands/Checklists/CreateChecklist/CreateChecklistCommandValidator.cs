using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Checklists.CreateChecklist;

public class CreateChecklistCommandValidator : AbstractValidator<CreateChecklistCommand>
{
    public CreateChecklistCommandValidator()
    {
    }
}
