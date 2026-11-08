using System.Text.Json;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Application.Features.WorkManagement.RelationTargets;

/// <summary>
/// The Application's half of a Relation field (D78.6, product sections 64-66): what the Domain cannot check because
/// it fetches nothing. A target board must be an active board of the same account and Workspace, and readable by the
/// person naming it. A target the person cannot read is refused with the same error as a missing one, so the
/// existence of a board in another Workspace or account is not revealed. Every linked item must be a live item of
/// the target board in the same account and Workspace; a new link also needs the target board readable by the writer.
/// Links already stored are kept on an edit that does not add them, so a viewer who cannot read the board does not
/// lose them by editing another part of the cell.
/// </summary>
public static class RelationFieldTargets
{
    /// <summary>The target board of a field, or null when its settings name none (the Domain refuses that on save).</summary>
    public static Guid? TargetBoardIdOf(BoardField field) => RelationFieldSettings.TargetBoardId(field.Settings.Data.Value);

    /// <summary>The item ids a stored Relation value links to; empty for no value.</summary>
    public static IReadOnlyList<Guid> ParseLinkIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "null") return [];
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];
        return doc.RootElement.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String && Guid.TryParse(e.GetString(), out _))
            .Select(e => Guid.Parse(e.GetString()!))
            .ToList();
    }

    /// <summary>The item ids the item's current stored value of the field links to.</summary>
    public static IReadOnlyCollection<Guid> StoredLinkIds(BoardItem item, Guid fieldId)
    {
        var stored = item.FieldValues.FirstOrDefault(value => value.FieldId == fieldId);
        return stored is null || stored.Value.IsEmpty ? [] : ParseLinkIds(stored.Value.Data.Value);
    }

    /// <summary>
    /// The target board must be an active board of the same account and Workspace, readable by the person naming it.
    /// An unreadable board gets the same refusal as a missing one.
    /// </summary>
    public static async Task EnsureTargetBoardAsync(
        IWorkManagementDbContext context,
        IResourceVisibility? visibility,
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        Guid targetBoardId,
        CancellationToken cancellationToken)
    {
        var exists = await context.Boards.AsNoTracking()
            .AnyAsync(b => b.Id == targetBoardId && b.AccountId == accountId && b.WorkspaceId == workspaceId
                && !b.IsArchived && b.DeletedAt == null, cancellationToken);
        if (!exists || !await IsReadableAsync(visibility, workspaceId, userId, targetBoardId, cancellationToken))
            throw TargetBoardUnavailable();
    }

    /// <summary>
    /// Checks the links of one write. Every linked id must be a live item of the target board in the same account and
    /// Workspace. Each id not already stored must also be an unarchived item and the target board must be readable by
    /// the writer; a missing, foreign or unreadable target gets a refusal that names no title.
    /// </summary>
    public static async Task EnsureLinksAsync(
        IWorkManagementDbContext context,
        IResourceVisibility? visibility,
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        Guid targetBoardId,
        IReadOnlyCollection<Guid> requested,
        IReadOnlyCollection<Guid> stored,
        CancellationToken cancellationToken)
    {
        if (requested.Count == 0) return;
        var added = requested.Where(id => !stored.Contains(id)).ToHashSet();
        var ids = requested.Distinct().ToList();

        var rows = await context.BoardItems.AsNoTracking()
            .Where(item => ids.Contains(item.Id))
            .Select(item => new { item.Id, item.AccountId, item.WorkspaceId, item.BoardId, item.DeletedAt, item.IsArchived })
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        if (added.Count > 0 && !await IsReadableAsync(visibility, workspaceId, userId, targetBoardId, cancellationToken))
            throw TargetBoardUnavailable();

        foreach (var id in ids)
        {
            if (!rows.TryGetValue(id, out var row) || row.AccountId != accountId || row.WorkspaceId != workspaceId
                || row.BoardId != targetBoardId || row.DeletedAt is not null)
                throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Relation_TargetItemNotFound, "A linked item is not an item of the target board.");
            if (added.Contains(id) && row.IsArchived)
                throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Relation_TargetItemNotFound, "An archived item cannot be newly linked.");
        }
    }

    /// <summary>Fails closed: without a visibility answer the board is not treated as readable.</summary>
    private static async Task<bool> IsReadableAsync(IResourceVisibility? visibility, Guid workspaceId, Guid userId, Guid targetBoardId, CancellationToken cancellationToken)
    {
        if (visibility is null) return false;
        return (await visibility.VisibleBoardIdsAsync(workspaceId, userId, cancellationToken)).Contains(targetBoardId);
    }

    private static Notrelix.Domain.Common.Exceptions.BusinessRuleException TargetBoardUnavailable() =>
        new(WorkManagementRuleCodes.WorkManagement_Relation_TargetBoardNotFound, "The target board does not exist, is archived or cannot be read by you.");
}
