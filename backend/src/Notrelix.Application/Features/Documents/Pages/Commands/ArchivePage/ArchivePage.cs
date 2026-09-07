using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;

namespace Notrelix.Application.Features.Documents.Pages.Commands.ArchivePage;

public record ArchivePageCommand(Guid PageId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ArchivePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class ArchivePageCommandHandler : IRequestHandler<ArchivePageCommand, Result>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArchivePageCommandHandler(IDocumentDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ArchivePageCommand request, CancellationToken ct)
    {
        // DeletedAt is the mapped soft-delete column on Page (IsDeleted is a
        // Domain-only flag that is deliberately ignored in EF mapping).
        var page = await _context.Pages.FirstOrDefaultAsync(page => page.Id == request.PageId && page.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(Page), request.PageId);

        // Page.Archive authority owns the lifecycle: an already-archived page
        // is a semantic no-op (no version bump, no second event).
        page.Archive(_requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
