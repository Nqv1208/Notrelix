using System.Text.Json;

namespace Notrelix.Domain.Workspaces.Onboarding;

/// <summary>
/// A person's unfinished workspace onboarding, kept so they can resume (accepted 2026-10-23: owned by
/// Workspaces, keyed by user, 30-day TTL). One draft per user; it is personal state, never shared and
/// never a source of workspace truth. <see cref="Data"/> is the client's opaque step payload (a JSON
/// object); the server validates only that it is a bounded JSON object.
/// </summary>
public sealed class OnboardingDraft : Entity
{
    /// <summary>A draft lives 30 days after its last save.</summary>
    public static readonly TimeSpan Retention = TimeSpan.FromDays(30);

    public const int MaxStepLength = 64;
    public const int MaxDataLength = 65_536;

    public Guid UserId { get; private set; }
    public string Step { get; private set; } = null!;

    /// <summary>The workspace the draft is about once it exists (after the company step); not validated here.</summary>
    public Guid? WorkspaceId { get; private set; }

    public string Data { get; private set; } = "{}";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    private OnboardingDraft() : base() { }

    public static OnboardingDraft Create(Guid userId, string step, Guid? workspaceId, string? dataJson, DateTimeOffset now)
    {
        Guard.NotEmpty(userId);
        var draft = new OnboardingDraft { UserId = userId, CreatedAt = now };
        draft.Apply(step, workspaceId, dataJson, now);
        return draft;
    }

    /// <summary>Replaces the whole draft and restarts its 30 days. Invalid input changes nothing.</summary>
    public void Replace(string step, Guid? workspaceId, string? dataJson, DateTimeOffset now) =>
        Apply(step, workspaceId, dataJson, now);

    public bool IsExpiredAt(DateTimeOffset now) => ExpiresAt <= now;

    private void Apply(string step, Guid? workspaceId, string? dataJson, DateTimeOffset now)
    {
        var normalizedStep = (step ?? string.Empty).Trim();
        if (normalizedStep.Length == 0 || normalizedStep.Length > MaxStepLength || normalizedStep.Any(char.IsControl))
        {
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Onboarding_DraftStepInvalid,
                $"A draft step is 1 to {MaxStepLength} characters without control characters.");
        }

        var data = string.IsNullOrWhiteSpace(dataJson) ? "{}" : dataJson;
        if (data.Length > MaxDataLength)
        {
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Onboarding_DraftDataTooLarge,
                $"A draft holds at most {MaxDataLength} characters of data.");
        }

        try
        {
            using var document = JsonDocument.Parse(data);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new JsonException("not an object");
        }
        catch (JsonException)
        {
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Onboarding_DraftDataInvalid,
                "Draft data must be a JSON object.");
        }

        Step = normalizedStep;
        WorkspaceId = workspaceId == Guid.Empty ? null : workspaceId;
        Data = data;
        UpdatedAt = now;
        ExpiresAt = now + Retention;
    }
}
