namespace Notrelix.Application.Features.Identity.Security.Abstractions;

/// <summary>The security changes a person is told about by mail.</summary>
public enum SecurityNoticeKind
{
    PasswordChanged,
    PasswordReset,
    MfaEnabled,
    MfaDisabled,
    ProviderLinked,
    ProviderUnlinked,
    SessionRevoked,
    OtherSessionsRevoked,
    ApiTokenIssued
}

/// <summary>
/// Tells a person by mail that one of their security settings changed: what, when, from which device, and a
/// link to the app; never a secret. The mail is only staged in the outbox, so it commits (or rolls back) with
/// the change it describes and is sent after the commit. The same event queues the same key, so a retry sends at
/// most one mail, and a failure to stage it never fails the change.
/// </summary>
public interface ISecurityNotice
{
    /// <param name="subjectId">The thing the event is about (a token id, a session id), so two events of one kind in the same instant stay two mails.</param>
    Task QueueAsync(
        Guid userId,
        SecurityNoticeKind kind,
        DateTimeOffset occurredAt,
        Guid? subjectId,
        CancellationToken cancellationToken);
}

/// <summary>Where the mail sends the person: the app, from which they open their account security.</summary>
public interface ISecurityPageLink
{
    string Build();
}
