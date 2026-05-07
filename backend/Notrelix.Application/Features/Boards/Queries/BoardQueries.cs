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
