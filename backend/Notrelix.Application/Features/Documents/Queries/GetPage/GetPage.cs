using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Document.Common;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

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
