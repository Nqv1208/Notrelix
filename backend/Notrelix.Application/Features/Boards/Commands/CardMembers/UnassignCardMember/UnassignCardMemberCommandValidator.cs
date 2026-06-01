using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.CardMembers.UnassignCardMember;

public class UnassignCardMemberCommandValidator : AbstractValidator<UnassignCardMemberCommand>
{
    public UnassignCardMemberCommandValidator()
    {
    }
}
