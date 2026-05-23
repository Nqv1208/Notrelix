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

// ══════════════════════════════════════════════════════════════
// Slug-based variants — used by Minimal API endpoints
// ══════════════════════════════════════════════════════════════

public record GetWorkspaceBySlugQuery(string Slug) : IRequest<Result<WorkspaceDto>>;
public record GetWorkspaceMembersBySlugQuery(string Slug) : IRequest<Result<List<WorkspaceMemberDto>>>;
public record GetWorkspaceActivityBySlugQuery(string Slug, int Page = 1, int PageSize = 20) : IRequest<Result<object>>;
