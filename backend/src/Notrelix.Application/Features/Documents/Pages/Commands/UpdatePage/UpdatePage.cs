using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;

namespace Notrelix.Application.Features.Documents.Pages.Commands.UpdatePage;

public record UpdatePageCommand(
    Guid PageId,
    string? Title
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
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
        var page = await _context.Pages.FirstOrDefaultAsync(page => page.Id == request.PageId && !page.IsDeleted, ct);
        if (page is null) throw new NotFoundException(nameof(Page), request.PageId);

        if (request.Title is not null) page.Rename(request.Title, _currentUser.UserId, _dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
