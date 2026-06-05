using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.Commands.CardMembers.UnassignCardMember;
using global::Notrelix.Application.Features.Boards.Commands.CardMembers;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.CardMembers.UnassignCardMember;

public record UnassignCardMemberCommand(Guid CardId, Guid UserId) : IRequest<Result>;

public class UnassignCardMemberCommandHandler : IRequestHandler<UnassignCardMemberCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UnassignCardMemberCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UnassignCardMemberCommand request, CancellationToken ct)
    {
        var member = await _context.CardMembers
            .FirstOrDefaultAsync(m => m.CardId == request.CardId && m.UserId == request.UserId, ct);
        if (member is not null)
        {
            _context.CardMembers.Remove(member);
            await _context.SaveChangesAsync(ct);
        }
        return Result.Success();
    }
}
