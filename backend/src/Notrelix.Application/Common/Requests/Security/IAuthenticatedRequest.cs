namespace Notrelix.Application.Common.Requests.Security;

public interface IAuthenticatedRequest : IUseCaseSecurityRequirement
{
    UseCaseSecurityKind IUseCaseSecurityRequirement.SecurityKind => UseCaseSecurityKind.AuthenticatedUser;
}
