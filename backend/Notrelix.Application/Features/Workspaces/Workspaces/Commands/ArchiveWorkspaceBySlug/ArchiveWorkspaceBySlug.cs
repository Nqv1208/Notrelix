using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.ArchiveWorkspaceBySlug;

public record ArchiveWorkspaceBySlugCommand(string Slug) : IRequest<Result>;

public class ArchiveWorkspaceBySlugCommandHandler : IRequestHandler<ArchiveWorkspaceBySlugCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public ArchiveWorkspaceBySlugCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(ArchiveWorkspaceBySlugCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Slug == request.Slug, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.Slug);

        workspace.Archive();
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
