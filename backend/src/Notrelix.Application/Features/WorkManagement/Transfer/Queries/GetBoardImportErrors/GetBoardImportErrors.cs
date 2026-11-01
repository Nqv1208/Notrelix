using System.Text.Json.Serialization;
using FluentValidation.Results;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Transfer;

/// <summary>The rows a refused import could not take, for the downloadable report (the client words the reasons).</summary>
public record GetBoardImportErrorsQuery(Guid BoardId, Guid JobId)
    : IQuery<ImportErrorsDto>, IReadRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.CreateItem;
    public ResourceRef Resource => ResourceRef.Create(ImportJobs.BoardKind, BoardId);
}

public class GetBoardImportErrorsQueryHandler(IBoardTransferJobs jobs, ICurrentRequestContext me)
    : IRequestHandler<GetBoardImportErrorsQuery, ImportErrorsDto>
{
    public async Task<ImportErrorsDto> Handle(GetBoardImportErrorsQuery request, CancellationToken ct)
    {
        var job = await ImportJobs.OwnAsync(jobs, me, request.BoardId, request.JobId, ct);
        var options = job.Options<ImportOptions>();
        var outcome = job.Detail<ImportOutcome>();
        return new ImportErrorsDto(outcome?.Errors ?? [], outcome?.ErrorsTruncated ?? false, options.Headers, options.FileName);
    }
}
