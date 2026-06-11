using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.UpdateWorkspace;

public record UpdateWorkspaceCommand(
    Guid WorkspaceId,
    string? Name,
    string? Description,
    string? IconType,
    string? IconValue,
    string? Settings
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
            var iconType = Enum.Parse<IconType>(request.IconType, ignoreCase: true);
            var icon = iconType == IconType.Emoji
                ? Domain.ValueObjects.Icon.FromEmoji(request.IconValue)
                : Domain.ValueObjects.Icon.FromName(request.IconValue);
            workspace.UpdateIcon(icon);
        }
        if (request.Settings is not null) workspace.UpdateSettings(request.Settings);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
