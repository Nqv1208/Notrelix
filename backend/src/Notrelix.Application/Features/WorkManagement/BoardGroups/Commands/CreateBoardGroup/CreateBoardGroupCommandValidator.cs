using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.CreateBoardGroup;

public class CreateBoardGroupCommandValidator : AbstractValidator<CreateBoardGroupCommand>
{
    public CreateBoardGroupCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}
