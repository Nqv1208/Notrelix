namespace Notrelix.Infrastructure.Captcha;

/// <summary>
/// The captcha provider of anonymous form submissions (the "Captcha" configuration section). The provider is
/// Cloudflare Turnstile; the secret key is a secret and comes from the environment, never from source. With no
/// secret the verifier refuses every token (fail closed), so an unconfigured host accepts no anonymous input.
/// </summary>
public sealed class CaptchaOptions
{
    public const string SectionName = "Captcha";

    /// <summary>The public site key the page renders its widget with.</summary>
    public string? SiteKey { get; set; }

    /// <summary>The server-side secret used to verify a token; never sent to a browser.</summary>
    public string? SecretKey { get; set; }

    public string VerifyUrl { get; set; } = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
}
