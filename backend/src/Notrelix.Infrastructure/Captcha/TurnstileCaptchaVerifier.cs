using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Forms.Services;

namespace Notrelix.Infrastructure.Captcha;

/// <summary>
/// Verifies a Cloudflare Turnstile token with the provider's siteverify endpoint. Fails closed: no secret, a
/// blank token, a provider error, a timeout or an unreadable answer all refuse the submission.
/// </summary>
public sealed class TurnstileCaptchaVerifier : ICaptchaVerifier
{
    private readonly HttpClient _http;
    private readonly CaptchaOptions _options;
    private readonly ILogger<TurnstileCaptchaVerifier> _logger;

    public TurnstileCaptchaVerifier(HttpClient http, IOptions<CaptchaOptions> options, ILogger<TurnstileCaptchaVerifier> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public string? SiteKey => string.IsNullOrWhiteSpace(_options.SecretKey) ? null : _options.SiteKey;

    public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 4096) return false;
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            _logger.LogError("Captcha secret is not configured; anonymous form submissions are refused");
            return false;
        }

        try
        {
            var form = new List<KeyValuePair<string, string>>
            {
                new("secret", _options.SecretKey),
                new("response", token),
            };
            if (!string.IsNullOrWhiteSpace(remoteIp)) form.Add(new("remoteip", remoteIp));

            using var response = await _http.PostAsync(_options.VerifyUrl, new FormUrlEncodedContent(form), cancellationToken);
            if (!response.IsSuccessStatusCode) return false;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            return doc.RootElement.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Captcha provider could not be reached or answered unreadably; the submission is refused");
            return false;
        }
    }
}
