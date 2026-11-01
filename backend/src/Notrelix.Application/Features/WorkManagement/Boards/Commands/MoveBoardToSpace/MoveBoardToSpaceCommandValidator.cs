namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.MoveBoardToSpace;

public class MoveBoardToSpaceCommandValidator : AbstractValidator<MoveBoardToSpaceCommand>
{
    public MoveBoardToSpaceCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEqual(Guid.Empty).When(x => x.SpaceId.HasValue);
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
        RuleFor(x => x.AddUserIds!.Count).LessThanOrEqualTo(200).When(x => x.AddUserIds is not null);
        RuleForEach(x => x.AddUserIds!).NotEmpty().When(x => x.AddUserIds is not null);
    }
}
