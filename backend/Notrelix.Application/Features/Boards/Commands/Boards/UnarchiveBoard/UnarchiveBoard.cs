using BoardEntity = global::Notrelix.Domain.Entities.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.BoardColumns.CreateBoardColumn;
using global::Notrelix.Application.Features.Boards.Commands.BoardColumns.DeleteBoardColumn;
using global::Notrelix.Application.Features.Boards.Commands.BoardColumns.ReorderBoardColumns;
using global::Notrelix.Application.Features.Boards.Commands.BoardColumns.UpdateBoardColumn;
using global::Notrelix.Application.Features.Boards.Commands.BoardColumns;
using global::Notrelix.Application.Features.Boards.Commands.BoardLists.ArchiveList;
using global::Notrelix.Application.Features.Boards.Commands.BoardLists.CreateList;
using global::Notrelix.Application.Features.Boards.Commands.BoardLists.DuplicateList;
using global::Notrelix.Application.Features.Boards.Commands.BoardLists.ReorderLists;
using global::Notrelix.Application.Features.Boards.Commands.BoardLists.UnarchiveList;
using global::Notrelix.Application.Features.Boards.Commands.BoardLists.UpdateList;
using global::Notrelix.Application.Features.Boards.Commands.BoardLists;
using global::Notrelix.Application.Features.Boards.Commands.Boards.AddBoardMember;
using global::Notrelix.Application.Features.Boards.Commands.Boards.ArchiveBoard;
using global::Notrelix.Application.Features.Boards.Commands.Boards.CreateBoardBySlug;
using global::Notrelix.Application.Features.Boards.Commands.Boards.CreateBoardInWorkspace;
using global::Notrelix.Application.Features.Boards.Commands.Boards.RemoveBoardMember;
using global::Notrelix.Application.Features.Boards.Commands.Boards.SaveBoardView;
using global::Notrelix.Application.Features.Boards.Commands.Boards.UnarchiveBoard;
using global::Notrelix.Application.Features.Boards.Commands.Boards.UpdateBoard;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.Commands.CardLinks.CreateCardLink;
using global::Notrelix.Application.Features.Boards.Commands.CardLinks.DeleteCardLink;
using global::Notrelix.Application.Features.Boards.Commands.CardLinks;
using global::Notrelix.Application.Features.Boards.Commands.CardMembers.AssignCardMember;
using global::Notrelix.Application.Features.Boards.Commands.CardMembers.UnassignCardMember;
using global::Notrelix.Application.Features.Boards.Commands.CardMembers;
using global::Notrelix.Application.Features.Boards.Commands.Cards.ArchiveCard;
using global::Notrelix.Application.Features.Boards.Commands.Cards.CreateCard;
using global::Notrelix.Application.Features.Boards.Commands.Cards.DuplicateCard;
using global::Notrelix.Application.Features.Boards.Commands.Cards.LinkPageToCard;
using global::Notrelix.Application.Features.Boards.Commands.Cards.MoveCard;
using global::Notrelix.Application.Features.Boards.Commands.Cards.SetCardDueDate;
using global::Notrelix.Application.Features.Boards.Commands.Cards.UnlinkPageFromCard;
using global::Notrelix.Application.Features.Boards.Commands.Cards.UpdateCard;
using global::Notrelix.Application.Features.Boards.Commands.Cards.UpdateCardFieldValues;
using global::Notrelix.Application.Features.Boards.Commands.Cards.UpdateCardStatus;
using global::Notrelix.Application.Features.Boards.Commands.Cards;
using global::Notrelix.Application.Features.Boards.Commands.Checklists.CreateChecklist;
using global::Notrelix.Application.Features.Boards.Commands.Checklists.CreateChecklistItem;
using global::Notrelix.Application.Features.Boards.Commands.Checklists.DeleteChecklist;
using global::Notrelix.Application.Features.Boards.Commands.Checklists.DeleteChecklistItem;
using global::Notrelix.Application.Features.Boards.Commands.Checklists.ToggleChecklistItem;
using global::Notrelix.Application.Features.Boards.Commands.Checklists.UpdateChecklist;
using global::Notrelix.Application.Features.Boards.Commands.Checklists.UpdateChecklistItem;
using global::Notrelix.Application.Features.Boards.Commands.Checklists;
using global::Notrelix.Application.Features.Boards.Commands.Labels.AddLabelToCard;
using global::Notrelix.Application.Features.Boards.Commands.Labels.CreateLabel;
using global::Notrelix.Application.Features.Boards.Commands.Labels.DeleteLabel;
using global::Notrelix.Application.Features.Boards.Commands.Labels.RemoveLabelFromCard;
using global::Notrelix.Application.Features.Boards.Commands.Labels.UpdateLabel;
using global::Notrelix.Application.Features.Boards.Commands.Labels;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.Boards.UnarchiveBoard;

public record UnarchiveBoardCommand(Guid BoardId) : IRequest<Result>;

public class UnarchiveBoardCommandHandler : IRequestHandler<UnarchiveBoardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public UnarchiveBoardCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(UnarchiveBoardCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);
        await _permissions.EnsureCanManageBoardAsync(board.Id, _currentUser.UserId, ct);
        board.Unarchive();
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
