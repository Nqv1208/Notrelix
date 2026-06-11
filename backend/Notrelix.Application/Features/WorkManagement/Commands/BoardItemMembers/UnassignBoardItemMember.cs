using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record UnassignCardMemberCommand(Guid BoardItemId, Guid UserId) : IRequest<Result>;

public class UnassignCardMemberCommandHandler : IRequestHandler<UnassignCardMemberCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UnassignCardMemberCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UnassignCardMemberCommand request, CancellationToken ct)
    {
        var member = await _context.BoardItemMembers
            .FirstOrDefaultAsync(m => m.BoardItemId == request.BoardItemId && m.UserId == request.UserId, ct);
        if (member is not null)
        {
            _context.BoardItemMembers.Remove(member);
            await _context.SaveChangesAsync(ct);
        }
        return Result.Success();
    }
}
