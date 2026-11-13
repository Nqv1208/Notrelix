---
document_id: SPEC-CHAT-MEDIA-CALLS
document_type: feature-spec
status: draft
owner: chat
applies_to:
  - chat
  - chat-media
  - chat-calls
  - chat-realtime
evidence:
  - docs/decisions/SYS-ADR-002-chat-rich-media-and-calls.md
  - docs/decisions/SYS-ADR-001-chat-bounded-context.md
  - docs/product/chat.md
  - docs/workstreams/use-cases/chat.md
  - docs/workstreams/teams/chat.md
  - docs/workstreams/specs/chat-foundation.md
  - backend/src/Notrelix.Domain/Chat/Messages/MessageKind.cs
review_on:
  - implementation-complete
  - behavior-changed
  - chat-realtime-change
  - call-infrastructure-change
---

# Chat rich media and calls

## 1. Summary

### Problem

Chat today sends one attachment at a time with no preview size, no gallery, no voice and no call. People leave the product to send a screenshot batch, record a voice note or start a call, which splits context and access control again.

### Outcome

Images behave like a modern messenger (gallery, progress, viewer, no layout jump), voice messages and stickers exist, and one-to-one and then group voice and video calls run inside a conversation with the same access model as the messages.

### Semantic owner

- Bounded context `Chat` (SYS-ADR-001, SYS-ADR-002). Collaboration owns attachment storage and policy; Platform owns the realtime gateway and the media infrastructure.

## 2. Non-goals

Recording and transcription of calls, stage or broadcast channels, calls across Workspaces, end-to-end encryption, a phone gateway, virtual backgrounds, live captions.

## 3. Ubiquitous language

- **Gallery:** the ordered attachments of one message shown as a grid.
- **Voice message:** a message with one audio attachment, a duration and a waveform.
- **Sticker:** a Notrelix-owned image from a pack; not an attachment.
- **Call:** a time-bounded real-time audio or video session attached to a space (channel, direct or group conversation).
- **Participant:** a person who joined the call.
- **Media plane:** the SFU and TURN service that carries audio and video. **Signalling:** call state frames on the gateway.

## 4. Domain model (Chat, schema `chat`)

```text
MessageAttachmentMedia (value object on the attachment view, not a table of Chat)
  width, height, durationMs, waveform[<=64], thumbnailUrl      -- owned by Collaboration, read through IChatMessageAttachments

Sticker             id, pack_id, label, asset_key, sort_order, is_active
StickerPack         id, workspace_id|null (null = global), name, is_active
Message             + kind: user | system | call            -- call = system message written when a call ends
Message             + sticker_id nullable                    -- sticker message has no text and no attachment

Call                id, workspace_id, space_id, initiator_id, kind (voice|video), state,
                    end_reason, started_at, answered_at, ended_at, room_key, version
CallParticipant     call_id, person_id, joined_at, left_at, audio_on, video_on, sharing_screen
```

### State model

```text
Call: Ringing -> Active -> Ended
      Ringing -> Ended(reason: Declined | Missed | Cancelled | Busy | Failed)
      Active  -> Ended(reason: Completed | Failed)
Participant: Invited -> Joined -> Left
```

Transitions are commands with an expected `version`; a transition from `Ended` is rejected with a stable code.

## 5. Invariants

1. At most one `Ringing` or `Active` call per space.
2. A person is in at most one `Active` call at a time (a second invite answers `Busy`).
3. Only members of the space who are not blocked by the other party can be invited; a Guest or a read-only member cannot initiate.
4. A direct call has exactly two participants; a group call is capped by `ChatLimits.CallMaxParticipants` (proposed 25 voice, 12 video).
5. `Ringing` ends as `Missed` after 45 seconds without an answer.
6. Every terminal state writes exactly one `call` system message in the space with `(call_id)` unique per space, so a retry never writes two.
7. A voice message is at most `ChatLimits.VoiceMaxSeconds` (proposed 300) and one audio attachment.
8. A message has either text with attachments, a sticker, or a voice attachment; never a sticker plus attachments.
9. The server never returns a media token to a person who is not a participant of an `Active` or `Ringing` call.

