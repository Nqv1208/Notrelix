using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.Profiles;
using Notrelix.Domain.Identity.Security;
using Notrelix.Domain.Identity.Sso;
using Notrelix.Domain.Identity.Tokens;

namespace Notrelix.Application.Features.Identity.Abstractions;

public interface IIdentityDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserSession> Sessions { get; }
    DbSet<OAuthAccount> OAuthAccounts { get; }
    DbSet<SsoIdentity> SsoIdentities { get; }
    DbSet<EmailVerificationToken> EmailVerificationTokens { get; }
    DbSet<UserMfaMethod> UserMfaMethods { get; }
    DbSet<UserSecuritySettings> UserSecuritySettings { get; }
    DbSet<MfaRecoveryBatch> MfaRecoveryBatches { get; }
    DbSet<ApiToken> ApiTokens { get; }
    DbSet<UserProfile> UserProfiles { get; }

    /// <summary>Writes what the request has changed so far; the single sign-on callback needs a new user stored before it joins the person to an Account.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
