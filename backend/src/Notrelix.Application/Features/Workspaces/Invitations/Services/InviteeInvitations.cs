using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Services;

/// <summary>
/// One pending invitation of the signed-in person with what the list shows of it. <paramref name="InviterName"/>
/// is null when the producer has no display name to give.
/// </summary>
public sealed record InviteePendingInvitation(
    WorkspaceInvitation Invitation, string WorkspaceName, string WorkspaceSlug, string? InviterName);

/// <summary>
/// The invitations addressed to the signed-in person. Their requests are Global (the invited workspace is
/// not known until the invitation is read), so the tenant query filter would hide every row. The database
/// policy (RLS 025) is what limits the read to the pending invitations addressed to the person's own
/// confirmed address; the address test here repeats it, so a stale or foreign address finds nothing even
/// where no policy runs. The invitation row is all that policy opens: the workspace name and the inviter's
/// display name are read inside the invitation token scope of that one invitation, entered from the digest
/// of a row the person was allowed to read, exactly as accepting by link does after parsing the token.
/// </summary>
public sealed class InviteeInvitations
{
    /// <summary>The most invitations one person's list holds.</summary>
    public const int MaxPending = 50;

    private readonly IWorkspaceDbContext _context;
    private readonly InvitationTokenScope _tokenScope;
    private readonly IActorLookupService _actorLookup;

    public InviteeInvitations(
        IWorkspaceDbContext context,
        InvitationTokenScope tokenScope,
        IActorLookupService actorLookup)
    {
        _context = context;
        _tokenScope = tokenScope;
        _actorLookup = actorLookup;
    }

    /// <summary>
    /// Pending, unexpired invitations to an active workspace, newest first, at most <see cref="MaxPending"/>
    /// (the contract of the list: the newest ones; an older one is answered once the newer ones are).
    /// Each distinct workspace and inviter is read once per call, inside the token scope of the first
    /// invitation that needs it.
    /// </summary>
    public async Task<IReadOnlyList<InviteePendingInvitation>> ListPendingAsync(
        string email, DateTimeOffset now, CancellationToken ct)
    {
        var rows = await _context.WorkspaceInvitations.IgnoreQueryFilters().AsNoTracking()
            .Where(i => i.Email == email
                        && i.Status == WorkspaceInvitationStatus.Pending
                        && i.ExpiresAt > now)
            .OrderByDescending(i => i.CreatedAt)
            .Take(MaxPending)
            .ToListAsync(ct);

        var workspaces = new Dictionary<Guid, Workspace?>();
        var inviters = new Dictionary<Guid, string?>();
        var result = new List<InviteePendingInvitation>();
        foreach (var row in rows)
        {
            if (!workspaces.ContainsKey(row.WorkspaceId) || !inviters.ContainsKey(row.InvitedBy))
            {
                // The invitation's own token scope is what lets its workspace and inviter be read.
                if (!await _tokenScope.EnterAsync(new ParsedOneTimeToken(row.Token.Value, row.HashVersion), ct))
                    continue;

                if (!workspaces.ContainsKey(row.WorkspaceId))
                    workspaces[row.WorkspaceId] = await _context.Workspaces.IgnoreQueryFilters().AsNoTracking()
                        .FirstOrDefaultAsync(
                            w => w.Id == row.WorkspaceId && w.DeletedAt == null && w.Status == WorkspaceStatus.Active,
                            ct);

                if (!inviters.ContainsKey(row.InvitedBy))
                    inviters[row.InvitedBy] = (await _actorLookup.FindAsync(row.InvitedBy, ct))?.Name;
            }

            if (workspaces[row.WorkspaceId] is { } workspace)
                result.Add(new InviteePendingInvitation(row, workspace.Name, workspace.Slug, inviters[row.InvitedBy]));
        }

        return result;
    }

    /// <summary>
    /// Opens one pending invitation for the person to answer: reads it by id (null when it is not pending
    /// or not addressed to <paramref name="email"/>: the same answer as an unknown id), then enters the
    /// invitation token scope and binds the person as its invitee (null when the database refuses the bind).
    /// Returns the tracked invitation.
    /// </summary>
    public async Task<WorkspaceInvitation?> OpenAsync(
        Guid invitationId, Guid userId, string email, CancellationToken ct)
    {
        var seen = await _context.WorkspaceInvitations.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(
                i => i.Id == invitationId && i.Email == email && i.Status == WorkspaceInvitationStatus.Pending,
                ct);
        if (seen is null)
            return null;

        var invitation = await _tokenScope.FindAsync(
            new ParsedOneTimeToken(seen.Token.Value, seen.HashVersion), track: true, ct);
        if (invitation is null)
            return null;

        // The database must bind this person as the invitee; if it refuses, nothing was opened.
        return await _tokenScope.AdoptAsync(invitation, userId, ct) ? invitation : null;
    }
}
