namespace Notrelix.Application.Common.Requests.Security;

public interface IAnonymousRequest : IUseCaseSecurityRequirement
{
    UseCaseSecurityKind IUseCaseSecurityRequirement.SecurityKind => UseCaseSecurityKind.Anonymous;
}
