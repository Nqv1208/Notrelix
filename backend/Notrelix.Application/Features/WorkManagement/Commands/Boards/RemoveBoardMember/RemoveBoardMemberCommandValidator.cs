using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands.Boards.RemoveBoardMember;

public class RemoveBoardMemberCommandValidator : AbstractValidator<RemoveBoardMemberCommand>
{
    public RemoveBoardMemberCommandValidator()
    {
    }
}