## 6. Operations (REST, idempotent, Workspace from the path)

| Operation             | Method and path (under `/api/v1/workspaces/{workspaceId}/chat`) | Notes                                                                                                                     |
| --------------------- | --------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| Start call            | `POST /spaces/{spaceId}/calls`                                  | body `kind`; returns the call and a join token for the initiator; `Idempotency-Key`.                                      |
| Get active call       | `GET /spaces/{spaceId}/calls/active`                            | for the banner and re-join.                                                                                               |
| Answer                | `POST /calls/{callId}/answer`                                   | returns a join token.                                                                                                     |
| Decline, cancel, end  | `POST /calls/{callId}/decline` `cancel` `end`                   | expected version.                                                                                                         |
| Join or leave (group) | `POST /calls/{callId}/join` `leave`                             |                                                                                                                           |
| Media state           | `PUT /calls/{callId}/participants/me`                           | `audioOn`, `videoOn`, `sharingScreen`.                                                                                    |
| List call history     | `GET /spaces/{spaceId}/calls`                                   | cursor paged.                                                                                                             |
| Sticker packs         | `GET /stickers`                                                 | cached; global and workspace packs.                                                                                       |
| Send sticker          | `POST /spaces/{spaceId}/messages` with `stickerId`              | same send path, same idempotency.                                                                                         |
| Voice message         | existing attachment upload with `kind=audio`, then send         | waveform and duration are computed by the client and verified by the server.                                              |
| Message link          | `GET /spaces/{spaceId}/messages/{messageId}/link`               | returns the in-app route; permission checked.                                                                             |
| Mark unread from here | `PUT /spaces/{spaceId}/read-state/unread-from`                  | moves the boundary back to before a message; the only case where a boundary decreases, bounded to the caller's own state. |

Invariant 5 of SPEC-CHAT-FOUNDATION (a read boundary only increases) is amended by the last operation: the boundary may move back by an explicit user command and never forward through it.

## 7. Realtime (protocol 1.2, additive)

- New server-to-client frame `chat.call.v1` on the Channel scope: `{ callId, state, kind, initiatorId, participants[], version }`. Size stays under 4096 bytes; large rosters send ids only.
- Ring delivery goes to a per-person scope so a person who has no channel open still rings (Chat resolves membership, the gateway authorizes).
- No client-to-server frame is added.
- Media travels over WebRTC to the SFU, never through the gateway.

## 8. Infrastructure

- **SFU:** LiveKit (owner decision pending, SYS-ADR-002 decision 7) as a container next to `redis` and `rabbitmq` in the compose files (`docker-compose.yml`, `.dev`, `.staging`, `.prod`), plus TURN (built in or coturn), UDP port range, TLS.
- **Configuration:** `Chat:Calls:Enabled`, `Chat:Calls:SfuUrl`, `Chat:Calls:ApiKey` and `ApiSecret` (secrets from the secret store, never in compose files), `Chat:Calls:TokenTtlSeconds` (60), `Chat:Calls:RingSeconds` (45).
- **Ports:** `IChatCallMediaGateway` (create room, issue token, end room, receive SFU webhooks) with a fake for tests; `IChatGifProvider` behind a workspace setting.
- **Webhooks:** SFU participant-joined and room-finished events reconcile call state; the endpoint verifies the signature and is idempotent.
- **Jobs:** a hosted service ends calls stuck `Ringing` after the ring limit and calls with no participants after 60 seconds.
- **Observability:** metrics for calls started, answered, missed, ended by reason, join latency and token errors; a log line per transition with `callId` and `spaceId`, no media content.
- **Capacity:** the call count and bandwidth budget go to the performance budget of the team spec before Phase 5 leaves INTEGRATING.

## 9. Data and migration

