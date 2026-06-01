using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Workspaces.Commands.RemoveMemberBySlug;

public record RemoveMemberBySlugCommand(
    string Slug,
    Guid UserId
) : IRequest<Result>;

public class RemoveMemberBySlugCommandHandler : IRequestHandler<RemoveMemberBySlugCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public RemoveMemberBySlugCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(RemoveMemberBySlugCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Slug == request.Slug, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.Slug);

        workspace.RemoveMember(request.UserId);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
