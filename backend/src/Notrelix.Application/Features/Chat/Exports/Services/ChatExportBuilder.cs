using System.Text.Json;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Domain.Chat.Exports;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Application.Features.Chat.Exports;

/// <summary>What an export holds and what it left out. Counts only.</summary>
public sealed record ChatExportBuild(int Messages, int Spaces, int ExcludedSpaces, int Tombstones, string? FailureCode, long SizeBytes)
{
    public bool IsTooLarge => FailureCode is not null;
}

/// <summary>
/// Builds the file of an export of one person's chat data (UC-CHT-H06, CHT-021, CHT-004). The file is one JSON document: the spaces the person
/// belongs to (or belonged to) with their role and dates, and the messages they wrote, oldest first, optionally inside a date range.
/// </summary>
/// <remarks>
/// What the file holds is what the requester may read, decided when the file is built: a public channel, and a private channel or a
/// conversation the requester is an active member of. A space of the person that the requester cannot see is left out of both lists and only
/// counted (CHT-004: an Owner or Admin does not read a private space by role). A deleted space is not part of the export. A message that is
/// already a tombstone has no body and is counted, not listed; a system message is not the person's own words and is not listed. Other
/// people's messages are never in it, not even as a quote. A file over <see cref="ChatLimits.ExportMaxMessages"/> messages or
/// <see cref="ChatLimits.ExportMaxBytes"/> bytes is refused with export-too-large, and the Admin narrows the range. Messages are read in
/// keyset pages of 500, so memory stays bounded by the file limit.
/// </remarks>
public sealed class ChatExportBuilder(IChatDbContext db)
{
    public const int PageSize = 500;
    private static readonly JsonWriterOptions WriterOptions = new() { Indented = false };

