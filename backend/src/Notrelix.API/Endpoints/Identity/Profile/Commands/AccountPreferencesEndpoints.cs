using Notrelix.API.Extensions;
using Notrelix.Application.Features.Identity.Profiles.Commands.UpdateAccountPreferences;
using Notrelix.Application.Features.Identity.Profiles.DTOs;
using Notrelix.Application.Features.Identity.Profiles.Queries.GetAccountPreferences;

namespace Notrelix.API.Endpoints.Identity.Profile.Commands;

/// <summary>
/// Partial update of the caller's account preferences. <c>expectedVersion</c> is the
/// <c>version</c> returned by the last read (0 when none was saved) and is required.
/// </summary>
public sealed record UpdateAccountPreferencesRequest(
    long? ExpectedVersion,
    string? Locale = null,
    string? Timezone = null,
    string? Theme = null,
    string? Phone = null,
    bool? PhoneVisibleToWorkspaceMembers = null,
    bool? ColourBlindLabels = null);

public static class AccountPreferencesEndpoints
{
    public static IEndpointRouteBuilder MapAccountPreferences(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedGet("/preferences", GetAsync)
            .WithName("Identity.Profile.GetPreferences")
            .Produces<AccountPreferencesDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status401Unauthorized)
            .WithTags("Identity.Profile")
            .WithSummary("Get the current user's account preferences")
            .WithDescription("Locale, time zone and theme the user chose as account-level defaults, plus their own phone (E.164) and whether Workspace members may see it (default false). Returns the defaults with version 0 until the first save.");

        group.MapAuthenticatedPatch("/preferences", UpdateAsync)
            .WithName("Identity.Profile.UpdatePreferences")
            .Produces<AccountPreferencesDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status412PreconditionFailed)
            .WithTags("Identity.Profile")
            .WithSummary("Update the current user's account preferences")
            .WithDescription("Partial update; omitted members keep their value; a blank phone clears it. Answers 412 when expectedVersion is stale.");

        return group;
    }

    private static async Task<IResult> GetAsync(ISender sender)
        => (await sender.Send(new GetAccountPreferencesQuery())).ToApiResult();

    private static async Task<IResult> UpdateAsync(UpdateAccountPreferencesRequest request, ISender sender)
    {
        if (request.ExpectedVersion is not { } expectedVersion)
        {
            return EndpointExtensions.InvalidInput("expectedVersion is required.");
        }

        var result = await sender.Send(new UpdateAccountPreferencesCommand
        {
            ExpectedVersion = expectedVersion,
            Locale = request.Locale,
            Timezone = request.Timezone,
            Theme = request.Theme,
            Phone = request.Phone,
            PhoneVisibleToWorkspaceMembers = request.PhoneVisibleToWorkspaceMembers,
            ColourBlindLabels = request.ColourBlindLabels
        });
        return result.ToApiResult();
    }
}
