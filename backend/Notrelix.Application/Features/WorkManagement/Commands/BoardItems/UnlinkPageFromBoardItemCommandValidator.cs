using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class UnlinkPageFromCardCommandValidator : AbstractValidator<UnlinkPageFromBoardItemCommand>
{
    public UnlinkPageFromCardCommandValidator()
    {
    }
}
