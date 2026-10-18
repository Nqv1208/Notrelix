namespace Notrelix.Application.Features.Documents.Pages.Commands.MovePageToSpace;

public class MovePageToSpaceCommandValidator : AbstractValidator<MovePageToSpaceCommand>
{
    public MovePageToSpaceCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEqual(Guid.Empty).When(x => x.SpaceId.HasValue);
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
