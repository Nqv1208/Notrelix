using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.AssignBoardItemMember;

public class AssignCardMemberCommandValidator : AbstractValidator<AssignCardMemberCommand>
{
    public AssignCardMemberCommandValidator()
    {
    }
}
