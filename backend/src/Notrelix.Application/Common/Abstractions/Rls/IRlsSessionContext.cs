using Microsoft.EntityFrameworkCore;

namespace Notrelix.Application.Common.Abstractions.Rls;

public interface IRlsSessionContext
{
    Task ApplyAsync(DbContext context, CancellationToken cancellationToken);
}
