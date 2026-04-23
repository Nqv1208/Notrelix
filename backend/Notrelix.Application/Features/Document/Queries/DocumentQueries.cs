using MediatR;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Document.DTOs;

namespace Notrelix.Application.Features.Document.Queries;

// ──────── Get Page Tree ────────
public record GetPageTreeQuery(Guid WorkspaceId) : IRequest<Result<List<PageTreeItemDto>>>;

// ──────── Get Page Detail ────────
public record GetPageQuery(Guid PageId) : IRequest<Result<PageDto>>;

// ──────── Get Page Blocks ────────
public record GetPageBlocksQuery(Guid PageId) : IRequest<Result<List<BlockDto>>>;
