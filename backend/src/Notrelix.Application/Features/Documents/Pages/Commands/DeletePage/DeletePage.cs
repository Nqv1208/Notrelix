using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;

namespace Notrelix.Application.Features.Documents.Pages.Commands.DeletePage;

/// <summary>
/// Deletes a page for good. Only a page already in the trash can be deleted,
/// and only by a Manager of the page (<c>DeletePage</c>).
/// </summary>
public record DeletePageCommand(Guid PageId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.DeletePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class DeletePageCommandHandler : IRequestHandler<DeletePageCommand, Result>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    public DeletePageCommandHandler(IDocumentDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeletePageCommand request, CancellationToken ct)
    {
        var page = await _context.Pages.FirstOrDefaultAsync(page => page.Id == request.PageId && page.DeletedAt == null, ct);
        if (page is null) throw new NotFoundException(nameof(Page), request.PageId);

        if (page.Status != PageStatus.Archived)
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(Notrelix.Domain.Documents.DocumentRuleCodes.Documents_Page_NotInTrash, "Only a page in the trash can be deleted for good.");

        page.Delete(_currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
