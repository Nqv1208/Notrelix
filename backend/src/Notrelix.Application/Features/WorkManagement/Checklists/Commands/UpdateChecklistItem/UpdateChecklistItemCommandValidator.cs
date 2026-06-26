using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklistItem;

public class UpdateChecklistItemCommandValidator : AbstractValidator<UpdateChecklistItemCommand>
{
    public UpdateChecklistItemCommandValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty();
    }
}
