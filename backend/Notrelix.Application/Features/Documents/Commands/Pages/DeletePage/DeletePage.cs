using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Document.Common;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Document.Commands.Pages.DeletePage;

public record DeletePageCommand(Guid PageId) : IRequest<Result>;

public class DeletePageCommandHandler : IRequestHandler<DeletePageCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public DeletePageCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(DeletePageCommand request, CancellationToken ct)
    {
        var page = await _context.Pages.FirstOrDefaultAsync(page => page.Id == request.PageId && !page.IsDeleted, ct);
        if (page is null) throw new NotFoundException(nameof(Page), request.PageId);

        page.SoftDelete();
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
