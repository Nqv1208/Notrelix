namespace Notrelix.Application.Common.CQRS.Security;

public interface IUseCaseSecurityRequirement
{
    UseCaseSecurityKind SecurityKind { get; }
}
