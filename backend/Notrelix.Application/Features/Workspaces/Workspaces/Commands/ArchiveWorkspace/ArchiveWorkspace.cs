using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.ArchiveWorkspace;

public record ArchiveWorkspaceCommand(Guid WorkspaceId) : IRequest<Result>;

public class ArchiveWorkspaceCommandHandler : IRequestHandler<ArchiveWorkspaceCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArchiveWorkspaceCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ArchiveWorkspaceCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        workspace.Archive(_currentUser.UserId, _dateTimeProvider.UtcNow);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
