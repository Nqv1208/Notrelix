using Notrelix.Application.Features.Collaboration.Attachments.Commands.CreateBoardItemAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.CreatePageAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Services;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>
/// UC-COL-B07 and UC-COL-B09: a link attachment accepts only an absolute http or https address. Every other scheme,
/// and every value that does not parse, is refused by the validator with a validation error (400).
/// </summary>
public sealed class AttachmentLinkUrlTests
{
    [Theory]
    [InlineData("https://example.com/doc")]
    [InlineData("http://x.test/a?b=c")]
    [InlineData("HTTPS://EXAMPLE.COM/Upper")]
    public void AnAbsoluteHttpOrHttpsAddress_IsAllowed(string url) =>
        AttachmentLinkUrl.IsAllowed(url).Should().BeTrue();

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("JAVASCRIPT:alert(1)")]
    [InlineData("data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("mailto:someone@example.com")]
    [InlineData("ftp://example.com/file")]
    [InlineData("//example.com/relative-scheme-less")]
    [InlineData("/relative/path")]
    [InlineData("example.com")]
    [InlineData("https:")]
    [InlineData("https://")]
    [InlineData("not a url at all")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AnyOtherSchemeOrAnUnparseableValue_IsRefused(string? url) =>
        AttachmentLinkUrl.IsAllowed(url).Should().BeFalse();

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("not a url")]
    public void BoardItemLink_WithARefusedUrl_FailsValidationOnTheServer(string url)
    {
        var result = new CreateBoardItemAttachmentCommandValidator()
            .Validate(new CreateBoardItemAttachmentCommand(Guid.CreateVersion7(), "doc", url, null, null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateBoardItemAttachmentCommand.Url));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("not a url")]
    public void PageLink_WithARefusedUrl_FailsValidationOnTheServer(string url)
    {
        var result = new CreatePageAttachmentCommandValidator()
            .Validate(new CreatePageAttachmentCommand(Guid.CreateVersion7(), "site", url, null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePageAttachmentCommand.Url));
    }

    [Fact]
    public void BoardItemLink_WithAnHttpsUrl_PassesValidation() =>
        new CreateBoardItemAttachmentCommandValidator()
            .Validate(new CreateBoardItemAttachmentCommand(Guid.CreateVersion7(), "doc", "https://example.com/doc", null, null, null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void PageLink_WithAnHttpUrl_PassesValidation() =>
        new CreatePageAttachmentCommandValidator()
            .Validate(new CreatePageAttachmentCommand(Guid.CreateVersion7(), "site", "http://example.com/x", null, null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void PageLink_OverTheLengthLimit_FailsValidation() =>
        new CreatePageAttachmentCommandValidator()
            .Validate(new CreatePageAttachmentCommand(Guid.CreateVersion7(), "site", "https://example.com/" + new string('a', AttachmentLinkUrl.MaxLength), null, null))
            .IsValid.Should().BeFalse();
}
