namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.MoveBoardField;

public class MoveBoardFieldCommandValidator : AbstractValidator<MoveBoardFieldCommand>
{
    public MoveBoardFieldCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
