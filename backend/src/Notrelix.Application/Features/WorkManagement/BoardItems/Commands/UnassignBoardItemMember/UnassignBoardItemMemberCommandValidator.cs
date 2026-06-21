using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnassignBoardItemMember;

public class UnassignCardMemberCommandValidator : AbstractValidator<UnassignCardMemberCommand>
{
    public UnassignCardMemberCommandValidator()
    {
    }
}
