using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Transfer;

/// <summary>
/// Step 4: sends the import. The file must check clean under this mapping, all or nothing; the creating itself runs in
/// the background so closing the dialog is safe. Repeating the call (same key or not) never queues a second run.
/// </summary>
[IdempotencyOperation("work-management.board-imports.confirm.v1")]
public record ConfirmBoardImportCommand(Guid BoardId, Guid JobId, IReadOnlyList<ColumnMap> Mapping)
    : ICommand<ImportJobDto>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.CreateItem;
    public ResourceRef Resource => ResourceRef.Create(ImportJobs.BoardKind, BoardId);
}

public class ConfirmBoardImportCommandHandler(
    IWorkManagementDbContext context, IBoardTransferJobs jobs, IStorageService storage, ITransferProgress progress, ICurrentRequestContext me,
    ImportPeopleDirectory directory)
    : IRequestHandler<ConfirmBoardImportCommand, ImportJobDto>
{
    public async Task<ImportJobDto> Handle(ConfirmBoardImportCommand request, CancellationToken ct)
    {
        var job = await ImportJobs.OwnAsync(jobs, me, request.BoardId, request.JobId, ct);
        if (job.Status != TransferStatus.Uploaded) return ImportJobs.ToDto(job, progress);

        var options = job.Options<ImportOptions>();
        var data = await ImportJobs.ReadAsync(storage, options, ct);
        var people = await directory.ResolveAsync(me.RequireWorkspaceId(), request.BoardId, me.UserId, data, ct);
        var schema = await ImportJobs.SchemaAsync(context, request.BoardId, ct, people);
        ImportJobs.RequireTitle(request.Mapping, schema);
        var evaluation = ImportPlanner.Evaluate(data, schema, request.Mapping, collectItems: false);
        if (evaluation.ErrorCount > 0)
            throw new Notrelix.Application.Common.Exceptions.ValidationException([new ValidationFailure("file", "has-errors")]);
        // A file that would take the board over its active-item limit is refused before it is queued (D78.8); the
        // run checks again under the key reservation, so a board filled in the meantime still refuses it whole.
        await Limits.WorkLimits.EnsureItemRoomAsync(context, request.BoardId, data.Rows.Count, ct);

        job.OptionsJson = TransferJson.Write(options with { Mapping = request.Mapping });
        job.Status = TransferStatus.Queued;
        await jobs.UpdateAsync(job, ct);
        return ImportJobs.ToDto(job, progress);
    }
}
