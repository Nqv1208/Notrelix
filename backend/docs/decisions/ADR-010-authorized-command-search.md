---
document_id: ADR-010
document_type: architecture-decision
status: Proposed
owner: backend-architecture
applies_to:
  - backend
  - workspaces-context
  - work-management-context
  - documents-context
  - governance-context
  - search
evidence:
  - docs/product/workspaces.md
  - docs/product/governance.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/src/Notrelix.Application/Features/Documents/Pages/Queries/SearchPages/SearchPages.cs
  - backend/src/Notrelix.Application/Features/Governance/Public/Visibility/IResourceVisibility.cs
review_on:
  - search-index-change
  - authorization-model-change
  - cross-context-read-change
---

# ADR-010: Authorized Command Search — Read Facade, Per-Context Search Ports and Datastore-Side Matching

## ID

`ADR-010`

## Status

Proposed

## Date

`2026-10-24`

## Owners

- `backend-architecture`

## Context

The web client's command palette needs one search box over what a person may open: Boards and Items
(Work Management), Pages (Documents), and the destinations a Space or Team gives (Workspaces). The
FE gap is `CTR-GAP-WS-COMMAND-SEARCH` (`GET /workspaces/{workspaceId}/command-search`). Today only
`Documents.Pages.SearchPages` exists and searches Pages alone.

What is undecided, and why it blocks the build (AGENTS.md section 13, no-guess policy):

- no document owns a cross-context search or its authorization rule. A result is a read of a
  Board, Item or Page and must never be visible to someone who could not open it (NRX-003, NRX-004,
  GOV-025). A search that returns a name, key or snippet is a disclosure;
- no accepted decision says whether an index table is created, who owns it, or how it is kept
  current. A projection that duplicates Board or Item names into a second store is a second copy of a
  durable fact and needs an owner, a freshness contract and RLS;
- Governance owns visibility (`IResourceVisibility`, ADR-009 collection visibility); the other contexts
  own the searchable text. Neither may read the other's tables.

The product recommendation accepted on 2026-10-23 is: Workspaces hosts a read facade that fans out to
per-context authorized search ports, matching with PostgreSQL `unaccent` and trigram, with an ADR
before any index table exists. This ADR proposes that approach in detail so it can be accepted or
changed. Nothing is built until it is Accepted.

## Decision

1. **Read facade owner.** Workspaces owns `GET /workspaces/{workspaceId}/command-search` as a pure
   query. It owns the response shape (result kind, id, title, secondary line, destination route key,
   rank) and the result ordering and limits. It stores nothing and decides no visibility.
2. **Per-context search ports.** Each context that has searchable text publishes one Application
   public port, in the style of `IBoardAccessSubjectFacts`:
   - Work Management: `IWorkSearch` (Boards by name; Items by key and name);
   - Documents: `IPageSearch` (Pages by title; the existing `SearchPages` is the first implementation);
   - Workspaces: its own Space and Team names (no port needed, same owner).
   A port takes the query text, the Workspace (from the execution context) and a limit, and returns
   already-authorized rows. The facade never filters rows by visibility itself.
3. **Authorization is inside the port, datastore side.** A port intersects its match with
   `IResourceVisibility` (Board, Page and Space ids the caller can open, ADR-009) in the same SQL
   statement, so an unauthorized row is never selected, counted or timed. Archived and deleted
   resources are excluded unless their own lifecycle contract says otherwise. A Guest sees only what an
   explicit grant gives.
4. **Matching.** Matching uses PostgreSQL `unaccent` and `pg_trgm` (`similarity` and `ILIKE` on a
   normalized form) over the owning contexts' existing columns. Item key matching is exact or prefix on
   the stored key (WM-043). The facade merges the per-context top-N lists by rank, with a fixed
   per-kind cap, and never runs an unbounded scan.
5. **No separate search index in the first release.** The first release adds only the extensions and
   per-column trigram indexes on the owning tables (an additive migration owned by each context). A
   dedicated index table or an external search engine is a separate future ADR that must show a measured
   need (latency or relevance on the real corpus), name the owning context, and define freshness and
   rebuild.
