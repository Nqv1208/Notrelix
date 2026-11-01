using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.WorkManagement.Templates.Services;

/// <summary>
/// Writes a <see cref="BoardTemplateStructure"/> into a new Board: every id is fresh, references between rows are
/// remapped, so the new Board shares nothing with its source. One writer for "create from a template" and
/// "duplicate a board". Returns how many assignees were left out because <paramref name="mayKeepAssignee"/> said no.
/// </summary>
public static class BoardStructureInstantiator
{
    public static int Instantiate(
        IWorkManagementDbContext context,
        Board board,
        BoardTemplateStructure structure,
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        DateTimeOffset now,
        Func<Guid, bool>? mayKeepAssignee = null)
    {
        var dropped = 0;
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
                accountId, workspaceId, board.Id, field.Name, type, FieldSettings.Create(initial),
                fieldPositions[index], userId, now, isSystem: field.IsSystem);
            map[field.Id] = entity.Id;
            var optionPositions = Positions(field.Options.Count);
            foreach (var (option, optionIndex) in field.Options.Select((o, i) => (o, i)))
            {
                var category = option.Category is null ? (WorkStatusCategory?)null : Enum.Parse<WorkStatusCategory>(option.Category);
                map[option.Id] = entity.AddOption(option.Name, Color.Create(option.Color), optionPositions[optionIndex], userId, now, category).Id;
            }
            context.BoardFields.Add(entity);
            created.Add((entity, field));
        }

        var groupPositions = Positions(structure.Groups.Count);
        foreach (var (group, index) in structure.Groups.Select((g, i) => (g, i)))
        {
            var entity = BoardGroup.Create(accountId, workspaceId, board.Id, group.Name, Color.Create(group.Color), groupPositions[index], userId, now);
            map[group.Id] = entity.Id;
            context.BoardGroups.Add(entity);
        }

        // Settings and view configs may name fields, options and groups, so they are written once every id exists.
        foreach (var (entity, source) in created)
        {
            var remapped = JsonValue.Create(Remap(source.Settings.GetRawText(), map))!;
            if (!remapped.Equals(entity.Settings.Data))
                entity.UpdateSettings(FieldSettings.Create(remapped), userId, now);
        }

        foreach (var label in structure.Labels)
            context.Labels.Add(Label.Create(accountId, workspaceId, board.Id, label.Name, LabelColor.Create(label.Color), userId, now));

        foreach (var view in structure.Views)
            context.BoardViews.Add(BoardView.Create(
                accountId, workspaceId, board.Id, view.Name, Enum.Parse<ViewType>(view.Type),
                BoardViewConfig.Create(JsonValue.Create(Remap(view.Config.GetRawText(), map))!), userId, now, view.IsDefault));

        foreach (var perGroup in structure.Items.Where(i => map.ContainsKey(i.GroupId)).GroupBy(i => i.GroupId))
        {
            var items = perGroup.ToList();
            var positions = Positions(items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                var (sequence, key) = board.GenerateNextItemIdentity(userId, now);
                var item = BoardItem.CreateRoot(
                    accountId, workspaceId, board.Id, map[perGroup.Key], items[i].Title, positions[i], userId, now,
                    itemKey: key, itemSequence: sequence, descriptionMd: items[i].Description);
                context.BoardItems.Add(item);
                foreach (var assignee in (items[i].Assignees ?? []).Distinct())
                {
                    if (mayKeepAssignee is null || !mayKeepAssignee(assignee)) { dropped++; continue; }
                    context.BoardItemMembers.Add(BoardItemMember.Create(accountId, workspaceId, board.Id, item.Id, assignee, userId, now));
                }
                foreach (var (oldFieldId, value) in items[i].Values.Where(v => map.ContainsKey(v.Key)))
                    context.BoardItemValues.Add(BoardItemValue.Create(
                        item.Id, map[oldFieldId], FieldValue.Create(JsonValue.Create(Remap(value.GetRawText(), map))!)));
            }
        }
        return dropped;
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
