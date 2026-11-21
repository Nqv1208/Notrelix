namespace Notrelix.Application.Features.WorkManagement.Forms.Services;

/// <summary>
/// Verifies, server side, the captcha token an anonymous visitor presents with a public form submission. The
/// provider and its keys come from configuration; tests replace the verifier with a double. An unconfigured
/// or unreachable provider refuses (fail closed): a form never accepts anonymous input it could not check.
/// </summary>
public interface ICaptchaVerifier
{
    /// <summary>The public site key the page renders its widget with; null when no provider is configured.</summary>
    string? SiteKey { get; }

    /// <summary>True only when the provider confirms the token is valid and unused.</summary>
    Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken);
}
