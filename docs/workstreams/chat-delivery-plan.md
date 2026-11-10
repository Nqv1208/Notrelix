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

# Cross-phase rules

- Contract first, then backend, mock, frontend, e2e.
- Documents, generated indexes and architecture tests change in the same pull request as the code.
- No hand edits of generated files; migrations are immutable once applied.
- Mock e2e never opens a real socket.