6. **Privacy of the query.** The query text is not logged at Information level and is not stored.
7. **Failure behaviour.** A context port that fails or times out omits its kind and the response says
   so (`partial` with the omitted kinds); it never widens results and never returns an error that
   reveals another context's state.

## Alternatives Considered

### Alternative A — Dedicated search index owned by Workspaces

**Description**

Workspaces keeps a table of searchable rows (kind, resource id, title, scope) fed by events from the
other contexts, and searches it.

**Benefits**

- One query, one ranking, simple pagination.

**Costs / risks**

- A second copy of names and keys with its own staleness and its own authorization copy: every
  permission, archive, rename and move must reach it, or it leaks or lies.
- Needs event consumers for every searchable kind (NRX-009, NRX-010) before it is correct.

**Why not chosen**

The authorization correctness cost is paid on every write path of every context; the benefit (one query)
does not yet justify it without a measured need.

### Alternative B — Facade queries every context's tables directly

**Description**

The Workspaces handler joins Work Management and Documents tables and Governance visibility in one query.

**Benefits**

- Fastest to write, one SQL statement.

**Costs / risks**

- Violates context data ownership: Workspaces would read Work and Document persistence.
- Any schema change in either context silently breaks search.

**Why not chosen**

Forbidden by the cross-context rules (public ports, not table reads).

### Alternative C — External search engine

**Description**

A hosted search engine indexed by events.

**Benefits**

- Best relevance and scale.

**Costs / risks**

- New operational dependency, a second authorization copy, per-tenant isolation to prove, cost.

**Why not chosen**

Premature for the scale of a Workspace; revisit under Alternative A's measured-need rule.

## Consequences

### Positive

- Authorization stays single-sourced (Governance visibility) and datastore side.
- No new durable copy of any fact; no event consumers are needed.
- Each context keeps ownership of its searchable text and may improve matching independently.

### Negative / trade-offs

- Ranking across contexts is approximate (merge of per-context top-N), and the per-kind caps bound recall.
- Trigram indexes add write cost and storage to Boards, Items and Pages.
- A fan-out call costs several queries per keystroke; the client must debounce and the facade must cap.

### New obligations

- tests/gates: for each port, Integration tests on real PostgreSQL proving a caller never receives a
  Board, Item or Page they cannot open (Private, other Space, Guest without grant, other Workspace,
  archived); an architecture test that the facade references only public ports.
- operations: migration enabling `unaccent` and `pg_trgm` where the database role allows it.
- security: query text is not logged at Information level; results carry no field beyond the
  documented ones.
- performance: query plans checked on a corpus of realistic size before acceptance of the migration.
- documentation: `docs/product/workspaces.md` (search section), `backend/docs/architecture/api-and-contracts.md`.
- ownership: each context owns its port and its trigram indexes.

## Compatibility / Migration

No existing contract changes: the endpoint is new and `Documents.Pages.SearchPages` stays as it is. The
migrations are additive (extensions and indexes). The FE keeps the search row hidden until the endpoint
exists.

## Evidence

### Canonical current architecture

- `backend/docs/architecture/security-tenancy-authorization.md`
- `backend/docs/architecture/infrastructure-and-data.md`
- `docs/product/governance.md`

### Source / manifests

- `backend/src/Notrelix.Application/Features/Governance/Public/Visibility/IResourceVisibility.cs`
- `backend/src/Notrelix.Application/Features/Documents/Pages/Queries/SearchPages/SearchPages.cs`

### Tests / gates

- Not yet written; required before this ADR is Accepted (see New obligations).

### Contracts / migrations / generated evidence

- None yet.

## Supersedes

`None`

## Superseded By

`None`

## Notes

Proposed 2026-10-24 from the accepted recommendation of 2026-10-23 ("Command search: Workspaces hosts a
read facade that fans out to per-context authorized search ports ... ADR needed before index tables").
Open questions for the acceptor: the per-kind result caps and the ranking weights; whether Item search is
in the first release or only Boards and Pages; whether Space and Team names are part of the facade.
