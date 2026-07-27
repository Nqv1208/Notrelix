using Notrelix.Domain.WorkManagement.Forms;

namespace Notrelix.API.Contracts.WorkManagement.Forms.Requests;

public record AddFormQuestionRequest(
    string QuestionKey,
    FormQuestionType QuestionType,
    string Label,
    bool IsRequired = false,
    string? ConfigJson = null,
    string? Position = null);
