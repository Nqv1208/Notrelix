using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;

namespace Notrelix.Application.Features.Documents.Pages.Commands.RestorePage;

/// <summary>
/// Takes a page out of the trash. Same authority as moving it there
/// (<c>ArchivePage</c>, Editor level on the page).
/// </summary>
public record RestorePageCommand(Guid PageId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ArchivePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class RestorePageCommandHandler : IRequestHandler<RestorePageCommand, Result>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RestorePageCommandHandler(IDocumentDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RestorePageCommand request, CancellationToken ct)
    {
        var page = await _context.Pages.FirstOrDefaultAsync(page => page.Id == request.PageId && page.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(Page), request.PageId);

        // Page.Unarchive owns the lifecycle: an active page is a semantic no-op.
        page.Unarchive(_requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
