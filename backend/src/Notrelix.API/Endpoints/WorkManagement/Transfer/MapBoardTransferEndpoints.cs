using Microsoft.Net.Http.Headers;
using Notrelix.API.Contracts.WorkManagement.Transfer.Requests;
using Notrelix.API.ErrorHandling;
using Notrelix.API.Extensions;
using Notrelix.API.Uploads;
using Notrelix.Application.Features.WorkManagement.Transfer;

namespace Notrelix.API.Endpoints.WorkManagement.Transfer;

/// <summary>Importing items from a file and exporting a board or view to a file (UC-WM-B29, B30, B31).</summary>
public static class MapBoardTransferEndpoints
{
    private const string Tag = "WorkManagement.Transfer";

    public static IEndpointRouteBuilder MapBoardTransfer(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/imports", UploadAsync)
            .WithName("WorkManagement.Imports.Upload").WithTags(Tag)
            .WithSummary("Upload a CSV or XLSX file to import items from (multipart/form-data, streamed); nothing is created yet")
            .Accepts<ImportFileForm>("multipart/form-data")
            .Produces<ImportUploadDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge);
        group.MapResourcePost("/imports/{jobId:guid}/preview", async (Guid boardId, Guid jobId, ImportMappingRequest body, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new PreviewBoardImportQuery(boardId, jobId, Map(body), body.AllErrors), ct)))
            .WithName("WorkManagement.Imports.Preview").WithTags(Tag)
            .WithSummary("Check a column mapping against the uploaded file: per-column checks and per-row errors")
            .Produces<ImportPreviewDto>(StatusCodes.Status200OK, "application/json");
        group.MapResourcePost("/imports/{jobId:guid}/confirm", async (Guid boardId, Guid jobId, ImportMappingRequest body, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ConfirmBoardImportCommand(boardId, jobId, Map(body)), ct)))
            .WithIdempotencyKey()
            .WithName("WorkManagement.Imports.Confirm").WithTags(Tag)
            .WithSummary("Send the import: all rows or none, created in the background; a retry queues no second run")
            .Produces<ImportJobDto>(StatusCodes.Status200OK, "application/json");
        group.MapResourceGet("/imports/{jobId:guid}", async (Guid boardId, Guid jobId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetBoardImportQuery(boardId, jobId), ct)))
            .WithName("WorkManagement.Imports.Get").WithTags(Tag)
            .WithSummary("Progress and outcome of the caller's import")
            .Produces<ImportJobDto>(StatusCodes.Status200OK, "application/json");
        group.MapResourceGet("/imports/{jobId:guid}/errors", async (Guid boardId, Guid jobId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetBoardImportErrorsQuery(boardId, jobId), ct)))
            .WithName("WorkManagement.Imports.GetErrors").WithTags(Tag)
            .WithSummary("The rows a refused import could not take, for the downloadable error report")
            .Produces<ImportErrorsDto>(StatusCodes.Status200OK, "application/json");

        group.MapResourcePost("/exports", async (Guid boardId, ExportRequest body, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new CreateBoardExportCommand(boardId, body.Format, body.Scope, body.Filter, body.Sort, body.Locale), ct)))
            .WithIdempotencyKey()
            .WithName("WorkManagement.Exports.Create").WithTags(Tag)
            .WithSummary("Ask for a CSV or XLSX of the current view or all items; made in the background, 24 hours to download")
            .Produces<ExportJobDto>(StatusCodes.Status200OK, "application/json");
        group.MapResourceGet("/exports", async (Guid boardId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListBoardExportsQuery(boardId), ct)))
            .WithName("WorkManagement.Exports.List").WithTags(Tag)
            .WithSummary("The caller's exports of this board from the last 24 hours")
            .Produces<IReadOnlyList<ExportJobDto>>(StatusCodes.Status200OK, "application/json");
        group.MapResourceGet("/exports/summary", async (Guid boardId, string? filter, string? sort, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetBoardExportSummaryQuery(boardId, filter, sort), ct)))
            .WithName("WorkManagement.Exports.GetSummary").WithTags(Tag)
            .WithSummary("How many items the current view and the whole board would export")
            .Produces<ExportSummaryDto>(StatusCodes.Status200OK, "application/json");
        group.MapResourceGet("/exports/{jobId:guid}", async (Guid boardId, Guid jobId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetBoardExportQuery(boardId, jobId), ct)))
            .WithName("WorkManagement.Exports.Get").WithTags(Tag)
            .WithSummary("Progress of the caller's export")
            .Produces<ExportJobDto>(StatusCodes.Status200OK, "application/json");
        group.MapResourceGet("/exports/{jobId:guid}/file", DownloadAsync)
            .WithName("WorkManagement.Exports.GetFile").WithTags(Tag)
            .WithSummary("Download the caller's ready export (authorised on every call; gone after 24 hours)")
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
            .ProducesProblem(StatusCodes.Status404NotFound);
        return group;
    }

    private static IReadOnlyList<ColumnMap> Map(ImportMappingRequest body) =>
        body.Mapping.Select(m => new ColumnMap(m.Column, m.Target)).ToList();

    private static async Task<IResult> UploadAsync(Guid boardId, HttpContext http, ISender sender, CancellationToken ct)
    {
        var part = await MultipartUpload.ReadFilePartAsync(
            http, TabularReader.MaxFileBytes, ErrorCodes.TransferUnsupportedMediaType, ErrorCodes.TransferPayloadTooLarge, ct);
        if (part.Failure is not null) return part.Failure;
        return Results.Ok(await sender.Send(new StartBoardImportCommand(boardId, part.FileName, part.Body!), ct));
    }

    private static async Task<IResult> DownloadAsync(Guid boardId, Guid jobId, HttpContext http, ISender sender, CancellationToken ct)
    {
        var file = await sender.Send(new GetBoardExportContentQuery(boardId, jobId), ct);
        var disposition = new ContentDispositionHeaderValue("attachment");
        disposition.SetHttpFileName(file.FileName);
        var headers = http.Response.Headers;
        headers[HeaderNames.ContentDisposition] = disposition.ToString();
        headers[HeaderNames.XContentTypeOptions] = "nosniff";
        headers[HeaderNames.CacheControl] = "private, no-store";
        headers[HeaderNames.ContentSecurityPolicy] = "default-src 'none'; sandbox";
        return Results.Stream(file.Content, file.ContentType, enableRangeProcessing: false);
    }
}
