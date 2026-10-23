using System.Text.Json;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Onboarding.Queries.GetOnboardingDraft;

/// <param name="Step">The client's step key.</param>
/// <param name="WorkspaceId">The workspace the draft is about once it exists.</param>
/// <param name="Data">The client's step payload (a JSON object).</param>
/// <param name="ExpiresAt">When the draft is dropped (30 days after the last save).</param>
public record OnboardingDraftDto(string Step, Guid? WorkspaceId, JsonElement Data, DateTimeOffset ExpiresAt);

/// <param name="Draft">Null when the caller has no unexpired draft.</param>
public record OnboardingDraftResponseDto(OnboardingDraftDto? Draft);

/// <summary>
/// The caller's own unfinished onboarding. The subject is always the principal (no user id input), so one
/// user can never read another's draft; RLS repeats the rule. An expired draft reads as absent.
/// </summary>
public record GetOnboardingDraftQuery : IQuery<Result<OnboardingDraftResponseDto>>, IAuthenticatedRequest, IReadRequest, IGlobalRequest;

public class GetOnboardingDraftQueryHandler : IRequestHandler<GetOnboardingDraftQuery, Result<OnboardingDraftResponseDto>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public GetOnboardingDraftQueryHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider clock)
    {
        _context = context;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result<OnboardingDraftResponseDto>> Handle(GetOnboardingDraftQuery request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var now = _clock.UtcNow;
        var draft = await _context.OnboardingDrafts
            .AsNoTracking()
            .Where(d => d.UserId == userId && d.ExpiresAt > now)
            .FirstOrDefaultAsync(ct);

        return Result<OnboardingDraftResponseDto>.Success(new OnboardingDraftResponseDto(
            draft is null ? null : new OnboardingDraftDto(draft.Step, draft.WorkspaceId, JsonDocument.Parse(draft.Data).RootElement.Clone(), draft.ExpiresAt)));
    }
}
