---
document_id: SYS-ADR-001
document_type: architecture-decision
status: Accepted
owner: architecture
applies_to:
  - chat
  - collaboration
  - bounded-context-map
  - realtime-gateway
  - backend
  - frontend
evidence:
  - docs/architecture/bounded-context-map.md
  - docs/product/product-model.md
  - docs/product/collaboration.md
  - docs/product/chat.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/workstreams/use-cases/chat.md
  - docs/workstreams/teams/chat.md
  - docs/workstreams/specs/chat-foundation.md
  - backend/src/Notrelix.API/Realtime/RealtimeProtocol.cs
  - backend/src/Notrelix.Application/Common/Realtime/Gateway/RealtimeScope.cs
review_on:
  - decision-superseded
  - bounded-context-added
  - chat-model-change
  - realtime-protocol-change
  - collaboration-boundary-change
---

# SYS-ADR-001: Chat is a twelfth bounded context, separate from Collaboration

## ID

`SYS-ADR-001`

## Status

Accepted on 2026-11-10 by the product owner (go-ahead to implement after reviewing the admission test and the realtime protocol change in decision 4).

## Date

`2026-11-10`

## Owners

- `architecture`
- `chat` (logical owner, introduced by this decision)

Semantic co-owners for the boundaries named below: Collaboration (mentions, reactions, attachments, read state, notification delivery), Governance (permissions, Audit), Workspaces (membership), Platform (realtime delivery).

## Context

The product has eleven bounded contexts. Collaboration owns human interaction **around product resources**: comments on board items and pages, mentions, reactions, attachments, read state, notifications and ephemeral presence. Every Collaboration object has an explicit target (COL-001), and the product mockups state that Collaboration is "attached to work and documents, not split into a separate chat channel".

The product owner asked for a workspace chat in the style of Discord: channels grouped in categories, public and private, direct and group conversations, threads, mentions, reactions, attachments, unread state and presence. That is human interaction that **is not about a resource**. It has its own aggregates (channel, membership, message stream), its own ordering rule (a monotonic sequence per channel), its own access model (membership of the channel, not access to a target) and a write volume far above comments.

Adding it to Collaboration would break COL-001 (a message has no target) and would put two different access models and two lifecycles in one context. The repository requires an admission test before a context is added (`docs/architecture/bounded-context-map.md` §85, `docs/product/product-model.md` §113-115, `docs/governance/documentation-lifecycle.md` §19).

The realtime gateway today is notify-only: server frames carry no payload, there is no client data frame, and the scope kind is a closed enum (`Workspace`, `Board`). Chat needs a channel scope, message content in the event, and short-lived typing and presence signals.

## Decision

1. **Chat is a new bounded context.** Logical owner `chat`, rule prefix `CHT`, database schema `chat`, API prefix `/api/v1/workspaces/{workspaceId}/chat`, backend folders `Chat/` inside the existing Domain, Application, Infrastructure and API projects (no new `.csproj`, `backend/AGENTS.md` §6), frontend package `@notrelix/features-chat`.
2. **Ownership.**
   - Chat owns: channels, channel groups, memberships, conversations (direct and group), messages, threads, pins, per-channel read boundaries and notification levels, and the chat-specific access rules.
   - Chat reuses, and does not duplicate, Collaboration's mention identity rules (COL-007, COL-030), reaction uniqueness (COL-009), attachment policy (COL-010, COL-011) and notification delivery (COL-017, COL-018) through Application contracts.
   - Collaboration keeps comments on resources, the Inbox, notification preferences and the presence semantics of COL-012.
   - Governance keeps permissions and Audit. Workspaces keeps membership. Platform keeps delivery mechanics.
3. **Chat never references a message from Work Management or Documents by foreign key.** A channel may link a Board, Page or Team by `ResourceRef`; the link never mutates the target (COL-002).
4. **Realtime protocol v1.1 (additive).** New scope kind `Channel` with an authorization query owned by Chat. A `message` event frame carries a compact payload (REST stays the source of truth, and the client reconciles by `clientMessageId` and `sequence`). New client-to-server ephemeral frames `typing` and `presence`, rate limited inside the existing 20 frames per second and 4096 byte caps. Existing frames and the Board scope are unchanged.
5. **Writes go through REST.** Sending, editing and deleting a message are REST commands with an idempotency key. The socket never accepts a durable write.
6. **Order is server-assigned.** Each channel has a gap-free, monotonic `sequence`. Clients never order by their own clock.
7. **Phasing.** Phase 1 core (channels, messages, read state, core realtime), Phase 2 interaction (conversations, threads, mentions, reactions, attachments, typing, presence), Phase 3 advanced (channel permission overrides, moderation, retention, search, digest). Voice, video and stage channels are out of scope and need their own ADR.

## Admission test (bounded-context-map §85)

| Question | Answer |
|---|---|
| What product concept does it own? | Free-standing conversation inside a workspace. |
| Why can an existing context not own it? | Collaboration is target-bound by COL-001 and has a different access model and lifecycle; extending it would weaken that rule for comments. |
| Does it have its own aggregates and invariants? | Yes: Channel, Membership, Message (sequence, tombstone), Conversation, Thread. |
| Does it need its own persistence? | Yes: schema `chat`, tenant scoped with RLS. |
| Does it reference other contexts only by contract? | Yes: Workspaces members, Governance permissions, Collaboration mentions and notifications through ports and published facts. |
| Can it be extracted later? | Yes: no foreign keys to other schemas, facts through the outbox. |

## Alternatives considered

- **A. Extend Collaboration with channels.** Rejected: breaks COL-001, mixes two access models, and enlarges the Collaboration team scope that is still at HARDENING.
- **B. Contextual threads only, no free channels.** Rejected by the product owner (Discord-style requested).
- **C. Keep the gateway notify-only and refetch over REST.** Rejected: higher latency, no typing or presence, and every message costs a REST round trip per recipient.
- **D. A separate chat service.** Rejected for now: the repository keeps contexts inside the current backend projects until extraction readiness is proven.

## Consequences

Positive: a clear owner for chat rules, no weakening of COL-001, reuse of existing mention, attachment and notification policy.

Negative: a twelfth context to keep in the registries and hard-coded architecture tests; a protocol change with a review trigger; storage and fan-out growth that Collaboration did not have.

New obligations: `CHT` in the rule-id tooling, a product document, a use-case catalog, a team spec, a feature spec, registry updates, architecture test lists, RLS policies, contract exports and a realtime contract update.

## Compatibility and migration

Additive only. No existing table, endpoint, event or realtime frame changes. The placeholder route `/workspaces/$workspaceId/chat` and the unmounted "Chat Rooms" navigation entry are replaced by the real feature in Phase 1.

## Evidence

See the front matter. The product rules are in `docs/product/chat.md`, the work breakdown in `docs/workstreams/use-cases/chat.md` and `docs/workstreams/teams/chat.md`, the blueprint in `docs/workstreams/specs/chat-foundation.md`, and the delivery plan in `docs/workstreams/chat-delivery-plan.md`.

## Supersedes

None. It narrows, for chat only, the sentence in the Collaboration mockup that says there is no separate chat channel.

## Superseded By

None.

## Notes

Open owner decisions are listed in `docs/product/chat.md` (Stop conditions and Open decisions): moderator delete rules, retention window, guest model, and the per-workspace channel and member limits.
