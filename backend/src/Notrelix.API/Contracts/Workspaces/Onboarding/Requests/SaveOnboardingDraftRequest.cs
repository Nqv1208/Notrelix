using System.Text.Json;

namespace Notrelix.API.Contracts.Workspaces.Onboarding.Requests;

/// <param name="Step">The client's step key (1 to 64 characters).</param>
/// <param name="WorkspaceId">The workspace the draft is about once it exists.</param>
/// <param name="Data">The client's step payload: a JSON object of at most 64 KB.</param>
public record SaveOnboardingDraftRequest(string Step, Guid? WorkspaceId, JsonElement? Data);
