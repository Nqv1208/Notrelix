namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.ReorderBoardGroups;

public class ReorderBoardGroupsCommandValidator : AbstractValidator<ReorderBoardGroupsCommand>
{
    public ReorderBoardGroupsCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Items).NotNull().Must(items => items.Count > 0);
    }
}
