using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.CardMembers.AssignCardMember;

public class AssignCardMemberCommandValidator : AbstractValidator<AssignCardMemberCommand>
{
    public AssignCardMemberCommandValidator()
    {
    }
}
