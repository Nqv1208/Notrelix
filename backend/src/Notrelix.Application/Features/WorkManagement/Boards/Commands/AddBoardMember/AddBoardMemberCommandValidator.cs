using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.AddBoardMember;

public class AddBoardMemberCommandValidator : AbstractValidator<AddBoardMemberCommand>
{
    public AddBoardMemberCommandValidator()
    {
    }
}
