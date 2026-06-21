using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ArchiveBoardItem;

public class ArchiveCardCommandValidator : AbstractValidator<ArchiveBoardItemCommand>
{
    public ArchiveCardCommandValidator()
    {
    }
}
