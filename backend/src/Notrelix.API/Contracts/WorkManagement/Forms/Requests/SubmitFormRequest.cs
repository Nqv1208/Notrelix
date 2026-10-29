using System.Text.Json;

namespace Notrelix.API.Contracts.WorkManagement.Forms.Requests;

/// <summary>Answers keyed by the form's question key.</summary>
public record SubmitFormRequest(Dictionary<string, JsonElement> Answers);
