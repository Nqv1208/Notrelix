using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands.Boards.AddBoardMember;

public class AddBoardMemberCommandValidator : AbstractValidator<AddBoardMemberCommand>
{
    public AddBoardMemberCommandValidator()
    {
    }
}
