using Microsoft.Extensions.Options;

namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Startup validation for <see cref="IdempotencyOptions"/> (spec 3.6).
/// The Infrastructure store owns every expiry calculation, so invalid option
/// values must fail configuration before any request can execute.
/// </summary>
public sealed class IdempotencyOptionsValidator : IValidateOptions<IdempotencyOptions>
{
    private static readonly TimeSpan MaxProcessingExpiry = TimeSpan.FromHours(1);
    private static readonly TimeSpan MaxResultExpiry = TimeSpan.FromDays(30);
    private const int MinResultBytes = 1024;
    private const int MaxResultBytes = 4 * 1024 * 1024;
    private static readonly TimeSpan MinRetryAfter = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxRetryAfter = TimeSpan.FromSeconds(30);

    public ValidateOptionsResult Validate(string? name, IdempotencyOptions options)
    {
        if (options.ProcessingExpiry <= TimeSpan.Zero || options.ProcessingExpiry > MaxProcessingExpiry)
        {
            return ValidateOptionsResult.Fail(
                $"Idempotency:ProcessingExpiry must be greater than zero and at most {MaxProcessingExpiry}. " +
                $"Current value: {options.ProcessingExpiry}.");
        }

        if (options.ResultExpiry <= options.ProcessingExpiry || options.ResultExpiry > MaxResultExpiry)
        {
            return ValidateOptionsResult.Fail(
                $"Idempotency:ResultExpiry must be greater than ProcessingExpiry ({options.ProcessingExpiry}) " +
                $"and at most {MaxResultExpiry}. Current value: {options.ResultExpiry}.");
        }

        if (options.MaxResultBytes < MinResultBytes || options.MaxResultBytes > MaxResultBytes)
        {
            return ValidateOptionsResult.Fail(
                $"Idempotency:MaxResultBytes must be between {MinResultBytes} and {MaxResultBytes} bytes. " +
                $"Current value: {options.MaxResultBytes}.");
        }

        if (options.IncompleteStateRetryAfter < MinRetryAfter || options.IncompleteStateRetryAfter > MaxRetryAfter)
        {
            return ValidateOptionsResult.Fail(
                $"Idempotency:IncompleteStateRetryAfter must be between {MinRetryAfter} and {MaxRetryAfter}. " +
                $"Current value: {options.IncompleteStateRetryAfter}.");
        }

        return ValidateOptionsResult.Success;
    }
}
