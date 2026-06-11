using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class ReorderListsCommandValidator : AbstractValidator<ReorderBoardGroupsCommand>
{
    public ReorderListsCommandValidator()
    {
    }
}
