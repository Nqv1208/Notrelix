using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Entities.Workspaces;
using Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Workspaces.Commands;

// ── CreateWorkspace ──────────────────────────────────────────
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

// ── UpdateWorkspace ──────────────────────────────────────────
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

// ── ArchiveWorkspace (by ID) ─────────────────────────────────
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

// ── ArchiveWorkspace (by Slug) ───────────────────────────────
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

// ── InviteMember (by Slug) ───────────────────────────────────
public class InviteMemberBySlugCommandHandler : IRequestHandler<InviteMemberBySlugCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public InviteMemberBySlugCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(InviteMemberBySlugCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Slug == request.Slug, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.Slug);

        var role = Enum.Parse<WorkspaceRole>(request.Role, ignoreCase: true);
        var invitation = WorkspaceInvitation.Create(workspace.Id, _currentUser.UserId, request.Email, role);

        _context.WorkspaceInvitations.Add(invitation);
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(invitation.Id);
    }
}

// ── RemoveMember (by Slug) ───────────────────────────────────
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

// ── UpdateMemberRole (by Slug) ───────────────────────────────
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
