using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.Security;
using Notrelix.Domain.Identity.Tokens;

namespace Notrelix.Application.Features.Identity.Abstractions;

public interface IIdentityDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserSession> Sessions { get; }
    DbSet<OAuthAccount> OAuthAccounts { get; }
    DbSet<EmailVerificationToken> EmailVerificationTokens { get; }
    DbSet<UserMfaMethod> UserMfaMethods { get; }
    DbSet<UserSecuritySettings> UserSecuritySettings { get; }
    DbSet<MfaRecoveryBatch> MfaRecoveryBatches { get; }
    DbSet<ApiToken> ApiTokens { get; }
}
