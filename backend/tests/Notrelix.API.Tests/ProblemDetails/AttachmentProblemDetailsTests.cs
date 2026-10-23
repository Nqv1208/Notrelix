using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Notrelix.API.ErrorHandling;
using Notrelix.Domain.Collaboration;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.API.Tests.ProblemDetails;

/// <summary>
/// Public error contract of the attachment file upload (api-and-contracts.md section 24A):
/// 409 attachment.limit_exceeded, 413 attachment.payload_too_large, 415
/// attachment.unsupported_media_type. Pinned at the exception-to-ProblemDetails seam.
/// </summary>
public class AttachmentProblemDetailsTests
{
    private readonly GlobalExceptionHandler _handler = new(
        Mock.Of<ILogger<GlobalExceptionHandler>>(),
        Microsoft.Extensions.Options.Options.Create(new IdempotencyOptions()));

    public static TheoryData<Exception, int, string> Cases => new()
    {
        {
            new DomainBusinessRuleException(
                CollaborationRuleCodes.Collaboration_Attachment_MaxAttachmentsExceeded, "limit"),
            StatusCodes.Status409Conflict, "attachment.limit_exceeded"
        },
        {
            new DomainBusinessRuleException(
                CollaborationRuleCodes.Collaboration_Attachment_FileSizeExceeded, "too big"),
            StatusCodes.Status413PayloadTooLarge, "attachment.payload_too_large"
        },
        { new PayloadTooLargeException(1024), StatusCodes.Status413PayloadTooLarge, "attachment.payload_too_large" },
        {
            new BadHttpRequestException("too large", StatusCodes.Status413PayloadTooLarge),
            StatusCodes.Status413PayloadTooLarge, "attachment.payload_too_large"
        },
        {
            new UnsupportedMediaTypeException("blocked"),
            StatusCodes.Status415UnsupportedMediaType, "attachment.unsupported_media_type"
        },
        {
            new BadHttpRequestException("interrupted", StatusCodes.Status400BadRequest),
            StatusCodes.Status400BadRequest, "validation.failed"
        },
        {
            new InvalidDataException("bad multipart"),
            StatusCodes.Status400BadRequest, "validation.failed"
        },
        {
            // Other attachment rules keep the generic business-rule mapping.
            new DomainBusinessRuleException(
                CollaborationRuleCodes.Collaboration_Attachment_FileSizeMustBePositive, "empty"),
            StatusCodes.Status400BadRequest, "business_rule.violation"
        },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Exception_MapsToItsPublicStatusAndErrorCode(Exception exception, int status, string errorCode)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/v1/board-items/x/attachments/files";

        await _handler.TryHandleAsync(context, exception, default);

        context.Response.StatusCode.Should().Be(status);
        context.Response.Body.Position = 0;
        var json = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
        json.GetProperty("status").GetInt32().Should().Be(status);
        json.GetProperty("errorCode").GetString().Should().Be(errorCode);
    }
}
