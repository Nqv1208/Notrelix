using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands.Checklists.CreateChecklistItem;

public class CreateChecklistItemCommandValidator : AbstractValidator<CreateChecklistItemCommand>
{
    public CreateChecklistItemCommandValidator()
    {
    }
}
