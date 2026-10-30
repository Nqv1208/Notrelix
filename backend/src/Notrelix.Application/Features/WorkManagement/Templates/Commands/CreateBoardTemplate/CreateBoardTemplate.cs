using global::Notrelix.Application.Common.Models;
using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Templates.Services;

namespace Notrelix.Application.Features.WorkManagement.Templates.Commands.CreateBoardTemplate;

[IdempotencyOperation("work-management.templates.create-board-template.v1")]
public record CreateBoardTemplateCommand(
    Guid BoardId,
    string Name,
    string? Description,
    bool IncludeItems = false)
    : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    // Saving a Board as a template needs the same right as managing the Board.
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class CreateBoardTemplateCommandHandler : IRequestHandler<CreateBoardTemplateCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateBoardTemplateCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateBoardTemplateCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(Board), request.BoardId);

        var fields = await _context.BoardFields.AsNoTracking()
            .Where(f => f.BoardId == board.Id && f.Type != FieldType.Rollup)
            .ToListAsync(ct);
        var fieldIds = fields.Select(f => f.Id).ToList();
        var options = await _context.FieldOptions.AsNoTracking()
            .Where(o => fieldIds.Contains(o.FieldId))
            .ToListAsync(ct);
        var groups = await _context.BoardGroups.AsNoTracking()
            .Where(g => g.BoardId == board.Id && !g.IsArchived)
            .ToListAsync(ct);
        var labels = await _context.Labels.AsNoTracking()
            .Where(l => l.BoardId == board.Id && l.Status == LabelStatus.Active)
            .ToListAsync(ct);
        var views = await _context.BoardViews.AsNoTracking()
            .Where(v => v.BoardId == board.Id && !v.IsArchived && v.Visibility == BoardViewVisibility.Board && v.Type != ViewType.Form)
            .ToListAsync(ct);

        var structure = new BoardTemplateStructure(
            BoardTemplateStructure.CurrentVersion,
            fields.OrderBy(f => f.Position.Value, StringComparer.Ordinal).Select(f => new TemplateField(
                f.Id, f.Name, f.Type.ToString(), f.IsSystem, Raw(f.Settings.Data.Value),
                options.Where(o => o.FieldId == f.Id).OrderBy(o => o.Position.Value, StringComparer.Ordinal)
                    .Select(o => new TemplateOption(o.Id, o.Name, o.Color.Value, o.Category?.ToString())).ToList())).ToList(),
            groups.OrderBy(g => g.Position.Value, StringComparer.Ordinal).Select(g => new TemplateGroup(g.Id, g.Title, g.Color.Value)).ToList(),
            labels.Select(l => new TemplateLabel(l.Name, l.Color.Hex)).ToList(),
            views.Select(v => new TemplateView(v.Name, v.Type.ToString(), v.IsDefault, Raw(v.Config.Data.Value))).ToList(),
            request.IncludeItems ? await CaptureItemsAsync(board.Id, groups.Select(g => g.Id).ToHashSet(), fieldIds, ct) : []);

        var template = BoardTemplate.Create(
            board.AccountId, board.WorkspaceId, request.Name, request.Description, structure.ToJson(), _dateTimeProvider.UtcNow);

        _context.BoardTemplates.Add(template);
        return Result<Guid>.Success(template.Id);
    }

    private async Task<List<TemplateItem>> CaptureItemsAsync(Guid boardId, HashSet<Guid> groupIds, List<Guid> fieldIds, CancellationToken ct)
    {
        var items = await _context.BoardItems.AsNoTracking()
            .Where(i => i.BoardId == boardId && i.ParentItemId == null && !i.IsArchived)
            .ToListAsync(ct);
        var kept = items.Where(i => groupIds.Contains(i.GroupId))
            .OrderBy(i => i.Position.Value, StringComparer.Ordinal)
            .ToList();
        // A template never holds a silently shortened board: past the cap the save is refused with the reason.
        if (kept.Count > BoardTemplateStructure.MaxItems)
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_BoardTemplate_TooManyItems,
                $"A template can hold at most {BoardTemplateStructure.MaxItems} items; this board has {kept.Count}. Save it without items or archive some first.");
        var itemIds = kept.Select(i => i.Id).ToList();
        var values = await _context.BoardItemValues.AsNoTracking()
            .Where(v => itemIds.Contains(v.ItemId) && fieldIds.Contains(v.FieldId))
            .ToListAsync(ct);

        return kept.Select(i => new TemplateItem(
            i.GroupId, i.Name, i.DescriptionMd,
            values.Where(v => v.ItemId == i.Id).ToDictionary(v => v.FieldId, v => Raw(v.Value.Data.Value)))).ToList();
    }

    private static JsonElement Raw(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
