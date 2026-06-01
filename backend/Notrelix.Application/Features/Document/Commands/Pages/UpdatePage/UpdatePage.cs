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

namespace Notrelix.Application.Features.Document.Commands.Pages.UpdatePage;

public record UpdatePageCommand(
    Guid PageId,
    string? Title,
    string? IconType,
    string? IconValue,
    string? CoverUrl
) : IRequest<Result>;

public class UpdatePageCommandHandler : IRequestHandler<UpdatePageCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UpdatePageCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdatePageCommand request, CancellationToken ct)
    {
        var page = await _context.Pages.FirstOrDefaultAsync(page => page.Id == request.PageId && !page.IsDeleted, ct);
        if (page is null) throw new NotFoundException(nameof(Page), request.PageId);

        if (request.Title is not null) page.UpdateTitle(request.Title);
        if (request.IconType is not null || request.IconValue is not null) page.UpdateIcon(request.IconType, request.IconValue);
        if (request.CoverUrl is not null) page.UpdateCover(request.CoverUrl);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
