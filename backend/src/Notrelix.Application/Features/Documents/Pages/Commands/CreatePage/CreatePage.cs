using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;

public record CreatePageCommand(
    Guid WorkspaceId,
    string Title,
    Guid? ParentId
) : ICommand<Result<Guid>>, ITransactionalRequest;

public class CreatePageCommandHandler : IRequestHandler<CreatePageCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreatePageCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreatePageCommand request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces.AsNoTracking()
            .AnyAsync(workspace => workspace.Id == request.WorkspaceId && workspace.Status == WorkspaceStatus.Active && !workspace.IsDeleted, ct);
        if (!workspaceExists) throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var page = Page.Create(Guid.Empty, request.WorkspaceId, request.Title, _currentUser.UserId, _dateTimeProvider.UtcNow, request.ParentId);
        _context.Pages.Add(page);

        return Result<Guid>.Success(page.Id);
    }
}
