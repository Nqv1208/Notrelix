using Notrelix.Domain.Identity.Tokens;

namespace Notrelix.Application.Features.Identity.Abstractions;

public interface IIdentityDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserSession> Sessions { get; }
    DbSet<OAuthAccount> OAuthAccounts { get; }
    DbSet<EmailVerificationToken> EmailVerificationTokens { get; }
}
