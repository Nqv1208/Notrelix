using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Services;

namespace Notrelix.Application.Features.Documents.Blocks.Commands.DeleteBlock;

public record DeleteBlockCommand(Guid PageId, Guid BlockId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.UpdatePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class DeleteBlockCommandHandler : IRequestHandler<DeleteBlockCommand, Result>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    public DeleteBlockCommandHandler(IDocumentDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteBlockCommand request, CancellationToken ct)
    {
        var block = await _context.Blocks.FirstOrDefaultAsync(block => block.Id == request.BlockId && block.PageId == request.PageId && block.DeletedAt == null, ct);
        if (block is null) throw new NotFoundException(nameof(Block), request.BlockId);

        var page = await _context.Pages.FirstOrDefaultAsync(p => p.Id == request.PageId && p.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(Page), request.PageId);
        var now = _dateTimeProvider.UtcNow;
        block.Delete(_currentUser.UserId, now);
        var tracker = new BlockSaveTracker();
        tracker.Deleted();
        tracker.Complete(page, [], _currentUser.UserId, now);
        return Result.Success();
    }
}
