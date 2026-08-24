using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Domain.Identity.Mfa;

namespace Notrelix.Application.Features.Identity.Mfa.Queries.GetMfaStatus;

public sealed record GetMfaStatusQuery
    : IQuery<Result<MfaStatusDto>>, IReadRequest,
      IGlobalRequest,
      IAuthenticatedRequest;

public sealed class GetMfaStatusQueryHandler
    : IRequestHandler<GetMfaStatusQuery, Result<MfaStatusDto>>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;

    public GetMfaStatusQueryHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<MfaStatusDto>> Handle(
        GetMfaStatusQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var methods = await _context.UserMfaMethods
            .Where(m => m.UserId == userId)
            .ToListAsync(cancellationToken);

        var activeMethod = methods.FirstOrDefault(m => m.Status == MfaMethodStatus.Active);
        var isEnabled = activeMethod is not null;

        var primary = methods.FirstOrDefault(m => m.IsPrimary && m.Status == MfaMethodStatus.Active);
        var primaryMethod = primary?.Type.ToString() ?? activeMethod?.Type.ToString();

        var activeBatch = await _context.MfaRecoveryBatches
            .Where(b => b.UserId == userId && b.InvalidatedAt == null)
            .OrderByDescending(b => b.CreatedAt)
            .Include(b => b.Codes)
            .FirstOrDefaultAsync(cancellationToken);

        var remaining = activeBatch?.Codes.Count(c => c.ConsumedAt == null) ?? 0;

        var dto = new MfaStatusDto(
            IsEnabled: isEnabled,
            PrimaryMethod: primaryMethod,
            HasRecoveryCodes: remaining > 0,
            RecoveryCodesRemaining: remaining);

        return Result<MfaStatusDto>.Success(dto);
    }
}
