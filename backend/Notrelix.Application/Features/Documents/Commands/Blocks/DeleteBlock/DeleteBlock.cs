using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Document.Common;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Document.Commands.Blocks.DeleteBlock;

public record DeleteBlockCommand(Guid BlockId) : IRequest<Result>;

public class DeleteBlockCommandHandler : IRequestHandler<DeleteBlockCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public DeleteBlockCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteBlockCommand request, CancellationToken ct)
    {
        var block = await _context.Blocks.FirstOrDefaultAsync(block => block.Id == request.BlockId && !block.IsDeleted, ct);
        if (block is null) throw new NotFoundException(nameof(Block), request.BlockId);

        block.SoftDelete();
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
