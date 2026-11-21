using Notrelix.Application.Features.WorkManagement.Forms.Services;

namespace Notrelix.Testing.Application.Fakes;

/// <summary>
/// Test double for the captcha provider: only the token <see cref="ValidToken"/> passes, every other token
/// (and a blank one) is refused. It is registered by tests only; the shipped host verifies with the provider.
/// </summary>
public sealed class FakeCaptchaVerifier : ICaptchaVerifier
{
    public const string ValidToken = "captcha-test-pass";

    public string? SiteKey => "test-site-key";

    public List<(string? Token, string? RemoteIp)> Calls { get; } = [];

    public Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken)
    {
        Calls.Add((token, remoteIp));
        return Task.FromResult(token == ValidToken);
    }
}
