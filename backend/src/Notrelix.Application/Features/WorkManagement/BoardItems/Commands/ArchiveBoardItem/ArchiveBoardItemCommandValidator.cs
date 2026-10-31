namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ArchiveBoardItem;

public class ArchiveBoardItemCommandValidator : AbstractValidator<ArchiveBoardItemCommand>
{
    public ArchiveBoardItemCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.BoardItemId).NotEmpty();
    }
}
