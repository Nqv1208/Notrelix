using Notrelix.Application.Features.Documents.Blocks.Services;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>The page block lock for handler tests that run without a database transaction.</summary>
internal sealed class NoPageBlockWriteLock : IPageBlockWriteLock
{
    public Task AcquireAsync(Guid pageId, CancellationToken cancellationToken) => Task.CompletedTask;
}
