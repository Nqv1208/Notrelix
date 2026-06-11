using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Document.Common;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Document.Commands.Blocks.UpdateBlock;

public record UpdateBlockCommand(
    Guid BlockId,
    string? Type,
    string? Properties
) : IRequest<Result>;

public class UpdateBlockCommandHandler : IRequestHandler<UpdateBlockCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UpdateBlockCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateBlockCommand request, CancellationToken ct)
    {
        var block = await _context.Blocks.FirstOrDefaultAsync(block => block.Id == request.BlockId && !block.IsDeleted, ct);
        if (block is null) throw new NotFoundException(nameof(Block), request.BlockId);

        if (request.Type is not null) block.UpdateType(request.Type);
        if (request.Properties is not null) block.UpdateProperties(request.Properties);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
