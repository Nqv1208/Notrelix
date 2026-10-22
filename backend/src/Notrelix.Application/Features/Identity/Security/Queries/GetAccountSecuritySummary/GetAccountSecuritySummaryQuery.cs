using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.Services;
using Notrelix.Application.Features.Identity.Security.DTOs;

namespace Notrelix.Application.Features.Identity.Security.Queries.GetAccountSecuritySummary;

/// <summary>Composes the caller's own security facts. The subject is always the principal.</summary>
public sealed record GetAccountSecuritySummaryQuery
    : IQuery<Result<AccountSecuritySummaryDto>>, IReadRequest,
      IGlobalRequest,
      IAuthenticatedRequest;

public sealed class GetAccountSecuritySummaryQueryHandler
    : IRequestHandler<GetAccountSecuritySummaryQuery, Result<AccountSecuritySummaryDto>>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAccountSecuritySummaryQueryHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AccountSecuritySummaryDto>> Handle(
        GetAccountSecuritySummaryQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var now = _dateTimeProvider.UtcNow;

        var hasPassword = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => (bool?)u.HasPasswordCredential)
            .FirstOrDefaultAsync(cancellationToken);

        if (hasPassword is null)
        {
            return Result<AccountSecuritySummaryDto>.Failure(new ApplicationError(
                "identity.auth.user-not-found", "User not found.", ApplicationErrorType.NotFound));
        }

        var passwordChangedAt = await _context.UserSecuritySettings
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => s.PasswordChangedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var activeSessions = await _context.Sessions
            .AsNoTracking()
            .CountAsync(
                s => s.UserId == userId && s.Status == SessionStatus.Active && s.ExpiresAt > now,
                cancellationToken);

        var providers = (await _context.OAuthAccounts
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .Select(a => a.Provider)
                .ToListAsync(cancellationToken))
            .Select(p => p.ToString())
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();

        var mfa = await MfaStatusProjection.LoadAsync(_context, userId, cancellationToken);

        return Result<AccountSecuritySummaryDto>.Success(new AccountSecuritySummaryDto(
            now, hasPassword.Value, passwordChangedAt, mfa, activeSessions, providers));
    }
}
