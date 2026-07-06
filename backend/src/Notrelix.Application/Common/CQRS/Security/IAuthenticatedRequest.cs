namespace Notrelix.Application.Common.CQRS.Security;

public interface IAuthenticatedRequest : IUseCaseSecurityRequirement
{
    UseCaseSecurityKind IUseCaseSecurityRequirement.SecurityKind => UseCaseSecurityKind.AuthenticatedUser;
}
