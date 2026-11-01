namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UnarchiveBoardGroup;

public class UnarchiveBoardGroupCommandValidator : AbstractValidator<UnarchiveBoardGroupCommand>
{
    public UnarchiveBoardGroupCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.GroupId).NotEmpty();
    }
}
