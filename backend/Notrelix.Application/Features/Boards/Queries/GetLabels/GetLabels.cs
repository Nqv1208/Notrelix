using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;

namespace Notrelix.Application.Features.Boards.Queries.GetLabels;

public record GetLabelsQuery(Guid BoardId) : IRequest<Result<List<CardLabelDto>>>;

public class GetLabelsQueryHandler : IRequestHandler<GetLabelsQuery, Result<List<CardLabelDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetLabelsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<CardLabelDto>>> Handle(GetLabelsQuery request, CancellationToken ct)
    {
        var labels = await _context.Labels.AsNoTracking()
            .Where(l => l.BoardId == request.BoardId)
            .Select(l => new CardLabelDto(l.Id, l.Name, l.Color))
            .ToListAsync(ct);

        return Result<List<CardLabelDto>>.Success(labels);
    }
}
