namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UpdateBoardGroup;

public class UpdateBoardGroupCommandValidator : AbstractValidator<UpdateBoardGroupCommand>
{
    public UpdateBoardGroupCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.Title).MaximumLength(200);
    }
}
