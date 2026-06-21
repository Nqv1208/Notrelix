using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.Common;
using global::Notrelix.Application.Features.Documents.DTOs;
using global::Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.Documents.Blocks.Commands.UpdateBlock;

public record UpdateBlockCommand(
    Guid BlockId,
    string? Type,
    string? Properties
) : ICommand<Result>, ITransactionalRequest;

public class UpdateBlockCommandHandler : IRequestHandler<UpdateBlockCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    public UpdateBlockCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateBlockCommand request, CancellationToken ct)
    {
        var block = await _context.Blocks.FirstOrDefaultAsync(block => block.Id == request.BlockId && !block.IsDeleted, ct);
        if (block is null) throw new NotFoundException(nameof(Block), request.BlockId);

        return Result.Success();
    }
}
