using Notrelix.Application.Common.Storage;
using Notrelix.Application.Common.Tenancy;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Transfer;

public sealed record ExportOutcome(long Size, string? FailureCode);

/// <param name="Status">queued, running, ready, failed or expired.</param>
public sealed record ExportJobDto(
    Guid Id, string Status, string Format, string Scope, string FileName, int RowCount, int Processed, long? Size,
    DateTimeOffset? ExpiresAt, string? FailureCode, DateTimeOffset CreatedAt);

public sealed record ExportSummaryDto(int ViewCount, int AllCount, IReadOnlyList<string> HiddenFields);

public sealed record ExportContentDto(Stream Content, string FileName, string ContentType, long Size);

public sealed record ExportRequestBody(string Format, string Scope, string? Filter, string? Sort, string? Locale);

internal static class ExportJobs
{
    public static readonly TimeSpan FileLife = TimeSpan.FromHours(24);

    /// <summary>A guest may see a board but never takes its data out as a file.</summary>
    public static async Task RequireMemberAsync(IWorkspaceAccessResolver access, Guid workspaceId, Guid userId, CancellationToken ct)
    {
        if ((await access.ResolveAsync(workspaceId, userId, ct)).IsGuest)
            throw new ForbiddenException("Guests cannot export a board.", "work-management.export-guest");
    }

    public static async Task<TransferJob> OwnAsync(IBoardTransferJobs jobs, ICurrentRequestContext me, Guid boardId, Guid jobId, CancellationToken ct)
    {
        var job = await jobs.FindAsync(TransferKind.Export, jobId, ct);
        if (job is null || job.BoardId != boardId || job.UserId != me.UserId) throw new NotFoundException("BoardExport", jobId);
        return job;
    }

    public static ExportJobDto ToDto(TransferJob job, ITransferProgress progress, IDateTimeProvider clock)
    {
        var options = job.Options<ExportOptions>();
        var outcome = job.Detail<ExportOutcome>();
        var expired = job.Status == TransferStatus.Ready && job.ExpiresAt <= clock.UtcNow;
        var live = job.Status == TransferStatus.Running ? progress.Get(job.Id) : null;
        return new ExportJobDto(
            job.Id, expired ? TransferStatus.Expired : job.Status, job.Format, options.Scope, options.FileName, live?.Total ?? job.Total,
            live?.Processed ?? (job.Status == TransferStatus.Ready ? job.Total : 0), outcome?.Size, job.ExpiresAt, outcome?.FailureCode, job.CreatedAt);
    }
}

/// <summary>
/// Asks for a file of a board's items (UC-WM-B31): the current view under its filter and sort, or every live item.
/// A guest is refused. The file is made in the background and lands in the person's inbox when ready; it is theirs
/// alone and expires after 24 hours. A repeated key makes one export.
/// </summary>
[IdempotencyOperation("work-management.board-exports.create.v1")]
public record CreateBoardExportCommand(Guid BoardId, string Format, string Scope, string? Filter, string? Sort, string? Locale)
    : ICommand<ExportJobDto>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ImportJobs.BoardKind, BoardId);
}

public class CreateBoardExportCommandValidator : AbstractValidator<CreateBoardExportCommand>
{
    public CreateBoardExportCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Format).Must(f => f is TabularWriter.Csv or TabularWriter.Xlsx).WithMessage("The format is csv or xlsx.");
        RuleFor(x => x.Scope).Must(s => s is BoardExportBuilder.ScopeView or BoardExportBuilder.ScopeAll).WithMessage("The scope is view or all.");
        RuleFor(x => x.Locale).Must(l => l is null or "vi" or "en").WithMessage("The locale is vi or en.");
    }
}

public class CreateBoardExportCommandHandler(
    IWorkManagementDbContext context, IBoardTransferJobs jobs, IWorkspaceAccessResolver access, ITransferProgress progress,
    ICurrentRequestContext me, IDateTimeProvider clock) : IRequestHandler<CreateBoardExportCommand, ExportJobDto>
{
    public async Task<ExportJobDto> Handle(CreateBoardExportCommand request, CancellationToken ct)
    {
        var board = await context.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == request.BoardId, ct)
            ?? throw new NotFoundException(nameof(Board), request.BoardId);
        await ExportJobs.RequireMemberAsync(access, board.WorkspaceId, me.UserId, ct);
        if (request.Scope == BoardExportBuilder.ScopeView)
            BoardItems.Services.BoardItemCriteria.Parse(request.Filter, request.Sort); // refuses a malformed filter now, not in the background

        var now = clock.UtcNow;
        var name = string.Concat(board.Title.Where(c => !Path.GetInvalidFileNameChars().Contains(c))).Trim();
        var options = new ExportOptions(me.RequireAccountId(), $"{name} - {now:yyyy-MM-dd}.{request.Format}", request.Scope, request.Filter, request.Sort, request.Locale ?? "vi");
        var job = new TransferJob
        {
            Kind = TransferKind.Export, Id = Guid.CreateVersion7(), WorkspaceId = board.WorkspaceId, BoardId = board.Id, UserId = me.UserId,
            Status = TransferStatus.Queued, Format = request.Format, OptionsJson = TransferJson.Write(options), CreatedAt = now,
        };
        jobs.Add(job);
        return ExportJobs.ToDto(job, progress, clock);
    }
}

