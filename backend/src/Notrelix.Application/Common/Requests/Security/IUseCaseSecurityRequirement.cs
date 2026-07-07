namespace Notrelix.Application.Common.Requests.Security;

public interface IUseCaseSecurityRequirement
{
    UseCaseSecurityKind SecurityKind { get; }
}
