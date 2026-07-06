namespace Notrelix.Application.Common.CQRS.Security;

public interface IAnonymousRequest : IUseCaseSecurityRequirement
{
    UseCaseSecurityKind IUseCaseSecurityRequirement.SecurityKind => UseCaseSecurityKind.Anonymous;
}
