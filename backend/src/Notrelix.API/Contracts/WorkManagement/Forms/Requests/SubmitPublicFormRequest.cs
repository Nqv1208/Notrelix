using System.Text.Json;

namespace Notrelix.API.Contracts.WorkManagement.Forms.Requests;

/// <summary>
/// An anonymous visitor's answers keyed by question key, with the captcha token the page obtained.
/// <paramref name="Website"/> is the hidden honeypot field a person never fills.
/// </summary>
public record SubmitPublicFormRequest(Dictionary<string, JsonElement> Answers, string? CaptchaToken, string? Website = null);
