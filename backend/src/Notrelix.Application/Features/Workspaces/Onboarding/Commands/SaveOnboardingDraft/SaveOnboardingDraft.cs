using System.Text.Json;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Workspaces.Onboarding;

namespace Notrelix.Application.Features.Workspaces.Onboarding.Commands.SaveOnboardingDraft;

/// <summary>
/// Saves the caller's unfinished onboarding, replacing any earlier draft and restarting its 30 days. One
/// draft per user, so the request is naturally idempotent: the same body leaves the same draft. The subject
/// is always the principal. An expired draft is replaced like an absent one.
/// </summary>
/// <param name="ForWorkspaceId">The workspace the draft is about once it exists; an opaque reference, never an authorization scope.</param>
/// <param name="Data">The client's step payload; must be a JSON object of at most 64 KB.</param>
public record SaveOnboardingDraftCommand(string Step, Guid? ForWorkspaceId, JsonElement? Data)
    : ICommand<Result>, IAuthenticatedRequest, IWriteRequest, IGlobalRequest;

public class SaveOnboardingDraftCommandHandler : IRequestHandler<SaveOnboardingDraftCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public SaveOnboardingDraftCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider clock)
    {
        _context = context;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result> Handle(SaveOnboardingDraftCommand request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var now = _clock.UtcNow;
        var data = request.Data is { ValueKind: not (JsonValueKind.Undefined or JsonValueKind.Null) } value ? value.GetRawText() : null;

        var existing = await _context.OnboardingDrafts.FirstOrDefaultAsync(d => d.UserId == userId, ct);
        if (existing is null)
            _context.OnboardingDrafts.Add(OnboardingDraft.Create(userId, request.Step, request.ForWorkspaceId, data, now));
        else
            existing.Replace(request.Step, request.ForWorkspaceId, data, now);

        return Result.Success();
    }
}
