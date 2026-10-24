using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;

namespace Notrelix.Application.Features.Identity.Auth.Queries.IdentifySignIn;

/// <summary>How the sign-in page continues for an email.</summary>
public static class SignInMethods
{
    public const string Password = "password";
    public const string Sso = "sso";
}

/// <summary>The company provider an email signs in through; present only for <see cref="SignInMethods.Sso"/>.</summary>
public sealed record SsoSignInDto(
    string OrgName,
    string OrgInitials,
    string Provider,
    string ProviderLabel);

/// <summary>
/// The answer is a function of the email's text alone, never of whether an account exists, so an
/// unknown email and a known password email are indistinguishable (no enumeration, no timing
/// difference).
/// </summary>
public sealed record SignInMethodDto(string Method, SsoSignInDto? Sso);

/// <summary>
/// Tells the sign-in page whether an email continues with a password or with its company provider.
/// Single sign-on is offered only for an address whose domain an Account has verified and whose
/// identity provider enforces it. No Account fact states that enforcement yet (the identity provider
/// has only a Draft/Active/Disabled status and no enforcement flag, and no company sign-in round trip
/// exists), so every email continues with a password until that product decision is taken.
/// </summary>
public sealed record IdentifySignInQuery(string Email)
    : IQuery<Result<SignInMethodDto>>, IAnonymousRequest, IGlobalRequest, INoDataRequest;

public sealed class IdentifySignInQueryHandler : IRequestHandler<IdentifySignInQuery, Result<SignInMethodDto>>
{
    public Task<Result<SignInMethodDto>> Handle(IdentifySignInQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result<SignInMethodDto>.Success(new SignInMethodDto(SignInMethods.Password, null)));
}

public sealed class IdentifySignInQueryValidator : AbstractValidator<IdentifySignInQuery>
{
    public IdentifySignInQueryValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(256)
            .EmailAddress();
    }
}
