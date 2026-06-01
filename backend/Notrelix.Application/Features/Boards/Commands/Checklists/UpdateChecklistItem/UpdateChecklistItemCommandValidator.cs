using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Checklists.UpdateChecklistItem;

public class UpdateChecklistItemCommandValidator : AbstractValidator<UpdateChecklistItemCommand>
{
    public UpdateChecklistItemCommandValidator()
    {
    }
}
