using Notrelix.Domain.Chat.Exports;

namespace Notrelix.Application.Features.Chat.Exports;

/// <summary>The file an export produces: one JSON document, named by the person and the day it was built (UC-CHT-H06).</summary>
public static class ChatExportFiles
{
    public const string ContentType = "application/json; charset=utf-8";
    public const string FormatName = "notrelix.chat-export.v1";

    public static string FileName(ChatExportJob job) =>
        $"chat-export-{job.SubjectUserId:N}-{(job.CompletedAt ?? job.CreatedAt):yyyy-MM-dd}.json";

    /// <summary>Where the file lives in object storage: under the tenant prefix, apart from attachments.</summary>
    public static string StorageKey(ChatExportJob job) => $"{job.AccountId:N}/{job.WorkspaceId:N}/chat-exports/{job.Id:N}";
}
