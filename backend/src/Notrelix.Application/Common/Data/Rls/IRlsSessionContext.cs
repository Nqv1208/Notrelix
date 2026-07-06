namespace Notrelix.Application.Common.Data.Rls;

public interface IRlsSessionContext
{
    Task ApplyAsync(DatabaseFacade database, CancellationToken cancellationToken);
}
