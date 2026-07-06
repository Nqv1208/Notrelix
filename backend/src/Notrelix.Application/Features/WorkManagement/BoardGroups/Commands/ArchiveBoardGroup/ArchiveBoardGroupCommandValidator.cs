namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.ArchiveBoardGroup;

public class ArchiveBoardGroupCommandValidator : AbstractValidator<ArchiveBoardGroupCommand>
{
    public ArchiveBoardGroupCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
    }
}
