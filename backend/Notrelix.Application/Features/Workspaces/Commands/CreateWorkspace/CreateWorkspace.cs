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

namespace Notrelix.Application.Features.Workspaces.Commands.CreateWorkspace;

public record CreateWorkspaceCommand(
    string Name,
    string? Description,
    bool IsPersonal
) : IRequest<Result<Guid>>;

public class CreateWorkspaceCommandHandler : IRequestHandler<CreateWorkspaceCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateWorkspaceCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateWorkspaceCommand request, CancellationToken ct)
    {
        // Check for duplicate slug
        var slug = Domain.ValueObjects.Slug.GenerateFromName(request.Name);
        var slugExists = await _context.Workspaces
            .AnyAsync(w => w.Slug == slug, ct);

        var workspace = request.IsPersonal
            ? Workspace.CreatePersonal(request.Name, _currentUser.UserId)
            : Workspace.CreateTeam(request.Name, _currentUser.UserId, request.Description);

        // If slug already exists, append a random suffix
        if (slugExists)
        {
            workspace.UpdateSlug(slug + "-" + Guid.NewGuid().ToString("N")[..6]);
        }

        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(workspace.Id);
    }
}
