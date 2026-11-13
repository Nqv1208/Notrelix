---
document_id: SYS-ADR-002
document_type: architecture-decision
status: Proposed
owner: architecture
applies_to:
  - chat
  - collaboration
  - realtime-gateway
  - platform
  - backend
  - frontend
evidence:
  - docs/decisions/SYS-ADR-001-chat-bounded-context.md
  - docs/product/chat.md
  - docs/workstreams/use-cases/chat.md
  - docs/workstreams/teams/chat.md
  - docs/workstreams/specs/chat-media-and-calls.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - backend/src/Notrelix.API/Realtime/RealtimeProtocol.cs
review_on:
  - decision-superseded
  - chat-model-change
  - realtime-protocol-change
  - media-provider-change
  - call-infrastructure-change
---

# SYS-ADR-002: Chat rich media, voice messages and calls

## ID

`SYS-ADR-002`

## Status

Proposed on 2026-11-13. The product owner asked for the documentation, the domain and the infrastructure to be opened for these features. Three choices below are owner decisions (marked **Owner decision**) and block the slices that depend on them; everything else can start once this record is accepted.

## Date

`2026-11-13`

## Owners

- `architecture`
- `chat` (logical owner)

Semantic co-owners: Collaboration (attachment storage and policy), Platform (realtime gateway, media infrastructure), Governance (permissions, Audit), Notifications handoff (missed-call intents).

## Context

SYS-ADR-001 admitted Chat as the twelfth bounded context and listed voice, video, screen sharing and calls as not covered. The product owner now asks for three additions:

1. Rich images in conversation: several images as one gallery, upload progress with cancel, a full-screen viewer, and stable layout while an image loads.
2. Voice messages, GIFs and stickers.
3. Voice and video calls, first one-to-one, then groups and screen sharing.

What exists today: the shared attachment flow (target kind `chat.message`, 100 MB per file, paste and drop in the composer, realtime `chat.message.attachment.changed`), the channel realtime scope with typing and presence, and the Chat notification handoff. What does not exist: image dimensions and thumbnails in the attachment view, audio metadata, a gallery notion, any call state, and any media plane. The gateway carries signalling-sized frames only (at most 4096 bytes, 20 frames per second, 50 subscriptions per connection) and must not carry audio or video.

## Decision

1. **Same context, no new bounded context.** Rich media and calls extend Chat. A call is a conversation event, so it shares the Chat access model (channel or conversation membership). The admission test of SYS-ADR-001 still holds.
2. **Rich media reuses the shared attachment flow.** Collaboration keeps storage, scanning and size policy. The attachment view gains additive fields: `width`, `height`, `durationMs`, `waveform` (up to 64 peaks, 0 to 255) and `thumbnailUrl`. The server fills dimensions and thumbnails after upload; until then the client reserves the box from the local file. The Chat port `IChatMessageAttachments` is extended, not replaced.
3. **A gallery is an ordering of attachments on one message.** No new aggregate. The grid shows up to four tiles and a `+N` tile; the viewer pages through all attachments of the message and, optionally, of the space.
4. **A voice message is an audio attachment with metadata.** Opus in WebM or MP4, at most 5 minutes (**Owner decision**, proposed 5), recorded in the browser. It is a normal message with one attachment of kind audio; no separate table.
5. **GIFs and stickers go through a Chat-owned port.** `IChatGifProvider` is called by the server so the browser never contacts a third party, which keeps CSP, privacy and the provider key server-side. **Owner decision:** provider (Tenor, Giphy) or Notrelix-owned sticker packs only. Proposed: ship own sticker packs first, add a GIF provider behind a workspace setting.
6. **Calls are a Chat aggregate.** `Call` (conversation or channel, initiator, kind voice or video, state, timestamps, participants) with states `Ringing`, `Active`, `Ended`, and terminal reasons `Declined`, `Missed`, `Cancelled`, `Busy`, `Failed`. Every terminal state writes one system message of kind `call` into the conversation (duration, participants) and, for `Missed`, a notification intent.
7. **Media plane is an SFU, signalling stays on the existing gateway.** Chat issues short-lived join tokens through a port `IChatCallMediaGateway`; audio and video flow between browsers and the SFU over WebRTC; the gateway carries only call state frames (`chat.call.v1`: ring, accept, decline, end, participant joined or left, media state). One-to-one calls also use the SFU so that there is one code path and a call can grow into a group call. **Owner decision:** self-hosted LiveKit (proposed), a managed provider, or mediasoup. TURN and STUN are required in every option.
8. **Realtime protocol 1.2 (additive).** One new server-to-client frame kind carries call state. No client-to-server frame is added: accepting, declining, ending and media state changes (mute, camera) go through REST and are echoed to the other participants as state frames. Review trigger `realtime-protocol-change` applies.
9. **Guards.** Only members of the space can call; a blocked person cannot be called; Guests and read-only members cannot start a call; one active call per person; ringing times out at 45 seconds. Calls are subject to the Chat rate limiter and the Audit facts of Chat. No recording and no transcription.
10. **Phasing.** Phase 4: rich images and voice messages, then GIFs and stickers. Phase 5: one-to-one voice and video calls. Phase 6: group calls and screen sharing.

## Alternatives considered

- **A. Peer-to-peer WebRTC for one-to-one calls.** Rejected: a second code path, no upgrade to group calls, and the same TURN dependency.
- **B. Build the media plane on the application gateway.** Rejected: the gateway frame limits and the .NET process are not a media server.
- **C. A managed communications provider.** Not rejected; it is an owner option in decision 7. It trades operational work for recurring cost and a data processor to disclose.
- **D. Third-party GIF embeds loaded by the browser.** Rejected: leaks viewer data, breaks CSP, and cannot be moderated.
- **E. Separate Media bounded context.** Rejected: media here has no rules of its own beyond the message that carries it.

## Consequences

Positive: one access model for messages and calls, no new context, reuse of attachment policy, a single media path.

Negative: a media server becomes a production dependency (capacity, TURN, observability); the attachment view gains fields every consumer must tolerate; a second realtime protocol change.

New obligations: infrastructure for the SFU and TURN in the deployment manifests, a call-state table and RLS policy, Chat Audit facts for calls, an ops runbook for media, a load test for concurrent calls, and design records for the new boards.

## Compatibility and migration

Additive only. New columns are nullable. Existing clients ignore the new attachment fields and the new frame kind. No existing endpoint, event or table changes meaning.

## Evidence

The blueprint is `docs/workstreams/specs/chat-media-and-calls.md`. The use cases are capabilities I and J in `docs/workstreams/use-cases/chat.md`. The design boards are in the Notrelix Chat canvas (boards "Mới: ảnh, lightbox, tin thoại, GIF" and "Mới: gọi thoại và video").

## Supersedes

None. It amends the "Not covered" scope of SYS-ADR-001 for voice, video, screen sharing and calls only.

## Superseded By

None.

## Notes

Open owner decisions: SFU hosting, GIF provider or own sticker packs, the voice message length limit, and the retention of call logs.
