using Notrelix.Domain.Chat.Exports;

namespace Notrelix.Application.Features.Chat.Exports.DTOs;

/// <summary>
/// An export of a person's chat data and where it is (UC-CHT-H06). The counts say what the file holds and what it leaves out (spaces the
/// requester cannot see, messages that were already tombstones); there are no names and no content. DownloadPath is set only while the file
/// can be downloaded; it is an authorized route of the API, never a public URL.
/// </summary>
public sealed record ChatExportDto(
    Guid Id,
    Guid SubjectUserId,
    DateTimeOffset? RangeFrom,
    DateTimeOffset? RangeTo,
    string Status,
    int MessageCount,
    int SpaceCount,
    int ExcludedSpaceCount,
    int TombstoneCount,
    long SizeBytes,
    string? FailureCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? ExpiresAt,
    string? DownloadPath)
{
    public static string PathOf(Guid workspaceId, Guid jobId) =>
        $"/api/v1/workspaces/{workspaceId}/chat/exports/{jobId}/content";

    public static ChatExportDto From(ChatExportJob job, DateTimeOffset now) => new(
        job.Id,
        job.SubjectUserId,
        job.RangeFrom,
        job.RangeTo,
        job.Status.ToWire(),
        job.MessageCount,
        job.SpaceCount,
        job.ExcludedSpaceCount,
        job.TombstoneCount,
        job.SizeBytes,
        job.FailureCode,
        job.CreatedAt,
        job.CompletedAt,
        job.ExpiresAt,
        job.IsDownloadableAt(now) ? PathOf(job.WorkspaceId, job.Id) : null);
}

/// <summary>An opened export file for the response: the bytes, the file name and the type.</summary>
public sealed record ChatExportContentDto(Stream Content, string FileName, string ContentType, long SizeBytes);
