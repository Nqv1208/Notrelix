using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Onboarding.Commands.ClearOnboardingDraft;

/// <summary>Clears the caller's own unfinished onboarding. Clearing an absent draft succeeds (a retried request is safe).</summary>
public record ClearOnboardingDraftCommand : ICommand<Result>, IAuthenticatedRequest, IWriteRequest, IGlobalRequest;

public class ClearOnboardingDraftCommandHandler : IRequestHandler<ClearOnboardingDraftCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;

    public ClearOnboardingDraftCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext)
    {
        _context = context;
        _requestContext = requestContext;
    }

    public async Task<Result> Handle(ClearOnboardingDraftCommand request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var existing = await _context.OnboardingDrafts.FirstOrDefaultAsync(d => d.UserId == userId, ct);
        if (existing is not null)
            _context.OnboardingDrafts.Remove(existing);
        return Result.Success();
    }
}
