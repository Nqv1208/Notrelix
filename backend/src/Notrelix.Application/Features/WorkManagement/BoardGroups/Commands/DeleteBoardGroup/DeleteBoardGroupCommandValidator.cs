namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DeleteBoardGroup;

public class DeleteBoardGroupCommandValidator : AbstractValidator<DeleteBoardGroupCommand>
{
    public DeleteBoardGroupCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
    }
}
