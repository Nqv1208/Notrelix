---
document_id: WRK-CHAT-DELIVERY-PLAN
document_type: workstream-plan
status: draft
owner: chat-team
applies_to:
  - chat
  - delivery-plan
evidence:
  - docs/workstreams/teams/chat.md
  - docs/workstreams/use-cases/chat.md
  - docs/workstreams/specs/chat-foundation.md
  - docs/decisions/SYS-ADR-001-chat-bounded-context.md
  - docs/delivery/product-delivery-lifecycle.md
review_on:
  - capability-state-change
  - phase-change
---

# Chat delivery plan

Phases, slices, entry and exit gates. Agents build to HARDENING; the owner sets DONE; one slice at a time per lane.

# Phase 0: documents and admission

Slices: ADR, product document, use-case catalog, team spec, feature spec, registry updates, doc tooling (`CHT` prefix), `make docs-generate` and `make docs-check`.
Exit: SYS-ADR-001 Accepted by the owner; docs checks green.

# Phase 1: core (capabilities A, B, D, part of C)

| Slice | Content | Use cases |
|---|---|---|
| S1.0 | Context scaffolding: folders, `DbSchemas.Chat`, DbContext interface, DI, architecture test lists, RLS lines, permission actions | none |
| S1.1 | Spaces and members (channels, groups): domain, migration, endpoints, tests | A01 to A08 |
| S1.2 | Messages: send with sequence allocation, history, edit, delete, system messages | B01 to B05, B09 |
| S1.3 | Read state: boundary, unread summary, mute, levels | D01 to D07 |
| S1.4 | Realtime core: scope `Channel`, authorization query, event frames, per-user stream, access-changed | C01 to C06, C09 |
| S1.5 | Contracts: OpenAPI, events, realtime contract, codegen | all |
| S1.6 | Mock handlers and event emit helper | all |
| S1.7 | Frontend package, route, navigation, sidebar, message list, composer, i18n (en, vi) | A, B, D UI |
| S1.8 | Realtime client adapter, cache reducers, recovery, a11y, mock e2e | C UI, B06, B07, B10 |

Entry: Phase 0 exit. Exit: all Phase 1 use cases HARDENING with evidence; RLS negative tests; concurrent send test; load test recorded; owner tries it in the real stack.
Risks: sequence hot spot per space (measure), fan-out limits, hard-coded architecture lists.

# Phase 2: interaction (E, F, G, rest of C)

| Slice | Content | Use cases |
|---|---|---|
| S2.1 | Conversations: direct, group, hide, block | E01 to E07 |
| S2.2 | Threads and replies | F01 to F05, F08 |
| S2.3 | Quote-reply, pins | F02, F06, F07 |
| S2.4 | Mentions and broadcast, mention counters | G01 to G04, D04 |
| S2.5 | Reactions | G06, G07 |
| S2.6 | Attachments through the shared policy | G08 |
| S2.7 | Typing and presence frames | C07, C08 |
| S2.8 | Notification intents to the Inbox, links to resources | G05, G09 |

Entry: Phase 1 exit. Exit: all Phase 2 use cases HARDENING; mention privacy tests (COL-030); load test with typing and presence.

# Phase 3: advanced (H)

| Slice | Content | Use cases |
|---|---|---|
| S3.1 | Permission overrides, guest hardening | H01, H02 |
| S3.2 | Moderation and Audit facts | H03, H04 |
| S3.3 | Retention and purge, export | H05, H06 |
| S3.4 | Search | H07 |
| S3.5 | Rate limits and abuse controls, email digest | H08, H09 |
| S3.6 | Resource links | H10 |

Entry: Phase 2 exit and the owner decisions in `docs/product/chat.md` section 34. Exit: all use cases HARDENING; operations runbook; performance budgets met.

# Status of Phase 3 (2026-11-12)

Built to HARDENING (the owner tests and sets DONE): S3.1 to S3.4 (H01 to H07), H08 (rate limits and slow mode), and, in the last slice, H09 (the email digest, the job `Chat.Notifications.Digest` on the shared email outbox) and H10 (resource links: `Chat.Channels.Link`, `Unlink`, `ListLinked`, the header chip, the Link resource dialog and the Discuss entry). The last slice delivered S3.5 and S3.6 together, so no Phase 3 use case is DEFINED any more.

Not built or not done (the exit gate of Phase 3 is not met until these are):
- The operations runbook (jobs, the digest window, retention, export, the Chat settings).
- Performance budgets and the load test (typing and presence included); the limits and the 50,000-message export bound are not tuned against load.
- Browser verification of every Phase 3 surface (no browser check was made for the link dialog, the digest preference copy or the Discuss entry), design records, axe records, real e2e, and the owner's own check.
- Gaps recorded in the use-case catalog: a Team cannot be linked (Governance publishes no Team visibility fact); the Discuss entry is on the Board header only (the Page header needs a slot in the docs host); the digest has no per-person frequency, no catch-up of missed windows, no Guest digest, and its link opens the channel, not the message; no real mail was sent in any test.

# Status of the catalog (2026-11-13)

Built to HARDENING (59 of 68 use cases; the owner tests and sets DONE): Phase 1 (S1.0 to S1.8: A01, A03, A05, A06; B01 to B06, B09; C01 to C06, C09; D01 to D06), Phase 2 (S2.1 to S2.8: C07, C08; E01 to E07; F01 to F04, F06 to F08; G01 to G09) and Phase 3 (H01 to H10). The catalog rows were recounted against the code in this slice (S3.6, a stacked polish slice): a row is HARDENING only where its backend, mock handler, UI and tests all exist.

Built in S3.6 (small gaps left by earlier slices): the thread root carries `threadReplyCount` and `threadLastReplyAt` on every message view (read in one batched query per page); the sidebar reads the category names (`Chat.ChannelGroups.List`) and shows them with a numbered fallback; the caller's blocks are read from the server (`Chat.Blocks.List`) with a Blocked people dialog to unblock.

Still DEFINED (9 use cases, each with what is missing in its State line): A02 (no UI to create, rename or delete a category), A04 (no reorder or collapse UI), A07 (no member removal or channel leave UI), A08 (no unarchive or delete UI), B07 (draft kept in memory only, untested), B08 (plain text only: no markup, autolinks or host previews), B10 (no Copy link action), D07 (no Mark all as read entry), F05 (no persisted sweep, no archived chip).

Capabilities I and J are specified in the catalog change of their own pull request and are DEFINED and not built; they are not in the counts above.

# Cross-phase rules

- Contract first, then backend, mock, frontend, e2e.
- Documents, generated indexes and architecture tests change in the same pull request as the code.
- No hand edits of generated files; migrations are immutable once applied.
- Mock e2e never opens a real socket.
