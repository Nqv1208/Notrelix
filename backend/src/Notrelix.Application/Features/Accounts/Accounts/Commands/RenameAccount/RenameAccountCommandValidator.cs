namespace Notrelix.Application.Features.Accounts.Accounts.Commands.RenameAccount;

public class RenameAccountCommandValidator : AbstractValidator<RenameAccountCommand>
{
    public RenameAccountCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
    }
}
