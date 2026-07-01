using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.ArchiveWorkspaceBySlug;

public record ArchiveWorkspaceBySlugCommand(string Slug) : ICommand<Result>, ITransactionalRequest;

public class ArchiveWorkspaceBySlugCommandHandler : IRequestHandler<ArchiveWorkspaceBySlugCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArchiveWorkspaceBySlugCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ArchiveWorkspaceBySlugCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Slug == request.Slug, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.Slug);

        workspace.Archive(_currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
