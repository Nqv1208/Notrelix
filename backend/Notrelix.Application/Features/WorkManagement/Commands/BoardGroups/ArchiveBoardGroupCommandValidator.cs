using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class ArchiveListCommandValidator : AbstractValidator<ArchiveBoardGroupCommand>
{
    public ArchiveListCommandValidator()
    {
    }
}
