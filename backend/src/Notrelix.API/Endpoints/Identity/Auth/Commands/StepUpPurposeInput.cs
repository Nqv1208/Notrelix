using Notrelix.Application.Features.Identity.Security.DTOs;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

/// <summary>Reads a step-up purpose by name only: a number or an unknown name is refused, never mapped to a purpose.</summary>
internal static class StepUpPurposeInput
{
    public static bool TryParse(string? value, out StepUpPurpose purpose)
    {
        purpose = default;
        return !string.IsNullOrWhiteSpace(value)
            && Enum.GetNames<StepUpPurpose>().Contains(value, StringComparer.OrdinalIgnoreCase)
            && Enum.TryParse(value, ignoreCase: true, out purpose);
    }
}