public record GetBoardExportQuery(Guid BoardId, Guid JobId)
    : IQuery<ExportJobDto>, IReadRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ImportJobs.BoardKind, BoardId);
}

public class GetBoardExportQueryHandler(IBoardTransferJobs jobs, ITransferProgress progress, ICurrentRequestContext me, IDateTimeProvider clock)
    : IRequestHandler<GetBoardExportQuery, ExportJobDto>
{
    public async Task<ExportJobDto> Handle(GetBoardExportQuery request, CancellationToken ct) =>
        ExportJobs.ToDto(await ExportJobs.OwnAsync(jobs, me, request.BoardId, request.JobId, ct), progress, clock);
}

/// <summary>The caller's exports of this board from the last 24 hours, newest first.</summary>
public record ListBoardExportsQuery(Guid BoardId)
    : IQuery<IReadOnlyList<ExportJobDto>>, IReadRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ImportJobs.BoardKind, BoardId);
}

public class ListBoardExportsQueryHandler(IBoardTransferJobs jobs, ITransferProgress progress, ICurrentRequestContext me, IDateTimeProvider clock)
    : IRequestHandler<ListBoardExportsQuery, IReadOnlyList<ExportJobDto>>
{
    public async Task<IReadOnlyList<ExportJobDto>> Handle(ListBoardExportsQuery request, CancellationToken ct) =>
        (await jobs.ListExportsAsync(request.BoardId, me.UserId, clock.UtcNow - ExportJobs.FileLife, ct))
            .Select(j => ExportJobs.ToDto(j, progress, clock)).ToList();
}

/// <summary>How many items an export would hold, for the dialog. Guests are refused here too.</summary>
public record GetBoardExportSummaryQuery(Guid BoardId, string? Filter, string? Sort)
    : IQuery<ExportSummaryDto>, IReadRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ImportJobs.BoardKind, BoardId);
}

public class GetBoardExportSummaryQueryHandler(
    IWorkManagementDbContext context, BoardExportBuilder builder, IWorkspaceAccessResolver access, ICurrentRequestContext me)
    : IRequestHandler<GetBoardExportSummaryQuery, ExportSummaryDto>
{
    public async Task<ExportSummaryDto> Handle(GetBoardExportSummaryQuery request, CancellationToken ct)
    {
        var board = await context.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == request.BoardId, ct)
            ?? throw new NotFoundException(nameof(Board), request.BoardId);
        await ExportJobs.RequireMemberAsync(access, board.WorkspaceId, me.UserId, ct);
        var (view, all) = await builder.CountAsync(request.BoardId, request.Filter, request.Sort, ct);
        return new ExportSummaryDto(view, all, []);
    }
}

/// <summary>The file of a ready export, for the person who asked: authorised on every call, never after its expiry.</summary>
public record GetBoardExportContentQuery(Guid BoardId, Guid JobId)
    : IQuery<ExportContentDto>, IReadRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ImportJobs.BoardKind, BoardId);
}

public class GetBoardExportContentQueryHandler(
    IBoardTransferJobs jobs, IStorageService storage, IWorkspaceAccessResolver access, ICurrentRequestContext me, IDateTimeProvider clock)
    : IRequestHandler<GetBoardExportContentQuery, ExportContentDto>
{
    public async Task<ExportContentDto> Handle(GetBoardExportContentQuery request, CancellationToken ct)
    {
        var job = await ExportJobs.OwnAsync(jobs, me, request.BoardId, request.JobId, ct);
        await ExportJobs.RequireMemberAsync(access, job.WorkspaceId, me.UserId, ct);
        if (job.Status != TransferStatus.Ready || job.StorageKey is null || job.ExpiresAt <= clock.UtcNow)
            throw new NotFoundException("BoardExportFile", request.JobId);
        var content = await storage.OpenReadAsync(job.StorageKey, ct) ?? throw new NotFoundException("BoardExportFile", request.JobId);
        var type = job.Format == TabularWriter.Xlsx
            ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            : "text/csv; charset=utf-8";
        return new ExportContentDto(content, job.Options<ExportOptions>().FileName, type, job.Detail<ExportOutcome>()?.Size ?? 0);
    }
}
