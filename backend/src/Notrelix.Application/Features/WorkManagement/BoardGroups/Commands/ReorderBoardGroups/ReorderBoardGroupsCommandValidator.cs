using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.ReorderBoardGroups;

public class ReorderListsCommandValidator : AbstractValidator<ReorderBoardGroupsCommand>
{
    public ReorderListsCommandValidator()
    {
    }
}
