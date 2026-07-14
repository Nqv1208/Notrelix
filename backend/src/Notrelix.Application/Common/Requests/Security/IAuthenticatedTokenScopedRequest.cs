namespace Notrelix.Application.Common.Requests.Security;

public interface IAuthenticatedTokenScopedRequest : IAuthenticatedRequest, ITokenScopedRequest
{
    UseCaseSecurityKind IUseCaseSecurityRequirement.SecurityKind =>
        UseCaseSecurityKind.AuthenticatedUser;
}
