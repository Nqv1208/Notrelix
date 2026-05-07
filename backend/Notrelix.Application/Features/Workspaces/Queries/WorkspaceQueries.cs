using MediatR;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.DTOs;

namespace Notrelix.Application.Features.Workspaces.Queries;

// ──────── Get Workspace by ID ────────
public record GetWorkspaceQuery(Guid WorkspaceId) : IRequest<Result<WorkspaceDto>>;

// ──────── Get User's Workspaces ────────
public record GetUserWorkspacesQuery(Guid UserId) : IRequest<Result<List<WorkspaceDto>>>;

// ──────── Get Workspace Members ────────
public record GetWorkspaceMembersQuery(Guid WorkspaceId) : IRequest<Result<List<WorkspaceMemberDto>>>;

// ──────── Get Workspace Invitations ────────
public record GetWorkspaceInvitationsQuery(Guid WorkspaceId) : IRequest<Result<List<WorkspaceInvitationDto>>>;
