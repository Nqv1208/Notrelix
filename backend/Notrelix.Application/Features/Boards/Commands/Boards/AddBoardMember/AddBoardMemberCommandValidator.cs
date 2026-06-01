using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Boards.AddBoardMember;

public class AddBoardMemberCommandValidator : AbstractValidator<AddBoardMemberCommand>
{
    public AddBoardMemberCommandValidator()
    {
    }
}
