namespace Notrelix.API.Contracts.Chat.Requests;

/// <summary>Starts a call in a space. Kind is "voice" or "video". Idempotency-Key makes a retry return the same call.</summary>
public record StartCallRequest(string Kind);

/// <summary>The call version the client read. A stale version is 412.</summary>
public record CallVersionRequest(long? ExpectedVersion);

/// <summary>The caller's own microphone, camera and screen share, with the call version the client read. A stale version is 412. ReplaceShare confirms that starting a share ends another person's share (without it a second share is 409).</summary>
public record UpdateCallMediaStateRequest(bool AudioOn, bool VideoOn, bool SharingScreen, long? ExpectedVersion, bool? ReplaceShare = null);
