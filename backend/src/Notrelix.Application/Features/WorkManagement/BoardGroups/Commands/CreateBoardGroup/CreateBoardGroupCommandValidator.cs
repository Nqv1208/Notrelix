using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.CreateBoardGroup;

public class CreateListCommandValidator : AbstractValidator<CreateBoardGroupCommand>
{
    public CreateListCommandValidator()
    {
    }
}
