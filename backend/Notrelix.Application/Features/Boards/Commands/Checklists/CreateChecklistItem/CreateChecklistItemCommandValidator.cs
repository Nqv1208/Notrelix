using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Checklists.CreateChecklistItem;

public class CreateChecklistItemCommandValidator : AbstractValidator<CreateChecklistItemCommand>
{
    public CreateChecklistItemCommandValidator()
    {
    }
}
