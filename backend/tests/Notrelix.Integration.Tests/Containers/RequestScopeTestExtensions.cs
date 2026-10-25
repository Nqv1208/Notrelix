using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Integration.Tests.Containers;

/// <summary>
/// Lets a test that drives a scoped <see cref="ApplicationDbContext"/> directly (outside the request
/// pipeline) run the way a request does on the application login: inside a transaction that carries the
/// tenant scope row-level security reads.
/// </summary>
internal static class RequestScopeTestExtensions
{
    /// <summary>
    /// Opens the transaction a request would run in and applies the scope of the scope's current tenant
    /// context to it. Set the tenant before calling. Dispose the result to roll back, or commit it.
    /// </summary>
    public static async Task<IDbContextTransaction> BeginTenantTransactionAsync(
        this IServiceProvider scopeServices, CancellationToken cancellationToken = default)
    {
        var context = scopeServices.GetRequiredService<ApplicationDbContext>();
        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await scopeServices.GetRequiredService<IRlsSessionContext>().ApplyAsync(cancellationToken);
        return transaction;
    }
}
