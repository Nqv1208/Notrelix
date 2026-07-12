namespace Notrelix.Application.Common.Requests.Security;

public interface IAnonymousTokenScopedRequest : IAnonymousRequest, ITokenScopedRequest
{
    UseCaseSecurityKind IUseCaseSecurityRequirement.SecurityKind =>
        UseCaseSecurityKind.Anonymous;
}
