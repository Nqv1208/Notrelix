using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklistItem;

public class CreateChecklistItemCommandValidator : AbstractValidator<CreateChecklistItemCommand>
{
    public CreateChecklistItemCommandValidator()
    {
    }
}
