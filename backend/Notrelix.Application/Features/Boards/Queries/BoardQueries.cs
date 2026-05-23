using MediatR;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Boards.DTOs;

namespace Notrelix.Application.Features.Boards.Queries;

// ──────── Get Boards in Workspace ────────
public record GetBoardsQuery(Guid WorkspaceId) : IRequest<Result<List<BoardDto>>>;

// ──────── Get Full Board (with lists and cards) ────────
public record GetFullBoardQuery(Guid BoardId) : IRequest<Result<FullBoardDto>>;

// ──────── Get Card Detail ────────
public record GetCardQuery(Guid CardId) : IRequest<Result<CardDto>>;

// ──────── Get My Cards (across boards) ────────
public record GetMyCardsQuery(Guid WorkspaceId) : IRequest<Result<List<CardSummaryDto>>>;

// ══════════════════════════════════════════════════════════════
// Additional queries needed by Minimal API endpoints
// ══════════════════════════════════════════════════════════════

public record GetBoardQuery(Guid BoardId) : IRequest<Result<BoardDto>>;
public record GetBoardsBySlugQuery(string Slug) : IRequest<Result<List<BoardDto>>>;
public record GetBoardMembersQuery(Guid BoardId) : IRequest<Result<List<BoardMemberDto>>>;
public record GetBoardViewQuery(Guid BoardId) : IRequest<Result<object>>;
public record GetLabelsQuery(Guid BoardId) : IRequest<Result<List<CardLabelDto>>>;
public record GetChecklistsQuery(Guid CardId) : IRequest<Result<List<ChecklistDto>>>;
