namespace Notrelix.Application.Common.Requests;

public interface ITokenScopedRequest : IUseCaseSecurityRequirement
{
    TokenPurpose TokenPurpose { get; }

    UseCaseSecurityKind IUseCaseSecurityRequirement.SecurityKind =>
        UseCaseSecurityKind.TokenScoped;
}
