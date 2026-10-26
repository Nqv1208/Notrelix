namespace Notrelix.Application.Common.Tenancy;

/// <summary>
/// Read-only port to the phone numbers people chose to share with the members of their
/// Workspaces (Identity owns the phone and the <c>phoneVisibleToWorkspaceMembers</c> flag).
/// It answers only the opt-in half of the visibility rule: a user who kept the default
/// (self-only) is never returned. The caller owns the other half, that the reader and the
/// subject share a Workspace and the reader is not a guest, and must not use this port in
/// guest, share-link or public views. The Workspace is part of the question so that an implementation under
/// a constrained database principal can enforce the same rule below the Application (reader and subject are
/// active non-guest members of that Workspace).
/// </summary>
public interface IMemberPhoneLookup
{
    /// <summary>Phones, in normalized E.164 form, of the given users who opted in; absent users are omitted.</summary>
    Task<IReadOnlyDictionary<Guid, string>> FindSharedPhonesAsync(Guid workspaceId, IReadOnlyCollection<Guid> userIds, CancellationToken ct);
}
