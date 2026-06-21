using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UpdateBoardGroup;

public class UpdateListCommandValidator : AbstractValidator<UpdateBoardGroupCommand>
{
    public UpdateListCommandValidator()
    {
    }
}
