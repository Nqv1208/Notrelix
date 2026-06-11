using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class ArchiveCardCommandValidator : AbstractValidator<ArchiveBoardItemCommand>
{
    public ArchiveCardCommandValidator()
    {
    }
}
