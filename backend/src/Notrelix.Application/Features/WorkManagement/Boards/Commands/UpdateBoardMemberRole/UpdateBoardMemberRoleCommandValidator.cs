namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoardMemberRole;

public class UpdateBoardMemberRoleCommandValidator : AbstractValidator<UpdateBoardMemberRoleCommand>
{
    public UpdateBoardMemberRoleCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.Role).IsInEnum();
    }
}