    public async Task<ChatExportBuild> BuildAsync(ChatExportJob job, DateTimeOffset now, Stream output, CancellationToken cancellationToken)
    {
        var workspaceId = job.WorkspaceId;
        var subject = job.SubjectUserId;

        var memberships = await db.ChatMemberships.AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == subject)
            .ToListAsync(cancellationToken);
        var spaceIds = memberships.Select(m => m.SpaceId).ToList();
        var spaces = await db.ChatSpaces.AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId && s.DeletedAt == null && spaceIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        var requesterSpaces = (await db.ChatMemberships.AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == job.RequestedBy && m.LeftAt == null)
            .Select(m => m.SpaceId)
            .ToListAsync(cancellationToken)).ToHashSet();

        // The requester is an Owner or Admin (the worker checked), so never a Guest: a public channel is visible, anything else needs membership.
        var visible = spaces.Where(s => s.IsPublic || requesterSpaces.Contains(s.Id)).OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ThenBy(s => s.Id).ToList();
        var excluded = spaces.Count - visible.Count;
        var visibleIds = visible.Select(s => s.Id).ToList();

        await using var writer = new Utf8JsonWriter(output, WriterOptions);
        writer.WriteStartObject();
        writer.WriteString("format", ChatExportFiles.FormatName);
        writer.WriteString("exportedAt", now);
        writer.WriteString("workspaceId", workspaceId);
        writer.WriteString("subjectUserId", subject);
        writer.WriteString("requestedBy", job.RequestedBy);
        writer.WriteStartObject("range");
        WriteNullable(writer, "from", job.RangeFrom);
        WriteNullable(writer, "to", job.RangeTo);
        writer.WriteEndObject();

        writer.WriteStartArray("memberships");
        foreach (var space in visible)
        {
            var membership = memberships.First(m => m.SpaceId == space.Id);
            writer.WriteStartObject();
            writer.WriteString("spaceId", space.Id);
            writer.WriteString("spaceName", space.Name);
            writer.WriteString("spaceKind", space.Kind.ToWire());
            writer.WriteString("role", membership.Role.ToString().ToLowerInvariant());
            writer.WriteString("joinedAt", membership.JoinedAt);
            WriteNullable(writer, "leftAt", membership.LeftAt);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        var names = visible.ToDictionary(s => s.Id, s => s.Name);
        var written = 0;
        string? failure = null;
        writer.WriteStartArray("messages");
        DateTimeOffset? lastAt = null;
        Guid lastId = Guid.Empty;
        while (visibleIds.Count > 0 && failure is null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var query = db.ChatMessages.AsNoTracking()
                .Where(m => m.WorkspaceId == workspaceId && m.AuthorId == subject && m.Kind == MessageKind.User && visibleIds.Contains(m.SpaceId));
            if (job.RangeFrom is { } from) query = query.Where(m => m.CreatedAt >= from);
            if (job.RangeTo is { } to) query = query.Where(m => m.CreatedAt < to);
            if (lastAt is { } at)
            {
                var id = lastId;
                query = query.Where(m => m.CreatedAt > at || (m.CreatedAt == at && m.Id.CompareTo(id) > 0));
            }

            var page = await query.OrderBy(m => m.CreatedAt).ThenBy(m => m.Id).Take(PageSize).ToListAsync(cancellationToken);
            if (page.Count == 0) break;

            foreach (var message in page.Where(m => m.DeletedAt == null))
            {
                writer.WriteStartObject();
                writer.WriteString("id", message.Id);
                writer.WriteString("spaceId", message.SpaceId);
                writer.WriteString("spaceName", names[message.SpaceId]);
                writer.WriteNumber("sequence", message.Sequence);
                writer.WriteString("createdAt", message.CreatedAt);
                WriteNullable(writer, "editedAt", message.EditedAt);
                if (message.ThreadRootId is { } root) writer.WriteString("threadRootId", root); else writer.WriteNull("threadRootId");
                writer.WriteString("content", message.Content);
                writer.WriteEndObject();
                written++;
            }

            await writer.FlushAsync(cancellationToken);
            if (written > ChatLimits.ExportMaxMessages || output.Length > ChatLimits.ExportMaxBytes)
                failure = ChatRuleCodes.ExportTooLarge;

            var last = page[^1];
            lastAt = last.CreatedAt;
            lastId = last.Id;
            if (page.Count < PageSize) break;
        }

        writer.WriteEndArray();
        if (failure is not null)
            return new ChatExportBuild(written, visible.Count, excluded, 0, failure, output.Length);

        var tombstones = visibleIds.Count == 0 ? 0 : await CountTombstonesAsync(job, visibleIds, cancellationToken);
        writer.WriteStartObject("summary");
        writer.WriteNumber("messages", written);
        writer.WriteNumber("spaces", visible.Count);
        writer.WriteNumber("spacesNotVisibleToRequester", excluded);
        writer.WriteNumber("tombstonesOmitted", tombstones);
        writer.WriteEndObject();
        writer.WriteEndObject();
        await writer.FlushAsync(cancellationToken);

        return output.Length > ChatLimits.ExportMaxBytes
            ? new ChatExportBuild(written, visible.Count, excluded, tombstones, ChatRuleCodes.ExportTooLarge, output.Length)
            : new ChatExportBuild(written, visible.Count, excluded, tombstones, null, output.Length);
    }

    private async Task<int> CountTombstonesAsync(ChatExportJob job, List<Guid> visibleIds, CancellationToken cancellationToken)
    {
        var query = db.ChatMessages.AsNoTracking()
            .Where(m => m.WorkspaceId == job.WorkspaceId && m.AuthorId == job.SubjectUserId && m.Kind == MessageKind.User
                        && m.DeletedAt != null && visibleIds.Contains(m.SpaceId));
        if (job.RangeFrom is { } from) query = query.Where(m => m.CreatedAt >= from);
        if (job.RangeTo is { } to) query = query.Where(m => m.CreatedAt < to);
        return await query.CountAsync(cancellationToken);
    }

    private static void WriteNullable(Utf8JsonWriter writer, string name, DateTimeOffset? value)
    {
        if (value is { } v) writer.WriteString(name, v);
        else writer.WriteNull(name);
    }
}
