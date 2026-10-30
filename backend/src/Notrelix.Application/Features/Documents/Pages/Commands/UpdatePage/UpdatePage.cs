using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;

namespace Notrelix.Application.Features.Documents.Pages.Commands.UpdatePage;

/// <summary>
/// Renames a page. Authorized as <c>ManageBoard</c> on the page (as before; aligning it with the edit right
/// <c>UpdatePage</c> waits for the owner) and guarded by the page version the editor read: a stale version is
/// refused (412) and changes nothing. The same title is a semantic no-op (no version bump, no event); a
/// trashed page cannot be renamed.
/// </summary>
public record UpdatePageCommand(
    Guid PageId,
    string Title,
    long ExpectedVersion
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class UpdatePageCommandHandler : IRequestHandler<UpdatePageCommand, Result>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    public UpdatePageCommandHandler(IDocumentDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdatePageCommand request, CancellationToken ct)
    {
        var page = await _context.Pages.FirstOrDefaultAsync(page => page.Id == request.PageId && page.DeletedAt == null, ct);
        if (page is null) throw new NotFoundException(nameof(Page), request.PageId);

        page.Rename(request.Title, _currentUser.UserId, _dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
