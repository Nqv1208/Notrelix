namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.ArchiveBoard;

public class ArchiveBoardCommandValidator : AbstractValidator<ArchiveBoardCommand>
{
    public ArchiveBoardCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
    }
}
