using System.Text.Json.Serialization;
using FluentValidation.Results;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Transfer;

/// <summary>Step 2 and 3: checks a mapping against the stored file and the board as it is now. Reads only.</summary>
/// <param name="AllErrors">Return every stored error (for the report) instead of the first 20.</param>
public record PreviewBoardImportQuery(Guid BoardId, Guid JobId, IReadOnlyList<ColumnMap> Mapping, bool AllErrors = false)
    : IQuery<ImportPreviewDto>, IReadRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.CreateItem;
    public ResourceRef Resource => ResourceRef.Create(ImportJobs.BoardKind, BoardId);
}

public class PreviewBoardImportQueryHandler(
    IWorkManagementDbContext context, IBoardTransferJobs jobs, IStorageService storage, ICurrentRequestContext me)
    : IRequestHandler<PreviewBoardImportQuery, ImportPreviewDto>
{
    public async Task<ImportPreviewDto> Handle(PreviewBoardImportQuery request, CancellationToken ct)
    {
        var job = await ImportJobs.OwnAsync(jobs, me, request.BoardId, request.JobId, ct);
        if (job.Status != TransferStatus.Uploaded) throw new ConflictException("The import was already sent.");
        var data = await ImportJobs.ReadAsync(storage, job.Options<ImportOptions>(), ct);
        var schema = await ImportJobs.SchemaAsync(context, request.BoardId, ct);
        var missing = !request.Mapping.Any(m => m.Target == ImportPlanner.Title || (schema.TitleField is { } t && m.Target == t.Id.ToString()));
        var result = ImportPlanner.Evaluate(data, schema, request.Mapping, collectItems: false);
        return new ImportPreviewDto(
            data.TotalRows, missing, result.Checks, result.ErrorCount, result.ErrorRows, result.Errors.Take(request.AllErrors ? ImportPlanner.MaxStoredErrors : 20).ToList(), result.Sample);
    }
}
