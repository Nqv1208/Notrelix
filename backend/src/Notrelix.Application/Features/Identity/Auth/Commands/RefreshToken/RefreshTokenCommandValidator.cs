namespace Notrelix.Application.Features.Identity.Auth.Commands.RefreshToken;

// Validator cho RefreshTokenCommand
public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
