using Notrelix.API.Contracts.Chat.Requests;
using Notrelix.API.Endpoints.Collaboration.Attachments;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Chat.Exports.Commands.CreateChatExport;
using Notrelix.Application.Features.Chat.Exports.Queries.GetChatExport;
using Notrelix.Application.Features.Chat.Exports.Queries.GetChatExportContent;
using Notrelix.Application.Features.Collaboration.Attachments.DTOs;

namespace Notrelix.API.Endpoints.Chat.Exports;

/// <summary>Export of one person's chat data for a privacy request: start, status and the permission-checked download (UC-CHT-H06, CHT-021, CHT-004).</summary>
public static class MapChatExportEndpoints
{
    public static IEndpointRouteBuilder MapChatExportRoutes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/chat/exports")
            .WithTags("Chat.Exports")
            .WithOpenApi();

        group.MapWorkspacePost("/", CreateAsync)
            .WithName("Chat.Exports.Create")
            .WithSummary("Start an export of a person's chat data")
            .WithDescription("A Workspace Owner or Admin asks for the messages a person wrote and the spaces they belong to, optionally inside a range. The export is built in the background: poll Chat.Exports.Get. It holds only what the requester may read (a private channel or conversation the requester is not in is left out and counted), no tombstone bodies and nobody else's messages. 404 when the person is not in the Workspace; 429 over the export limit. Audited; the audit names who, whose and the range, never content.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status429TooManyRequests);

        group.MapWorkspaceGet("/{exportId:guid}", GetAsync)
            .WithName("Chat.Exports.Get")
            .WithSummary("Read the state of an export")
            .WithDescription("queued, running, ready, failed (failureCode) or expired, with the counts. Only the person who asked reads it; another person's export is 404. downloadPath is present while the file can be downloaded.")
            .ProducesProblems(StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        group.MapWorkspaceGet("/{exportId:guid}/content", GetContentAsync)
            .WithName("Chat.Exports.GetContent")
            .WithSummary("Download a ready export (authorised, streamed)")
            .WithDescription("Permission checked on every request: only the Owner or Admin who asked, for 24 hours. There is no public URL. An export that is not ready, not yours or expired is 404. Never cacheable.")
            .Produces(StatusCodes.Status200OK, contentType: "application/json")
            .ProducesProblems(StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateAsync(Guid workspaceId, CreateChatExportRequest body, ISender sender)
    {
        if (body.SubjectUserId is not { } subject)
            return EndpointExtensions.InvalidInput("subjectUserId is required.");

        var result = await sender.Send(new CreateChatExportCommand(workspaceId, subject, body.RangeFrom, body.RangeTo));
        if (!result.Succeeded)
            return result.ToApiResult();

        return Results.Accepted($"/api/v1/workspaces/{workspaceId}/chat/exports/{result.Data!.Id}", result.Data);
    }

    private static async Task<IResult> GetAsync(Guid workspaceId, Guid exportId, ISender sender)
    {
        var result = await sender.Send(new GetChatExportQuery(workspaceId, exportId));
        return result.ToApiResult();
    }

    private static async Task<IResult> GetContentAsync(Guid workspaceId, Guid exportId, HttpContext httpContext, ISender sender, CancellationToken cancellationToken)
    {
        var file = await sender.Send(new GetChatExportContentQuery(workspaceId, exportId), cancellationToken);
        return AttachmentContentResponse.Create(httpContext, new AttachmentContentDto(file.Content, file.FileName, file.ContentType, file.SizeBytes));
    }
}
