using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.ArchiveBoardGroup;

public class ArchiveListCommandValidator : AbstractValidator<ArchiveBoardGroupCommand>
{
    public ArchiveListCommandValidator()
    {
    }
}
