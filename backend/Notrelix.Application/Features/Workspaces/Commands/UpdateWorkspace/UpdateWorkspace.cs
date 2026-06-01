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

namespace Notrelix.Application.Features.Workspaces.Commands.UpdateWorkspace;

public record UpdateWorkspaceCommand(
    Guid WorkspaceId,
    string? Name,
    string? Description,
    string? IconType,
    string? IconValue
) : IRequest<Result>;

public class UpdateWorkspaceCommandHandler : IRequestHandler<UpdateWorkspaceCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateWorkspaceCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateWorkspaceCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        if (request.Name is not null) workspace.UpdateName(request.Name);
        if (request.Description is not null) workspace.UpdateDescription(request.Description);
        if (request.IconType is not null && request.IconValue is not null)
        {
            var iconType = Enum.Parse<Domain.Enums.IconType>(request.IconType, ignoreCase: true);
            var icon = iconType == Domain.Enums.IconType.Emoji
                ? Domain.ValueObjects.Icon.FromEmoji(request.IconValue)
                : Domain.ValueObjects.Icon.FromName(request.IconValue);
            workspace.UpdateIcon(icon);
        }

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
