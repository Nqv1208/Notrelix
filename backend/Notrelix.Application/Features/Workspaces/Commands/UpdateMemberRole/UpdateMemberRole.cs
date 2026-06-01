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

namespace Notrelix.Application.Features.Workspaces.Commands.UpdateMemberRole;

public record UpdateMemberRoleCommand(
    Guid WorkspaceId,
    Guid UserId,
    string Role
) : IRequest<Result>;

public class UpdateMemberRoleCommandHandler : IRequestHandler<UpdateMemberRoleCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateMemberRoleCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateMemberRoleCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && !w.IsArchived, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var newRole = Enum.Parse<WorkspaceRole>(request.Role, ignoreCase: true);
        workspace.UpdateMemberRole(request.UserId, newRole);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
