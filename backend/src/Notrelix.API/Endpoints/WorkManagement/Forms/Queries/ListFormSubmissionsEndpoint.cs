using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Forms.Queries.ListFormSubmissions;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Queries;

public static class ListFormSubmissionsEndpoint
{
    public static IEndpointRouteBuilder MapListFormSubmissions(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/submissions", HandleAsync)
            .WithName("WorkManagement.Forms.ListSubmissions")
            .WithTags("WorkManagement.Forms")
            .WithSummary("List submissions for a form");
        return group;
    }

    public static IEndpointRouteBuilder MapListFormSubmissionsPage(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/submissions/page", HandlePageAsync)
            .WithName("WorkManagement.Forms.ListSubmissionsPage")
            .WithTags("WorkManagement.Forms")
            .WithSummary("List a page of the submissions of a form")
            .WithDescription("Newest first, default 50 and at most 200 a page. Pass nextCursor back as cursor for the next page.")
            .Produces<CursorPage<FormSubmissionDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandlePageAsync(
        Guid formId,
        string? cursor,
        int? limit,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var take = TimeCursor.ClampLimit(limit);
        var result = await sender.Send(new ListFormSubmissionsQuery(formId, TimeCursor.Decode(cursor), take + 1), cancellationToken);
        if (!result.Succeeded) return result.ToApiResult();
        return Results.Ok(TimeCursor.Page(result.Data!, take, submission => (submission.SubmittedAt, submission.Id)));
    }

    private static async Task<IResult> HandleAsync(
        Guid formId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListFormSubmissionsQuery(formId), cancellationToken);
        return result.ToApiResult();
    }
}
