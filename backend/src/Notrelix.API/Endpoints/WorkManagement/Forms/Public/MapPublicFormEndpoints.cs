using Notrelix.API.Contracts.WorkManagement.Forms.Requests;
using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.SubmitPublicForm;
using Notrelix.Application.Features.WorkManagement.Forms.Queries.GetPublicForm;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Public;

/// <summary>
/// The anonymous face of a public form, addressed only by its unguessable key. Both routes are limited per
/// address and per form before anything runs; a key that opens nothing (private, closed, archived, unknown)
/// is a 404 that does not say which.
/// </summary>
public static class MapPublicFormEndpoints
{
    public static IEndpointRouteBuilder MapPublicForms(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/public/forms/{publicKey}")
            .WithTags("WorkManagement.PublicForms")
            .WithOpenApi();

        group.MapPublicGet("/", GetAsync)
            .WithMetadata(new RateLimitPolicyAttribute("PublicFormReadByIpAndForm"))
            .WithMetadata(new RateLimitPolicyAttribute("PublicFormByForm"))
            .WithName("WorkManagement.PublicForms.Get")
            .WithSummary("The form an anonymous visitor fills");

        group.MapPublicPost("/submissions", SubmitAsync)
            .WithMetadata(new RateLimitPolicyAttribute("PublicFormSubmitByIpAndForm"))
            .WithMetadata(new RateLimitPolicyAttribute("PublicFormByForm"))
            .WithName("WorkManagement.PublicForms.Submit")
            .WithSummary("Submit answers to a public form anonymously, with a captcha token");
        return app;
    }

    private static async Task<IResult> GetAsync(string publicKey, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPublicFormQuery(publicKey), cancellationToken);
        return result.ToApiResult();
    }

    private static async Task<IResult> SubmitAsync(
        string publicKey,
        SubmitPublicFormRequest body,
        HttpContext http,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SubmitPublicFormCommand(publicKey, body.Answers, body.CaptchaToken, body.Website, http.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);
        return result.ToNoContentResult();
    }
}
