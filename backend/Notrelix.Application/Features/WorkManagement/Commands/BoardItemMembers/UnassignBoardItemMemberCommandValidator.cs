using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class UnassignCardMemberCommandValidator : AbstractValidator<UnassignCardMemberCommand>
{
    public UnassignCardMemberCommandValidator()
    {
    }
}
