using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class AssignCardMemberCommandValidator : AbstractValidator<AssignCardMemberCommand>
{
    public AssignCardMemberCommandValidator()
    {
    }
}
