namespace Notrelix.API.Contracts.WorkManagement.Forms.Requests;

public record UpdateFormQuestionRequest(
    string Label,
    bool IsRequired = false,
    string? ConfigJson = null,
    string? Position = null);
