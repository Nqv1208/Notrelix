using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Accounts.Public.Sso;

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
    string ProviderLabel,
    bool Enforced);

/// <summary>
/// The answer is a function of the email's domain alone, never of whether a person exists, so an unknown email
/// and a known password email are indistinguishable (no enumeration), and so are an unverified domain, a domain
/// nobody claimed and an Account without an Active provider.
/// </summary>
public sealed record SignInMethodDto(string Method, SsoSignInDto? Sso);

/// <summary>
/// Tells the sign-in page whether an email continues with a password or with its company provider. Single sign-on
/// is offered only for an address whose domain an Account has verified and whose OIDC provider is Active.
/// </summary>
public sealed record IdentifySignInQuery(string Email)
    : IQuery<Result<SignInMethodDto>>, IAnonymousRequest, IGlobalRequest, IReadRequest;

public sealed class IdentifySignInQueryHandler : IRequestHandler<IdentifySignInQuery, Result<SignInMethodDto>>
{
    public const string OidcProvider = "oidc";

    private readonly IAccountSsoFacts _facts;

    public IdentifySignInQueryHandler(IAccountSsoFacts facts) => _facts = facts;

    public async Task<Result<SignInMethodDto>> Handle(IdentifySignInQuery request, CancellationToken cancellationToken)
    {
        var route = await _facts.FindRouteAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (route is null)
            return Result<SignInMethodDto>.Success(new SignInMethodDto(SignInMethods.Password, null));

        return Result<SignInMethodDto>.Success(new SignInMethodDto(
            SignInMethods.Sso,
            new SsoSignInDto(route.AccountName, Initials(route.AccountName), OidcProvider, route.ProviderName, route.Enforced)));
    }

    /// <summary>Up to two capital letters from the first words of the name ("Acme Corp" gives "AC").</summary>
    private static string Initials(string name)
    {
        var letters = name.Split([' ', '-', '_', '.'], StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.FirstOrDefault(char.IsLetterOrDigit))
            .Where(letter => letter != default)
            .Take(2)
            .ToArray();
        return letters.Length == 0 ? "?" : new string(letters).ToUpperInvariant();
    }
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
