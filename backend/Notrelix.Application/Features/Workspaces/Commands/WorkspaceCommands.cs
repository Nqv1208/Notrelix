using MediatR;
using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Workspacess.Commands;

// ──────── Create Workspace ────────
public record CreateWorkspaceCommand(
    string Name,
    string? Description,
    bool IsPersonal
) : IRequest<Result<Guid>>;

// ──────── Update Workspace ────────
public record UpdateWorkspaceCommand(
    Guid WorkspaceId,
    string? Name,
    string? Description,
    string? IconType,
    string? IconValue
) : IRequest<Result>;

// ──────── Delete/Archive Workspace ────────
public record ArchiveWorkspaceCommand(Guid WorkspaceId) : IRequest<Result>;

// ──────── Invite Member ────────
public record InviteMemberCommand(
    Guid WorkspaceId,
    string Email,
    string Role
) : IRequest<Result<Guid>>;

// ──────── Accept Invitation ────────
public record AcceptInvitationCommand(string Token) : IRequest<Result>;

// ──────── Remove Member ────────
public record RemoveMemberCommand(
    Guid WorkspaceId,
    Guid UserId
) : IRequest<Result>;

// ──────── Update Member Role ────────
public record UpdateMemberRoleCommand(
    Guid WorkspaceId,
    Guid UserId,
    string Role
) : IRequest<Result>;