Expand-only migrations: tables `chat.calls`, `chat.call_participants`, `chat.sticker_packs`, `chat.stickers`; nullable column `chat.messages.sticker_id`; new message kind value `call` stored as text. RLS through `ops.apply_scoped_business_policies('chat', ...)` for the new tables; stickers of a global pack have a null Workspace and read-only policy. No contract step.

## 10. Failure semantics

- SFU unreachable: `start` returns a stable code `chat.calls.unavailable`, no call row stays `Ringing`.
- Token expired before join: the client asks again with the same call id while the call is joinable.
- Webhook lost: the reconciliation job closes calls by time.
- Browser without microphone or camera permission: the call can be joined listen-only; the UI explains how to allow access.
- Image still uploading when the message is sent: the send waits for the attachment ids; cancel removes the pending tile.

## 11. Security and privacy

Join tokens are scoped to one room and one person, expire in 60 seconds, and are never logged. The server checks membership and block rules on every operation. Stickers and GIFs are fetched by the server. Audio, video and screen content are not stored by Notrelix. Audit facts: `chat.call.started`, `chat.call.ended` (no media), sticker pack changes.

## 12. Accessibility

Every call control is a labelled button; the incoming call is an alert dialog with focus on Answer; M and V toggle audio and video only while the call window has focus; Esc never ends a call; the gallery tiles and the viewer have text alternatives; voice messages expose play, pause, seek and a readable duration; reduced motion stops GIF animation.

## 13. Acceptance scenarios

1. Given two members in a direct conversation, when A starts a video call and B answers, then both see each other, the space shows an active-call banner, and ending writes one `call` message with the duration.
2. Given B does not answer in 45 seconds, then the call ends as `Missed`, A sees "no answer", B gets one missed-call notification and one system message appears.
3. Given B is in another call, when A calls, then A is told B is busy and no ring is delivered.
4. Given a Guest, when they try to start a call, then it is rejected and no room exists.
5. Given five images selected, when sent, then one message shows four tiles and `+1`, and the viewer pages through all five.
6. Given a 6-minute recording, then recording stops at 5 minutes and the message sends with 5 minutes.
7. Given a sticker is sent twice with the same idempotency key, then one message exists.

## 14. Acceptance proof matrix

| Scenario | Domain                        | Application               | API                      | Integration (RLS, SFU fake)          | Frontend                           | Real e2e                  |
| -------- | ----------------------------- | ------------------------- | ------------------------ | ------------------------------------ | ---------------------------------- | ------------------------- |
| 1 to 4   | call state machine tests      | handler tests per command | OpenAPI and authz tests  | cross-tenant and webhook idempotency | hook and component tests           | by owner, after HARDENING |
| 5, 6     | attachment media value object | send with attachments     | upload and send contract | thumbnail job                        | gallery and viewer component tests | by owner                  |
| 7        | sticker invariant             | send sticker              | idempotency              | duplicate key                        | optimistic send                    | by owner                  |

## 15. Delivery order

1. Phase 4a: attachment media metadata and thumbnails (Collaboration extension), gallery, upload progress, viewer.
2. Phase 4b: voice messages, message link, mark unread, create task from a message.
3. Phase 4c: stickers, then GIF provider behind a setting.
4. Phase 5: `Call` domain, REST, signalling frames, SFU port with fake, hosted jobs, infrastructure manifests, one-to-one UI.
5. Phase 6: group calls, screen share, participant list, rate limits and load test.

## 16. Blocking open questions (owner)

1. SFU hosting: self-hosted LiveKit, managed provider, or mediasoup.
2. GIF provider or own stickers only.
3. Voice message limit (proposed 5 minutes) and group call caps.
4. Retention of call logs and of stickers uploaded by a Workspace.

## 17. Final feature acceptance

The owner accepts a phase after the HARDENING evidence is in the use-case catalog and the owner has tried the feature in a real browser with two accounts. Agents do not run real end-to-end tests and do not set DONE.
