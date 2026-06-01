using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Document.Common;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Document.Queries.GetPage;

public record GetPageQuery(Guid PageId) : IRequest<Result<PageDto>>;

public class GetPageQueryHandler : IRequestHandler<GetPageQuery, Result<PageDto>>
{
    private readonly IApplicationDbContext _context;
    public GetPageQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<PageDto>> Handle(GetPageQuery request, CancellationToken ct)
    {
        var page = await _context.Pages.AsNoTracking()
            .FirstOrDefaultAsync(page => page.Id == request.PageId && !page.IsDeleted, ct);
        if (page is null) throw new NotFoundException(nameof(Page), request.PageId);

        return Result<PageDto>.Success(DocumentDtoMapper.ToPageDto(page));
    }
}
