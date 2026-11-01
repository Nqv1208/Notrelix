using System.Text.Json;

namespace Notrelix.Application.Features.WorkManagement.Transfer;

public enum TransferKind { Import, Export }

/// <summary>A file import or export of one board for one person, as the Ops job tables keep it.</summary>
public sealed class TransferJob
{
    public TransferKind Kind { get; init; }
    public Guid Id { get; init; }
    public Guid WorkspaceId { get; init; }
    public Guid BoardId { get; init; }
    public Guid UserId { get; init; }
    public string Status { get; set; } = "";
    public string Format { get; set; } = "";
    public int Total { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    /// <summary>What the file and the person asked for (<see cref="ImportOptions"/> or <see cref="ExportOptions"/>).</summary>
    public string OptionsJson { get; set; } = "{}";
    /// <summary>An import's outcome (<see cref="ImportOutcome"/>); an export's filter.</summary>
    public string? DetailJson { get; set; }
    public string? StorageKey { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public T Options<T>() => JsonSerializer.Deserialize<T>(OptionsJson, TransferJson.Options)!;
    public T? Detail<T>() where T : class => DetailJson is null ? null : JsonSerializer.Deserialize<T>(DetailJson, TransferJson.Options);
}

public static class TransferJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    public static string Write<T>(T value) => JsonSerializer.Serialize(value, Options);
}

public static class TransferStatus
{
    public const string Uploaded = "uploaded";
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Ready = "ready";
    public const string Expired = "expired";
}

public sealed record ImportOptions(
    Guid AccountId, string FileName, string Format, long Size, string StorageKey, int RowCount, IReadOnlyList<string> Headers,
    IReadOnlyList<ColumnMap>? Mapping);

/// <param name="FailureCode">Why nothing was created: has-errors, board-archived, board-gone, storage-missing, or error.</param>
public sealed record ImportOutcome(
    int Created, int ErrorCount, int ErrorRows, IReadOnlyList<ImportRowError> Errors, bool ErrorsTruncated,
    string? FirstKey, string? LastKey, string? FailureCode, string? FailureBy = null);

public sealed record ExportOptions(Guid AccountId, string FileName, string Scope, string? Filter, string? Sort, string Locale);

/// <summary>Where the Ops job rows live. Application asks; Infrastructure reads and writes them.</summary>
public interface IBoardTransferJobs
{
    void Add(TransferJob job);
    Task<TransferJob?> FindAsync(TransferKind kind, Guid id, CancellationToken ct);
    /// <summary>Copies the job's current state onto its stored row.</summary>
    Task UpdateAsync(TransferJob job, CancellationToken ct);
    Task<IReadOnlyList<TransferJob>> ListExportsAsync(Guid boardId, Guid userId, DateTimeOffset since, CancellationToken ct);
    /// <summary>Worker: takes the oldest queued job (import first), marking it running; null when none waits.</summary>
    Task<TransferJob?> ClaimNextAsync(DateTimeOffset now, CancellationToken ct);
    /// <summary>Worker: finished exports whose file is past its expiry.</summary>
    Task<IReadOnlyList<TransferJob>> ListExpiredExportsAsync(DateTimeOffset now, CancellationToken ct);
    /// <summary>Worker: imports left in the queue or running by a stopped process, and old finished source files.</summary>
    Task<IReadOnlyList<TransferJob>> ListStaleImportsAsync(DateTimeOffset now, CancellationToken ct);
}

/// <summary>Live counts of a running job, kept in memory (the worker and the API are one process).</summary>
public interface ITransferProgress
{
    void Set(Guid jobId, string phase, int processed, int total);
    (string Phase, int Processed, int Total)? Get(Guid jobId);
    void Clear(Guid jobId);
}
