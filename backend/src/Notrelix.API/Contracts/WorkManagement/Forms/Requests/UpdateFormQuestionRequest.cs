namespace Notrelix.API.Contracts.WorkManagement.Forms.Requests;

public record UpdateFormQuestionRequest(
    string Label,
    long ExpectedVersion,
    bool IsRequired = false,
    string? ConfigJson = null,
    string? Position = null,
    Guid? BoardFieldId = null);
