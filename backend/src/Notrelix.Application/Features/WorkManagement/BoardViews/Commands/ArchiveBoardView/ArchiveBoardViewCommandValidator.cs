namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.ArchiveBoardView;

public class ArchiveBoardViewCommandValidator : AbstractValidator<ArchiveBoardViewCommand>
{
    public ArchiveBoardViewCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.ViewId).NotEmpty();
    }
}
