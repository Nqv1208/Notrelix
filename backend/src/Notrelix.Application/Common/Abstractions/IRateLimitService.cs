namespace Notrelix.Application.Common.Abstractions;

public interface IRateLimitService
{
    Task<bool> IsRateLimitedAsync(string action, string identifier, int maxAttempts, TimeSpan window);
    Task<int> GetRemainingAsync(string action, string identifier, int maxAttempts, TimeSpan window);
}
