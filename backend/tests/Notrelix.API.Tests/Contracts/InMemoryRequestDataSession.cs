using Notrelix.Application.Common.Data;
using Notrelix.Infrastructure.Data;

namespace Notrelix.API.Tests.Contracts;

/// <summary>
/// API.Tests own public HTTP binding/error/idempotency/header contracts;
/// transaction, RLS and provider-specific mechanics belong to
/// Notrelix.Integration.Tests on real PostgreSQL. This session keeps the
/// seven-stage pipeline runnable against the In-Memory provider by executing
/// the handler action and persisting transactional side effects without any
/// relational-only operation.
/// </summary>
public sealed class InMemoryRequestDataSession(ApplicationDbContext dbContext) : IRequestDataSession
{
    public async Task<TResponse> ExecuteAsync<TResponse>(
        RequestDataSessionOptions options,
        Func<CancellationToken, Task<TResponse>> action,
        CancellationToken cancellationToken)
    {
        if (options.Access == RequestDataAccess.None)
        {
            return await action(cancellationToken);
        }

        var response = await action(cancellationToken);

        if (options.Access == RequestDataAccess.Transactional)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return response;
    }
}
