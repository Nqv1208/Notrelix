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

namespace Notrelix.Application.Features.Document.Commands.Blocks.CreateBlock;

public record CreateBlockCommand(
    Guid PageId,
    string Type,
    string Properties,
    double Position,
    Guid? ParentBlockId
) : IRequest<Result<Guid>>;

public class CreateBlockCommandHandler : IRequestHandler<CreateBlockCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateBlockCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateBlockCommand request, CancellationToken ct)
    {
        var pageExists = await _context.Pages.AsNoTracking()
            .AnyAsync(page => page.Id == request.PageId && !page.IsDeleted, ct);
        if (!pageExists) throw new NotFoundException(nameof(Page), request.PageId);

        var block = Block.Create(request.PageId, _currentUser.UserId, request.Type, request.Properties, request.Position, request.ParentBlockId);
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(block.Id);
    }
}
