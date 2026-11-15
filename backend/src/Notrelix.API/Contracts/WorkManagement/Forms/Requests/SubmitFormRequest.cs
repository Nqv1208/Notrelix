using System.Text.Json;

namespace Notrelix.API.Contracts.WorkManagement.Forms.Requests;

/// <summary>Answers keyed by the form's question key. <paramref name="Website"/> is a hidden honeypot field that a person never fills.</summary>
public record SubmitFormRequest(Dictionary<string, JsonElement> Answers, string? Website = null);
