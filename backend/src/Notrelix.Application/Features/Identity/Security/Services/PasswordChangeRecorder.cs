using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Domain.Identity.Security;

namespace Notrelix.Application.Features.Identity.Security.Services;

/// <summary>
/// Stamps the credential-changed time on the user's <see cref="UserSecuritySettings"/>
/// (created on first use) so the account security summary can show when the password last
/// changed. Used by every flow that replaces the password.
/// </summary>
internal static class PasswordChangeRecorder
{
    public static async Task RecordAsync(
        IIdentityDbContext context, Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var settings = await context.UserSecuritySettings
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (settings is null)
        {
            settings = UserSecuritySettings.Create(userId, now);
            context.UserSecuritySettings.Add(settings);
        }

        settings.MarkPasswordChanged(now);
    }
}
