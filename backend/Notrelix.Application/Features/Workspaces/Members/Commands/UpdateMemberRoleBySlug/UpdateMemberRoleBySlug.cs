using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMemberRoleBySlug;

public record UpdateMemberRoleBySlugCommand(
    string Slug,
    Guid UserId,
    string Role
) : IRequest<Result>;

public class UpdateMemberRoleBySlugCommandHandler : IRequestHandler<UpdateMemberRoleBySlugCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateMemberRoleBySlugCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateMemberRoleBySlugCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Slug == request.Slug, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.Slug);

        var newRole = Enum.Parse<WorkspaceRole>(request.Role, ignoreCase: true);
        workspace.UpdateMemberRole(request.UserId, newRole);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
