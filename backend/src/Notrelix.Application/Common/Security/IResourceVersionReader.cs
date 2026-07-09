namespace Notrelix.Application.Common.Security;

public interface IResourceVersionReader
{
    Task<long?> GetVersionAsync(ResourceRef resource, CancellationToken cancellationToken);
}
