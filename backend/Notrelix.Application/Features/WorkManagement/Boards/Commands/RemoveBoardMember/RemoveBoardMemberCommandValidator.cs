using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.RemoveBoardMember;

public class RemoveBoardMemberCommandValidator : AbstractValidator<RemoveBoardMemberCommand>
{
    public RemoveBoardMemberCommandValidator()
    {
    }
}
