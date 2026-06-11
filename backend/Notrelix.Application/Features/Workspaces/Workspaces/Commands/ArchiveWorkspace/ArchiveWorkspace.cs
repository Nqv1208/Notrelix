using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.ArchiveWorkspace;

public record ArchiveWorkspaceCommand(Guid WorkspaceId) : IRequest<Result>;

public class ArchiveWorkspaceCommandHandler : IRequestHandler<ArchiveWorkspaceCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public ArchiveWorkspaceCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(ArchiveWorkspaceCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        workspace.Archive();
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
