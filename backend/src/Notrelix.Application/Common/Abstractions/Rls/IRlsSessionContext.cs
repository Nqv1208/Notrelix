namespace Notrelix.Application.Common.Abstractions.Rls;

public interface IRlsSessionContext
{
    Task ApplyAsync(DatabaseFacade database, CancellationToken cancellationToken);
}
