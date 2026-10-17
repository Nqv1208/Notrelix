namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.MoveFieldOption;

public class MoveFieldOptionCommandValidator : AbstractValidator<MoveFieldOptionCommand>
{
    public MoveFieldOptionCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
