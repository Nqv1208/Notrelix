---
document_id: ADR-009
document_type: architecture-decision
status: Accepted
owner: backend-architecture
applies_to:
  - backend
  - governance-context
  - workspaces-context
  - authorization
  - persistence
evidence:
  - docs/product/workspaces.md
  - docs/product/governance.md
  - backend/src/Notrelix.Domain/Workspaces/Spaces/Space.cs
  - backend/src/Notrelix.Application/Features/Governance/Authorization/AccessPolicyEngine.cs
  - backend/src/Notrelix.Infrastructure/Data/Authz/AccessFactsQuery.cs
  - backend/src/Notrelix.Infrastructure/Data/Authz/ResourceVisibilityQuery.cs
  - backend/tests/Notrelix.Integration.Tests/Baselines/AccessPolicyEngineCharacterizationTests.cs
review_on:
  - authorization-model-change
  - space-hierarchy-change
  - permission-subject-change
  - collection-visibility-change
---

# ADR-009: Space-Scoped Authorization — Closure-Indexed Space Tree, Team Subjects, and Datastore-Side Collection Visibility

## ID

`ADR-009`

## Status

`Accepted` — 2026-10-11 (workspace org-tree workstream, phase OT-0; product decisions confirmed by the
product owner on 2026-10-11 and recorded in `docs/product/workspaces.md` WSP-021..WSP-025 and
`docs/product/governance.md` GOV-025..GOV-029).

## Context

A Workspace must be composable from department Spaces (IT, Accounting, Marketing, …) forming an org
tree with unlimited depth, with people partitioned per department. Before this decision:

- `Space` was flat (no parent, no order, no move) and authorization never consulted Spaces;
- `PermissionScopeType.Space`, `PermissionAction.ManageSpaces/ManageTeams` existed but were unused;
  Space/Team commands were gated by Workspace actions that only Owners pass;
- `governance.resource_permissions` / `permission_rules` evaluated only `subject_type = 'User'`; Team
  grants were stored but inert;
- `work.boards.space_id` existed but was never written or read; Pages had no placement;
- collection reads (`GetBoards`, `GetBoardsBySlug`, `GetWorkspaceSpaces`, Page list/tree/search, My
  Work) returned Private resources to every Workspace member while single reads hid them — a
  `SOURCE_DEBT` leak against GOV-019 / BE-SEC-028.

Constraints: one evaluator (ADR-007), one facts command per request
(`PipelinePerformanceContractTests`), the frozen seven-behavior pipeline (ADR-006), Domain purity,
NRX-003/NRX-004 fail-closed authorization, and no cross-context persistence writes.

## Decision

1. **Hierarchy model.** `Space` carries `ParentSpaceId` and a sibling `Position`
   (`FractionalIndex`). The Domain validates cycles and sibling-name uniqueness from facts supplied by
   Application (target-parent ancestry, sibling names). Infrastructure maintains a closure index
   `workspace.space_paths (ancestor_id, descendant_id, depth)` in the same transaction as the Space
   write, under a per-Workspace transaction advisory lock, so ancestry lookups for authorization are
   index-backed at any depth and concurrent moves cannot form a cycle.
2. **Membership.** `SpaceMember` (subject `User | Team`, role `Admin | Member | Viewer`) is an entity
   of the Space aggregate; the last-Admin guard is a Domain rule. Existing Spaces get their creator
   as Space Admin in the migration.
3. **Facts.** The canonical `AccessFactsQuery` stays one SQL command. It additionally resolves, for
   the target Space (a `workspaces.space` resource, a Board's `space_id`, or a Page's Space supplied
   by the Documents-published page fact), whether the Space exists, the principal's effective Space
   role (highest User/active-Team entry on the Space or an ancestor, Member/Viewer inheritance stopped
   by a Private Space in between, Admin never stopped) and whether the Space is open (no Private Space
   from the root). Resource permissions and permission rules evaluate `User` and active `Team`
   subjects.
4. **Policy.** `AccessPolicyEngine` remains the only evaluator and owns all role→level semantics:
   Owner → explicit rules → resource restriction (Private board/page needs board membership or explicit
   grant) → Space-derived level → Workspace default. Workspace Admin holds Space Admin on every Space
   and `ManageTeams`; `ManageSpaces` on a Space requires effective Space Admin.
5. **Collections.** List/tree/search/My Work handlers obtain the visible resource ids from a
   Governance-published port whose Infrastructure implementation is one SQL authority
   (`ResourceVisibilityQuery`) that encodes the same decision for `ViewBoard` / `ViewPage` / Space
   visibility. A PostgreSQL parity test asserts that list membership equals the single-read engine
   decision across a scenario matrix, so the two authorities cannot drift silently.
6. **Placement.** Work Management and Documents own `space_id` on their resources; Workspaces
   publishes the Space facts they need. Creating in a Space is authorized on the Space; moving between
   Spaces is authorized on the resource (pipeline) and on the target Space (secondary evaluation of the
   create contract through the same facts provider and engine, as `WorkItemActionAuthorizer` does).

## Rejected alternatives

- **Recursive CTE over `parent_space_id` at every authorization read.** No extra table, but every
  facts query and every list query pays an unbounded recursion; the facts query is on the hot path of
  every protected request and has an index-backed performance contract.
- **Materialized path column (`/root/child/...`).** Cheap ancestor reads, but a move rewrites string
  prefixes of the whole subtree and prefix matching on uuids is not index-friendly for "is ancestor of"
  joins against membership rows.
- **Database triggers maintaining the closure.** Always consistent, but introduces hidden database
  behavior with no precedent in this repository and splits hierarchy semantics between C# and PL/pgSQL.
- **Copying Space grants into `governance.resource_permission_inheritance_cache` or per-user grant rows.**
  Requires fan-out on every membership/move change and makes revocation eventually consistent; the
  table stays unused.
- **Filtering collections in memory after loading all rows.** Violates GOV-019 and leaks counts/ids
  under scale.

## Consequences

- Positive: department isolation is enforced server-side for single reads and collections; Team grants
  become effective; Spaces can be managed by Workspace Admins and Space Admins instead of Owners only.
- Positive: revocation (member removed, Team archived, Space made Private) takes effect on the next
  evaluation; no derived grant copies exist.
- Negative: two SQL texts (facts and collection visibility) encode the same decision; the parity test
  is the mandatory guard and must be extended whenever the engine's Board/Page/Space branch changes.
- Negative: hierarchy writes in one Workspace serialize on the advisory lock (acceptable: rare,
  administrative operations).
- Compatibility: additive REST fields and endpoints; existing unplaced Boards/Pages keep their previous
  decisions (characterization suite unchanged). Private Spaces become invisible to non-members, which
  is the intended security fix.
- Migration: `parent_space_id`, `position`, `space_members`, `space_paths`, `docs.pages.space_id`, FK +
  index on `work.boards.space_id`; closure and creator-Admin backfill for existing Spaces; RLS through
  `ops.apply_scoped_business_policies`.
