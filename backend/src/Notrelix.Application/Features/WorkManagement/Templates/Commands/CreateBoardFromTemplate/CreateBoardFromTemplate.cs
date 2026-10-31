using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Application.Features.WorkManagement.Boards.Services;
using Notrelix.Application.Features.WorkManagement.Templates.Services;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.WorkManagement.Templates.Commands.CreateBoardFromTemplate;

[IdempotencyOperation("work-management.templates.create-board-from-template.v1")]
public record CreateBoardFromTemplateCommand(
    Guid TemplateId,
    Guid WorkspaceId,
    string Name,
    Guid? SpaceId = null)
    : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.CreateBoard;

    // GOV-028: creating in a Space is authorized on that Space (Editor level).
    public ResourceRef Resource => SpaceId is { } spaceId
        ? ResourceRef.Create(ResourceKind.Create("workspaces.space"), spaceId, WorkspaceId)
        : ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class CreateBoardFromTemplateCommandHandler : IRequestHandler<CreateBoardFromTemplateCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IBoardSpacePlacement _placement;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIdentityUserFacts _identity;

    public CreateBoardFromTemplateCommandHandler(
        IWorkManagementDbContext context,
        IBoardSpacePlacement placement,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IIdentityUserFacts identity)
    {
        _context = context;
        _placement = placement;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _identity = identity;
    }

    public async Task<Result<Guid>> Handle(CreateBoardFromTemplateCommand request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var locale = await _identity.FindLocaleAsync(userId, ct);

        BoardTemplateStructure structure;
        string? description;
        if (BuiltInBoardTemplates.IsBuiltIn(request.TemplateId))
        {
            structure = BuiltInBoardTemplates.Structure(request.TemplateId, locale);
            description = BuiltInBoardTemplates.All(locale).Single(t => t.Id == request.TemplateId).Description;
        }
        else
        {
            // A template of another workspace or one that is not published is not found: the gallery offers
            // only this workspace's published templates.
            var template = await _context.BoardTemplates.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.WorkspaceId == request.WorkspaceId, ct);
            if (template is null || template.Status != TemplateStatus.Published)
                throw new NotFoundException(nameof(BoardTemplate), request.TemplateId);
            structure = BoardTemplateStructure.FromJson(template.Structure);
            description = template.Description;
        }

        if (request.SpaceId is { } spaceId)
            await _placement.EnsureActiveTargetAsync(request.WorkspaceId, spaceId, ct);

        var now = _dateTimeProvider.UtcNow;
        var accountId = _requestContext.RequireAccountId();
        var board = Board.Create(accountId, request.WorkspaceId, userId, request.Name, description, now, spaceId: request.SpaceId);
        _context.Boards.Add(board);
        BoardOwnership.AddCreatorAsOwner(_context, board.Id, userId, now);

        // Every id of the new board is fresh; the structure's ids only say which new row a reference points to.
        var map = new Dictionary<Guid, Guid>();
        var created = new List<(BoardField Field, TemplateField Source)>();
        var fieldPositions = Positions(structure.Fields.Count);
        foreach (var (field, index) in structure.Fields.Select((f, i) => (f, i)))
        {
            var type = Enum.Parse<FieldType>(field.Type);
            var initial = type == FieldType.Status
                ? JsonValue.Create("{\"transitions\":{}}")!
                : JsonValue.Create(field.Settings.GetRawText())!;
            var entity = BoardField.Create(
                accountId, request.WorkspaceId, board.Id, field.Name, type, FieldSettings.Create(initial),
                fieldPositions[index], userId, now, isSystem: field.IsSystem);
            map[field.Id] = entity.Id;
            var optionPositions = Positions(field.Options.Count);
            foreach (var (option, optionIndex) in field.Options.Select((o, i) => (o, i)))
            {
                var category = option.Category is null ? (WorkStatusCategory?)null : Enum.Parse<WorkStatusCategory>(option.Category);
                map[option.Id] = entity.AddOption(option.Name, Color.Create(option.Color), optionPositions[optionIndex], userId, now, category).Id;
            }
            _context.BoardFields.Add(entity);
            created.Add((entity, field));
        }

        var groupPositions = Positions(structure.Groups.Count);
        foreach (var (group, index) in structure.Groups.Select((g, i) => (g, i)))
        {
            var entity = BoardGroup.Create(accountId, request.WorkspaceId, board.Id, group.Name, Color.Create(group.Color), groupPositions[index], userId, now);
            map[group.Id] = entity.Id;
            _context.BoardGroups.Add(entity);
        }

        // Settings and view configs may name fields, options and groups, so they are written once every id exists.
        foreach (var (entity, source) in created)
        {
            var remapped = JsonValue.Create(Remap(source.Settings.GetRawText(), map))!;
            if (!remapped.Equals(entity.Settings.Data))
                entity.UpdateSettings(FieldSettings.Create(remapped), userId, now);
        }

        foreach (var label in structure.Labels)
            _context.Labels.Add(Label.Create(accountId, request.WorkspaceId, board.Id, label.Name, LabelColor.Create(label.Color), userId, now));

        foreach (var view in structure.Views)
            _context.BoardViews.Add(BoardView.Create(
                accountId, request.WorkspaceId, board.Id, view.Name, Enum.Parse<ViewType>(view.Type),
                BoardViewConfig.Create(JsonValue.Create(Remap(view.Config.GetRawText(), map))!), userId, now, view.IsDefault));

        foreach (var perGroup in structure.Items.Where(i => map.ContainsKey(i.GroupId)).GroupBy(i => i.GroupId))
        {
            var items = perGroup.ToList();
            var positions = Positions(items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                var (sequence, key) = board.GenerateNextItemIdentity(userId, now);
                var item = BoardItem.CreateRoot(
                    accountId, request.WorkspaceId, board.Id, map[perGroup.Key], items[i].Title, positions[i], userId, now,
                    itemKey: key, itemSequence: sequence, descriptionMd: items[i].Description);
                _context.BoardItems.Add(item);
                foreach (var (oldFieldId, value) in items[i].Values.Where(v => map.ContainsKey(v.Key)))
                    _context.BoardItemValues.Add(BoardItemValue.Create(
                        item.Id, map[oldFieldId], FieldValue.Create(JsonValue.Create(Remap(value.GetRawText(), map))!)));
            }
        }

        return Result<Guid>.Success(board.Id);
    }

    private static IReadOnlyList<FractionalIndex> Positions(int count) =>
        count == 0 ? [] : FractionalIndexGenerator.GenerateNKeysBetween(null, null, count);

    private static string Remap(string json, Dictionary<Guid, Guid> map)
    {
        foreach (var (oldId, newId) in map)
            json = json.Replace(oldId.ToString(), newId.ToString(), StringComparison.OrdinalIgnoreCase);
        return json;
    }
}
