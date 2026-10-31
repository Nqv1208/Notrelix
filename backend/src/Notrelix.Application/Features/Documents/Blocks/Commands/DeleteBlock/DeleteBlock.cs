using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Services;
using Notrelix.Domain.Documents.Rules;

namespace Notrelix.Application.Features.Documents.Blocks.Commands.DeleteBlock;

/// <summary>
/// Deletes a block and every block under it, against the version the editor read: the same result as a batch
/// delete. A stale <c>ExpectedVersion</c> fails with 412 carrying the current block; a block that is already
/// deleted is 404.
/// </summary>
public record DeleteBlockCommand(Guid PageId, Guid BlockId, long? ExpectedVersion) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.UpdatePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class DeleteBlockCommandHandler : IRequestHandler<DeleteBlockCommand, Result>
{
    private readonly IDocumentDbContext _context;
    private readonly IPageBlockWriteLock _writeLock;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    public DeleteBlockCommandHandler(IDocumentDbContext context, IPageBlockWriteLock writeLock, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _writeLock = writeLock;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteBlockCommand request, CancellationToken ct)
    {
        await _writeLock.AcquireAsync(request.PageId, ct);
        var page = await _context.Pages.FirstOrDefaultAsync(p => p.Id == request.PageId && p.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(Page), request.PageId);
        PageRules.EnsureCanEdit(page.Status);

        var live = await _context.Blocks
            .Where(b => b.PageId == request.PageId && b.DeletedAt == null)
            .ToListAsync(ct);
        var set = new PageBlockSet(page.AccountId, page.WorkspaceId, request.PageId, live);
        var block = set.Find(request.BlockId) ?? throw new NotFoundException(nameof(Block), request.BlockId);
        PageBlockSet.EnsureVersion(block, request.ExpectedVersion);

        var now = _dateTimeProvider.UtcNow;
        set.Delete(block, _currentUser.UserId, now);
        var tracker = new BlockSaveTracker();
        tracker.Deleted();
        tracker.Complete(page, [], _currentUser.UserId, now);
        return Result.Success();
    }
}
