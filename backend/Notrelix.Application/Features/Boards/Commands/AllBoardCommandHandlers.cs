using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Entities.Boards;

namespace Notrelix.Application.Features.Boards.Commands;

// ── CreateList ───────────────────────────────────────────────
public class CreateListCommandHandler : IRequestHandler<CreateListCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    public CreateListCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateListCommand request, CancellationToken ct)
    {
        var position = request.Position ?? await _context.BoardLists
            .Where(l => l.BoardId == request.BoardId && !l.IsArchived)
            .MaxAsync(l => (double?)l.Position, ct) + 1 ?? 0;

        var list = BoardList.Create(request.BoardId, request.Title, position);
        _context.BoardLists.Add(list);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(list.Id);
    }
}

// ── UpdateList ───────────────────────────────────────────────
public class UpdateListCommandHandler : IRequestHandler<UpdateListCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UpdateListCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateListCommand request, CancellationToken ct)
    {
        var list = await _context.BoardLists.FirstOrDefaultAsync(l => l.Id == request.ListId, ct);
        if (list is null) throw new NotFoundException(nameof(BoardList), request.ListId);
        list.UpdateTitle(request.Title);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── ArchiveList ──────────────────────────────────────────────
public class ArchiveListCommandHandler : IRequestHandler<ArchiveListCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public ArchiveListCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(ArchiveListCommand request, CancellationToken ct)
    {
        var list = await _context.BoardLists.FirstOrDefaultAsync(l => l.Id == request.ListId, ct);
        if (list is null) throw new NotFoundException(nameof(BoardList), request.ListId);
        list.Archive();
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── UnarchiveList ────────────────────────────────────────────
public class UnarchiveListCommandHandler : IRequestHandler<UnarchiveListCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UnarchiveListCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UnarchiveListCommand request, CancellationToken ct)
    {
        var list = await _context.BoardLists.FirstOrDefaultAsync(l => l.Id == request.ListId, ct);
        if (list is null) throw new NotFoundException(nameof(BoardList), request.ListId);
        list.Unarchive();
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── ReorderLists ─────────────────────────────────────────────
public class ReorderListsCommandHandler : IRequestHandler<ReorderListsCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public ReorderListsCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(ReorderListsCommand request, CancellationToken ct)
    {
        foreach (var item in request.Items)
        {
            var list = await _context.BoardLists.FirstOrDefaultAsync(l => l.Id == item.Id, ct);
            list?.UpdatePosition(item.NewPosition);
        }
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── UpdateCard ───────────────────────────────────────────────
public class UpdateCardCommandHandler : IRequestHandler<UpdateCardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UpdateCardCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateCardCommand request, CancellationToken ct)
    {
        var card = await _context.Cards.FirstOrDefaultAsync(c => c.Id == request.CardId && !c.IsDeleted, ct);
        if (card is null) throw new NotFoundException(nameof(Card), request.CardId);

        if (request.Title is not null) card.UpdateTitle(request.Title);
        if (request.DescriptionMd is not null) card.UpdateDescription(request.DescriptionMd);
        if (request.Priority is not null)
            card.UpdatePriority(Enum.Parse<Domain.Enums.CardPriority>(request.Priority, ignoreCase: true));
        if (request.Cover is not null) card.UpdateCover(request.Cover);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── ArchiveCard ──────────────────────────────────────────────
public class ArchiveCardCommandHandler : IRequestHandler<ArchiveCardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public ArchiveCardCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(ArchiveCardCommand request, CancellationToken ct)
    {
        var card = await _context.Cards.FirstOrDefaultAsync(c => c.Id == request.CardId, ct);
        if (card is null) throw new NotFoundException(nameof(Card), request.CardId);
        card.Archive();
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── SetCardDueDate ───────────────────────────────────────────
public class SetCardDueDateCommandHandler : IRequestHandler<SetCardDueDateCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public SetCardDueDateCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(SetCardDueDateCommand request, CancellationToken ct)
    {
        var card = await _context.Cards.FirstOrDefaultAsync(c => c.Id == request.CardId, ct);
        if (card is null) throw new NotFoundException(nameof(Card), request.CardId);
        card.SetDueDate(request.DueDate);
        card.SetStartDate(request.StartDate);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── UpdateCardStatus ─────────────────────────────────────────
public class UpdateCardStatusCommandHandler : IRequestHandler<UpdateCardStatusCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UpdateCardStatusCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateCardStatusCommand request, CancellationToken ct)
    {
        var card = await _context.Cards.FirstOrDefaultAsync(c => c.Id == request.CardId, ct);
        if (card is null) throw new NotFoundException(nameof(Card), request.CardId);
        card.UpdateStatus(Enum.Parse<Domain.Enums.CardStatus>(request.Status, ignoreCase: true));
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── UnlinkPageFromCard ───────────────────────────────────────
public class UnlinkPageFromCardCommandHandler : IRequestHandler<UnlinkPageFromCardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UnlinkPageFromCardCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UnlinkPageFromCardCommand request, CancellationToken ct)
    {
        var card = await _context.Cards.FirstOrDefaultAsync(c => c.Id == request.CardId, ct);
        if (card is null) throw new NotFoundException(nameof(Card), request.CardId);
        card.UnlinkPage();
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── AssignCardMember ─────────────────────────────────────────
public class AssignCardMemberCommandHandler : IRequestHandler<AssignCardMemberCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    public AssignCardMemberCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task<Result> Handle(AssignCardMemberCommand request, CancellationToken ct)
    {
        var card = await _context.Cards
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == request.CardId, ct);
        if (card is null) throw new NotFoundException(nameof(Card), request.CardId);
        card.AssignMember(request.UserId, _currentUser.UserId);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── UnassignCardMember ───────────────────────────────────────
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

// ── AddLabelToCard ───────────────────────────────────────────
public class AddLabelToCardCommandHandler : IRequestHandler<AddLabelToCardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public AddLabelToCardCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(AddLabelToCardCommand request, CancellationToken ct)
    {
        var card = await _context.Cards
            .Include(c => c.Labels)
            .FirstOrDefaultAsync(c => c.Id == request.CardId, ct);
        if (card is null) throw new NotFoundException(nameof(Card), request.CardId);
        card.AddLabel(request.LabelId);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── RemoveLabelFromCard ──────────────────────────────────────
public class RemoveLabelFromCardCommandHandler : IRequestHandler<RemoveLabelFromCardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public RemoveLabelFromCardCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(RemoveLabelFromCardCommand request, CancellationToken ct)
    {
        var cl = await _context.CardLabels
            .FirstOrDefaultAsync(l => l.CardId == request.CardId && l.LabelId == request.LabelId, ct);
        if (cl is not null)
        {
            _context.CardLabels.Remove(cl);
            await _context.SaveChangesAsync(ct);
        }
        return Result.Success();
    }
}

// ── CreateLabel ──────────────────────────────────────────────
public class CreateLabelCommandHandler : IRequestHandler<CreateLabelCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    public CreateLabelCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateLabelCommand request, CancellationToken ct)
    {
        var label = Label.Create(request.BoardId, request.Color, request.Name);
        _context.Labels.Add(label);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(label.Id);
    }
}

// ── UpdateLabel ──────────────────────────────────────────────
public class UpdateLabelCommandHandler : IRequestHandler<UpdateLabelCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UpdateLabelCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateLabelCommand request, CancellationToken ct)
    {
        var label = await _context.Labels.FirstOrDefaultAsync(l => l.Id == request.LabelId, ct);
        if (label is null) throw new NotFoundException(nameof(Label), request.LabelId);
        if (request.Name is not null) label.UpdateName(request.Name);
        if (request.Color is not null) label.UpdateColor(request.Color);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── DeleteLabel ──────────────────────────────────────────────
public class DeleteLabelCommandHandler : IRequestHandler<DeleteLabelCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public DeleteLabelCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteLabelCommand request, CancellationToken ct)
    {
        var label = await _context.Labels.FirstOrDefaultAsync(l => l.Id == request.LabelId, ct);
        if (label is null) throw new NotFoundException(nameof(Label), request.LabelId);
        _context.Labels.Remove(label);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── CreateChecklist ──────────────────────────────────────────
public class CreateChecklistCommandHandler : IRequestHandler<CreateChecklistCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    public CreateChecklistCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateChecklistCommand request, CancellationToken ct)
    {
        var position = await _context.Checklists
            .Where(c => c.CardId == request.CardId)
            .MaxAsync(c => (double?)c.Position, ct) + 1 ?? 0;

        var checklist = Checklist.Create(request.CardId, request.Title, position);
        _context.Checklists.Add(checklist);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(checklist.Id);
    }
}

// ── UpdateChecklist ──────────────────────────────────────────
public class UpdateChecklistCommandHandler : IRequestHandler<UpdateChecklistCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UpdateChecklistCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateChecklistCommand request, CancellationToken ct)
    {
        var checklist = await _context.Checklists.FirstOrDefaultAsync(c => c.Id == request.ChecklistId, ct);
        if (checklist is null) throw new NotFoundException(nameof(Checklist), request.ChecklistId);
        if (request.Title is not null) checklist.UpdateTitle(request.Title);
        if (request.Position.HasValue) checklist.UpdatePosition(request.Position.Value);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── DeleteChecklist ──────────────────────────────────────────
public class DeleteChecklistCommandHandler : IRequestHandler<DeleteChecklistCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public DeleteChecklistCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteChecklistCommand request, CancellationToken ct)
    {
        var checklist = await _context.Checklists.FirstOrDefaultAsync(c => c.Id == request.ChecklistId, ct);
        if (checklist is null) throw new NotFoundException(nameof(Checklist), request.ChecklistId);
        // Delete items first
        var items = await _context.ChecklistItems.Where(i => i.ChecklistId == checklist.Id).ToListAsync(ct);
        _context.ChecklistItems.RemoveRange(items);
        _context.Checklists.Remove(checklist);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── CreateChecklistItem ──────────────────────────────────────
public class CreateChecklistItemCommandHandler : IRequestHandler<CreateChecklistItemCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    public CreateChecklistItemCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateChecklistItemCommand request, CancellationToken ct)
    {
        var position = await _context.ChecklistItems
            .Where(i => i.ChecklistId == request.ChecklistId)
            .MaxAsync(i => (double?)i.Position, ct) + 1 ?? 0;

        var item = ChecklistItem.Create(request.ChecklistId, request.Title, position);
        _context.ChecklistItems.Add(item);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(item.Id);
    }
}

