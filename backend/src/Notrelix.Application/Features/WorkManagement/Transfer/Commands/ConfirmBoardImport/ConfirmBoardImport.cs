using System.Text.Json.Serialization;
using FluentValidation.Results;
using Notrelix.Application.Common.Data;
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
    IWorkManagementDbContext context, IBoardTransferJobs jobs, IStorageService storage, ITransferProgress progress, ICurrentRequestContext me)
    : IRequestHandler<ConfirmBoardImportCommand, ImportJobDto>
{
    public async Task<ImportJobDto> Handle(ConfirmBoardImportCommand request, CancellationToken ct)
    {
        var job = await ImportJobs.OwnAsync(jobs, me, request.BoardId, request.JobId, ct);
        if (job.Status != TransferStatus.Uploaded) return ImportJobs.ToDto(job, progress);

        var options = job.Options<ImportOptions>();
        var schema = await ImportJobs.SchemaAsync(context, request.BoardId, ct);
        ImportJobs.RequireTitle(request.Mapping, schema);
        var evaluation = ImportPlanner.Evaluate(await ImportJobs.ReadAsync(storage, options, ct), schema, request.Mapping, collectItems: false);
        if (evaluation.ErrorCount > 0)
            throw new Notrelix.Application.Common.Exceptions.ValidationException([new ValidationFailure("file", "has-errors")]);

        job.OptionsJson = TransferJson.Write(options with { Mapping = request.Mapping });
        job.Status = TransferStatus.Queued;
        await jobs.UpdateAsync(job, ct);
        return ImportJobs.ToDto(job, progress);
    }
}
