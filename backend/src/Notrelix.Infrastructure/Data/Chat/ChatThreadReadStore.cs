using Notrelix.Application.Features.Chat.Abstractions;

namespace Notrelix.Infrastructure.Data.Chat;

/// <summary>
/// The thread read pointer in Postgres (UC-CHT-F08). One UPDATE on the caller's own follow row sets the pointer to
/// GREATEST(current, target), so two requests for one thread cannot move it backwards. The statement runs on the request's
/// transaction and under the Workspace's row policy.
/// </summary>
public sealed class ChatThreadReadStore(ApplicationDbContext db) : IChatThreadReadStore
{
    public async Task<bool> AdvanceAsync(Guid workspaceId, Guid rootMessageId, Guid userId, long target, CancellationToken cancellationToken)
    {
        var touched = await db.ChatThreadFollowers
            .Where(f => f.RootMessageId == rootMessageId && f.UserId == userId && f.WorkspaceId == workspaceId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(f => f.ReadReplySequence, f => Math.Max(f.ReadReplySequence, target)),
                cancellationToken);
        return touched > 0;
    }
}