// ── UpdateChecklistItem ──────────────────────────────────────
public class UpdateChecklistItemCommandHandler : IRequestHandler<UpdateChecklistItemCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UpdateChecklistItemCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateChecklistItemCommand request, CancellationToken ct)
    {
        var item = await _context.ChecklistItems.FirstOrDefaultAsync(i => i.Id == request.ItemId, ct);
        if (item is null) throw new NotFoundException(nameof(ChecklistItem), request.ItemId);
        if (request.Title is not null) item.UpdateTitle(request.Title);
        if (request.IsChecked.HasValue) { if (request.IsChecked.Value) item.Check(); else item.Uncheck(); }
        if (request.DueDate.HasValue) item.SetDueDate(request.DueDate);
        if (request.AssigneeId.HasValue) item.Assign(request.AssigneeId);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── DeleteChecklistItem ──────────────────────────────────────
public class DeleteChecklistItemCommandHandler : IRequestHandler<DeleteChecklistItemCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public DeleteChecklistItemCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteChecklistItemCommand request, CancellationToken ct)
    {
        var item = await _context.ChecklistItems.FirstOrDefaultAsync(i => i.Id == request.ItemId, ct);
        if (item is null) throw new NotFoundException(nameof(ChecklistItem), request.ItemId);
        _context.ChecklistItems.Remove(item);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
