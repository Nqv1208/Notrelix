using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Application.Features.Identity.Mfa.Services;

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
        var dto = await MfaStatusProjection.LoadAsync(_context, _currentUser.UserId, cancellationToken);

        return Result<MfaStatusDto>.Success(dto);
    }
}
