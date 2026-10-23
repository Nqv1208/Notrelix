using Notrelix.API.Contracts.Workspaces.Onboarding.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Onboarding.Commands.ClearOnboardingDraft;
using Notrelix.Application.Features.Workspaces.Onboarding.Commands.DismissGettingStarted;
using Notrelix.Application.Features.Workspaces.Onboarding.Commands.SaveOnboardingDraft;
using Notrelix.Application.Features.Workspaces.Onboarding.Queries.GetGettingStarted;
using Notrelix.Application.Features.Workspaces.Onboarding.Queries.GetOnboardingDraft;

namespace Notrelix.API.Endpoints.Workspaces.Onboarding;

/// <summary>
/// Workspace onboarding owned by Workspaces: the caller's own unfinished draft (30-day TTL) and the
/// per-workspace getting-started card. The slug check lives with the Account workspace routes.
/// </summary>
public static class MapOnboardingEndpoints
{
    public static IEndpointRouteBuilder RegisterWorkspaceOnboardingEndpoints(this IEndpointRouteBuilder app)
    {
        var draft = app
            .MapGroup("/api/v1/workspaces/onboarding-draft")
            .WithTags("Workspaces.Onboarding")
            .WithOpenApi();

        draft.MapAuthenticatedGet("/", async (ISender sender) =>
                (await sender.Send(new GetOnboardingDraftQuery())).ToApiResult())
            .WithName("Workspaces.Onboarding.GetOnboardingDraft")
            .WithSummary("Get the caller's unfinished onboarding")
            .WithDescription("Only the caller's own draft; an expired draft reads as absent (draft is null).")
            .Produces<OnboardingDraftResponseDto>(StatusCodes.Status200OK, "application/json");

        draft.MapAuthenticatedPut("/", async (SaveOnboardingDraftRequest body, ISender sender) =>
                (await sender.Send(new SaveOnboardingDraftCommand(body.Step, body.WorkspaceId, body.Data))).ToNoContentResult())
            .WithName("Workspaces.Onboarding.SaveOnboardingDraft")
            .WithSummary("Save the caller's unfinished onboarding")
            .WithDescription("Replaces the one draft of the caller and restarts its 30 days; idempotent.")
            .Produces(StatusCodes.Status204NoContent);

        draft.MapAuthenticatedDelete("/", async (ISender sender) =>
                (await sender.Send(new ClearOnboardingDraftCommand())).ToNoContentResult())
            .WithName("Workspaces.Onboarding.ClearOnboardingDraft")
            .WithSummary("Clear the caller's unfinished onboarding")
            .WithDescription("Succeeds when there is no draft.")
            .Produces(StatusCodes.Status204NoContent);

        var gettingStarted = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/getting-started")
            .WithTags("Workspaces.Onboarding")
            .WithOpenApi();

        gettingStarted.MapWorkspaceGet("/", async (Guid workspaceId, ISender sender) =>
                (await sender.Send(new GetGettingStartedQuery(workspaceId))).ToApiResult())
            .WithName("Workspaces.Onboarding.GetGettingStarted")
            .WithSummary("Get the getting-started card of a workspace")
            .WithDescription("Derived items (first-item from the Boards the caller can read) and whether the caller dismissed the card.")
            .Produces<GettingStartedDto>(StatusCodes.Status200OK, "application/json");

        gettingStarted.MapWorkspacePost("/dismiss", async (Guid workspaceId, ISender sender) =>
                (await sender.Send(new DismissGettingStartedCommand(workspaceId))).ToNoContentResult())
            .WithName("Workspaces.Onboarding.DismissGettingStarted")
            .WithSummary("Hide the getting-started card for the caller")
            .WithDescription("Stored per user and workspace; dismissing again is a no-op.")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}
