namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.MoveBoardGroup;

public class MoveBoardGroupCommandValidator : AbstractValidator<MoveBoardGroupCommand>
{
    public MoveBoardGroupCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
