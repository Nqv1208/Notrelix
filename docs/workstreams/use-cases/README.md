---
document_id: WRK-UC-README
document_type: workstream-use-case-router
status: active
owner: engineering-delivery
applies_to:
  - use-case-catalog
  - capability-acceptance
  - delivery-sequencing
evidence:
  - docs/workstreams/README.md
  - docs/workstreams/capability-delivery-map.md
  - docs/delivery/definition-of-done.md
  - docs/templates/feature-spec-template.md
review_on:
  - capability-state-change
  - product-semantic-change
  - team-capability-decomposition-change
---

# Use-case catalog

## 1. Purpose

This directory lists every use case of Notrelix, one file per bounded context (module), grouped by the capabilities that `docs/workstreams/teams/*.md` already defines.

For each use case it answers:

- who may do it, and on which resource;
- what goes in and what comes out;
- how it runs, including no-op, rejection, conflict and retry;
- which user-visible states the UI must show;
- how it is accepted;
- what exists today in backend, frontend, design and tests, and what is missing.

It lets a team, a reviewer or a coding agent build and accept a capability without inventing behaviour.

## 2. Authority position

This catalog sits at the workstream level (`docs/workstreams/README.md` §2).

- Product meaning stays in `docs/product/<context>.md`. A use case links the rule it exercises; it does not redefine it.
- Normative requirement IDs of an execution pack (`docs/workstreams/executions/<stream>/<stream>.spec.md`) are cited, not restated.
- When a use case needs behaviour the product docs do not decide, the file records the product decision and lists it under "Canonical updates required", so the owning product doc absorbs it.
- Source, tests and CI remain the proof of current behaviour.

## 3. Files

| Context | File | Team spec |
|---|---|---|
| Accounts | [accounts.md](accounts.md) | [identity-accounts](../teams/identity-accounts.md) |
| Identity | [identity.md](identity.md) | [identity-accounts](../teams/identity-accounts.md) |
| Workspaces | [workspaces.md](workspaces.md) | [workspace-governance](../teams/workspace-governance.md) |
| Governance | [governance.md](governance.md) | [workspace-governance](../teams/workspace-governance.md) |
| Work Management | [work-management.md](work-management.md) | [work-management](../teams/work-management.md) |
| Documents | [documents.md](documents.md) | [documents-collaboration](../teams/documents-collaboration.md) |
| Collaboration | [collaboration.md](collaboration.md) | [documents-collaboration](../teams/documents-collaboration.md) |
| Automation and Integrations | [automation-integrations.md](automation-integrations.md) | [automation-integrations](../teams/automation-integrations.md) |
| Billing | [billing.md](billing.md) | [billing-entitlements](../teams/billing-entitlements.md) |
| Analytics and Reporting | [analytics.md](analytics.md) | [analytics-reporting](../teams/analytics-reporting.md) |
| Platform and Foundation (system use cases) | [platform-foundation.md](platform-foundation.md) | [platform-foundation](../teams/platform-foundation.md) |

## 4. File layout

Every context file has the same parts:

1. **Assessment**: state per capability, the main gaps, defects found in shipped code, and the enterprise baseline the context is measured against.
2. **Index**: one row per use case: `ID | Use case | Capability | Actors | BE | FE | Design | Real e2e | State | Gap`.
3. **Use cases**: one block per use case, grouped under its capability.
4. **Product decisions applied**: decisions this catalog relies on that the product docs do not yet state.
5. **Canonical updates required**: exact text the owning product doc must absorb.
6. **Not covered**: product-doc sections deliberately not turned into a use case, with the reason.

## 5. Use-case block

IDs are `UC-<CTX>-<capability letter><nn>`, for example `UC-WM-A01`. They are catalog coordinates, not rule IDs.

```text
### UC-WM-A01 Create a board
- Capability: <team capability id and name>
- Actors and permission: <roles; the permission name from Governance>
- Preconditions:
- Input: <fields, limits, validation>
- Output: <state change, emitted fact, what other people see>
- Main flow: <numbered steps>
- No-op / rejection / concurrency / retry: <semantic no-op; each rejection; expected version; idempotency>
- UI states: <the feature-spec §12 states that apply>
- Rules: <product doc section and rule ids; execution-pack requirement ids>
- Acceptance: <Given / When / Then, each testable>
- Evidence: BE <route, handler> · FE <route, component> · Design <artboard> · Tests <files> · Real e2e <spec>
- State: <state word> — <gap to DONE>
```

## 6. State words

The lifecycle of `docs/workstreams/README.md` §8, applied to one use case:

| State | Meaning |
|---|---|
| DEFINED | Behaviour is specified; no code exists. |
| IMPLEMENTING | Some backend or frontend code exists; the use case cannot run end to end. |
| INTEGRATING | Backend and frontend exist and connect; end-to-end proof or required states are missing. |
| HARDENING | Runs end to end against the real backend; gaps remain in states, design fidelity, tests or security proof. |
| DONE | Every acceptance line in §7 holds on one commit of `develop`. |
| BLOCKED | Waits on a named decision or dependency. |

## 7. Acceptance standard

A use case is DONE only when all of these hold on the same commit (`docs/delivery/definition-of-done.md`):

1. **Backend**: the server authorizes every actor and returns the identical NotFound to people without access. Competing writes carry the expected version, and a stale write changes nothing. A retry with the same idempotency key has no second effect. A semantic no-op bumps no version and emits nothing. Tests cover success, each rejection, the no-op and the conflict; tenant-scoped data has a cross-tenant negative test.
2. **Frontend**: the UI uses the generated API client. Every applicable state is built: loading, empty, success, validation error, permission denied, read-only, conflict, pending, offline and destructive confirmation. Optimistic writes roll back on rejection. Keyboard and screen reader complete the flow, and axe reports no serious violation.
3. **Design**: every screen and state that has an artboard matches it within 1 px, proven by a box-tree comparison, at each width and theme the artboard draws. A state or width the canvas does not draw is built from design-system components and tokens on the pattern of the nearest drawn state, and the acceptance screenshots at 1440, 1024 and 390 px in light and dark show no overflow, clipping or squeezed columns, 44 px targets at 390 px and no serious axe violation. The Evidence line names which states were measured and which were reviewed.
4. **End to end**: one journey per use case runs against the real backend and covers the main flow and one rejection.
5. **Lead acceptance**: the lead, or an acceptance agent that did not build the use case, runs the use case on the development stack against every acceptance line and records the evidence (the real e2e spec and its pass count, screenshots or the box-tree report) in the use case's Evidence line. The owner tests released versions as a user and files what they find as defects, not as a gate.

The design canvas holding the artboards is the owner's claude.ai artifact `18L4jUXGfdXh7Wn9YQvA2x`. Artboard ids in the catalog refer to it.

## 8. System assessment (2026-10-30, develop at `cc416f805`; Identity, Workspaces, Documents, Governance and Work Management re-counted 2026-11-01; Accounts, Workspaces, Documents, Collaboration and Work Management adjusted again 2026-11-01 for decisions 3 and 7 and the Work Management index fixes; Identity counted again 2026-11-02 after the 0.1.0 sign-in, session and second-step real specs and again after the independent acceptance of its second and third batches; Workspaces counted again 2026-11-02 after the independent acceptance of its third and fourth batches)

| Context | Use cases | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | DONE | BLOCKED |
|---|---|---|---|---|---|---|---|
| Accounts | 42 | 16 | 22 | 2 | 2 | 0 | 0 |
| Identity | 61 | 6 | 10 | 12 | 11 | 18 | 4 |
| Workspaces | 91 | 6 | 13 | 45 | 19 | 6 | 2 |
| Governance | 75 | 15 | 19 | 14 | 21 | 0 | 6 |
| Work Management | 171 | 10 | 1 | 13 | 145 | 1 | 1 |
| Documents | 82 | 12 | 4 | 43 | 20 | 0 | 3 |
| Collaboration | 85 | 19 | 7 | 37 | 17 | 0 | 5 |
| Automation | 79 | 11 | 26 | 21 | 20 | 0 | 1 |
| Integrations | 55 | 13 | 20 | 11 | 11 | 0 | 0 |
| Billing | 99 | 56 | 27 | 8 | 7 | 0 | 1 |
| Analytics | 61 | 28 | 22 | 5 | 4 | 0 | 2 |
| Platform and Foundation | 58 | 4 | 33 | 4 | 16 | 0 | 1 |
| **Total** | **959** | **196** | **204** | **215** | **293** | **25** | **26** |

Twenty-five use cases are DONE: Workspaces F02 and B01, accepted 2026-11-02 by a fourth independent acceptance agent that did not build them at develop `8d139a73e` (the same run kept Workspaces G04, G05, G06, H01 and H03 and Work Management B36, A16, B28, B09 and C07 at HARDENING, each with its gap in the State line), Workspaces F05, G01, I04 and K01, accepted 2026-11-02 by an independent acceptance agent that did not build them at develop `a6708f501` (the same run held Workspaces F02, G04, G05, G06, H01, H03 and B01 and Work Management B36 and A16 at HARDENING, each with its gap in the State line; F02 and B01 were accepted by the later run named above), Identity C01, C02, C04, D03, H03 and J02, accepted 2026-11-02 by an independent acceptance agent that did not build them (the same run did not accept Identity B04, B05, B06, B07, B09, D01, I01, I02 and J01; their gaps are in their State lines), Identity B04, B05, I02 and J01, accepted 2026-11-02 by a second independent acceptance agent that did not build them at develop `876ebfb57` (the same run held Identity B06, B07, B09, D01 and I01 at HARDENING, each with its gap in the State line), and Identity C03, D02, H01, J03, K04, K05 and Work Management A24, each accepted 2026-11-01 by an acceptance agent that did not build it, and Identity H02 and K03, which that agent rejected on one line each (a concurrent activation, the missing provider step-up screen) and the fix lane accepted again after closing those lines; an independent acceptance agent confirmed H02 and K03 on all five lines on 2026-11-02 at `6ac912d0c` (see their State lines). The same run kept Work Management B36, B09, C07, A16 and B28 and sixteen Workspaces use cases at HARDENING, each with its gaps in the State line (the later run named above re-judged B36, B09, C07, A16 and B28 and kept them at HARDENING).

Patterns that repeat across contexts, each detailed with file paths in the context files:

- Competing writes without an expected version, so a stale write silently overwrites (Work Management items, workspace members, automation rules, page rename, comments, billing plan change, account rename).
- Creates without an idempotency key, so a retry can duplicate (workspace create, invitations, page create, comment create).
- Unbounded reads that load a whole board, page tree, member list or comment thread.
- Frontend surfaces that run only on the mock backend (Billing, Analytics dashboards, custom roles, automation and integrations packages).
- Routes whose handlers throw `NotImplementedException` (page move, calendar sync) and facts that are stored but never acted on (five automation triggers, sharing policy, API token scopes).
- Real end-to-end proof covers about one use case in six.

## 9. Decisions only the product owner can make

These block use cases in several contexts. They are security-principal, tenant or data-destruction choices (`AGENTS.md` §13), so no lead decision was taken, except where a row below says decided.

| # | Decision | Blocks |
|---|---|---|
| 1 | Platform operator principal: decided and built 2026-10-31 (a separate account, its own sign-in scheme and database login, an audit row per action; UC-ACC-D01, D02 HARDENING, UC-BIL-A03, D02, D03, R09 INTEGRATING). Still open: what the customer sees as the audit actor, consent-based support access, operator MFA | UC-BIL-L05, UC-ANA-X02 |
| 2 | Anonymous principal and abuse controls (rate limit, captcha, spam handling) for public forms, share links, public pages and public comments | UC-WM-N06, UC-GOV-G03, UC-GOV-G05, UC-DCT-A15, UC-COL-H08 |
| 3 | Deletion and retention: account, workspace and page deletion windows, export before deletion, identity anonymization. Decided 2026-11-01: one rule everywhere, a 30-day recoverable window, then purge. Workspace delete is Owner-only with typed-name confirmation, members lose access at once, the Owner can restore for 30 days and export during the window, and a purge job hard-deletes after 30 days with an audit fact. Account delete: the account Owner, takes its workspaces, same window. Pages and comments: trash 30 days, then purge. Identity deletion anonymizes (name "Deleted user", email replaced by a non-reversible hash; content and audit keep the user id). Workspace delete and restore are built (HARDENING); the rest is not built | none now; was UC-ACC-D05, UC-DCT-A17, UC-DCT-L05, UC-COL-F04, UC-COL-F05, now DEFINED; UC-WSP-D03 and UC-WSP-D04 are now built (HARDENING) |
| 4 | Custom roles: place in the Governance decision order, reach into Private resources, relation to Deny rules; the moderator permission for comments | UC-GOV-C04, UC-GOV-C05, UC-COL-C02, UC-COL-F07 |
| 5 | Sign-in policy: company SSO enforcement and the verified-domain rule, required MFA owner, SCIM provisioning principal, second-factor recovery | UC-IDN-B08, UC-IDN-J04, UC-IDN-J05, UC-WSP-F10, UC-WSP-J04 |
| 6 | API token scope vocabulary and whether a token may call person-level routes | UC-IDN-L05 |
| 7 | Account suspension effect on its workspaces. Decided 2026-11-01: a suspended account is read-only end to end; automation runs and outbound webhooks are skipped and recorded with reason account.suspended, public forms and share-link writes refuse, reads stay open, nothing is deleted. Not built | none now; was UC-WSP-D06, now DEFINED |

Also needs the owner: the plan tier list (artboard shows four tiers, catalog has three) and where job title lives (owner decision of 2026-10-27 puts it in the Identity profile; product docs and code keep it in Workspaces).

Architecture decisions that wait for a real consumer: ordered message delivery (UC-PLT-H04), live co-editing (UC-DCT-K03, UC-COL-E04), integration execution principal (UC-COL-H07).

## 10. Delivery rule

- One use case at a time, through review, QA and green CI on the same commit, then merge.
- The pull request that implements a use case also applies that use case's "Canonical updates required" text to the owning product doc and moves the use case's state in this catalog.
- Defects that leak data across tenants or permissions, lose data, or lose concurrent writes are fixed before new capability in the same context.

## 11. Maintenance

- Change a use case's state when its evidence changes, in the same pull request.
- Do not add sprint status, assignees or percentages (`docs/workstreams/README.md` §5).
- When a capability reaches DONE for every use case, its durable rules belong in the product doc; the catalog keeps the evidence pointers only.

## 12. Version scope

A use case belongs to the first version whose scope lists it. The catalog state stays the single progress measure: a version is cut when every use case in its scope is DONE (`docs/delivery/ci-cd-architecture.md` §13). This section names scope; it carries no counts of progress, percentages, assignees or dates (§11).

### 12.1 Version 0.1.0: Work Management core, sign-in and session, workspaces and members

Scope as the owner approved it, pinned to use-case IDs. Teams, onboarding and Home, the rest of Spaces, comments and attachments on items, provider sign-in, profile and API tokens are not in it unless the owner adds them. Use cases BLOCKED on an owner decision (§9) are outside it until the owner decides.
Workspace delete and restore (UC-WSP-D03, D04) are in 0.1.0 now that owner decision 3 is made (§9).

**Identity (`identity.md`)**

| Capability | Name | Use cases | Count |
|---|---|---|---|
| A | Session bootstrap | A01-A03 | 3 |
| B | Login | B01-B07, B09 | 8 |
| C | Logout | C01-C04 | 4 |
| D | Session expiration | D01-D05 | 5 |
| H | MFA enrollment | H01-H03 | 3 |
| I | MFA challenge | I01-I02 | 2 |
| J | MFA recovery (with disable) | J01-J03 | 3 |
| K | Security settings (step-up only) | K03-K05 | 3 |
| | Total | | 31 |

**Workspaces (`workspaces.md`)**

| Capability | Name | Use cases | Count |
|---|---|---|---|
| A | Workspace create | A01-A02, A04 | 3 |
| B | Workspace read / resolve | B01, B04 | 2 |
| C | Workspace update | C01 | 1 |
| D | Workspace lifecycle transition (delete and restore only) | D03-D04 | 2 |
| E | Membership read | E01-E02 | 2 |
| F | Membership add / remove / change | F02, F05 | 2 |
| G | Invitation create | G01, G03-G06 | 5 |
| H | Invitation accept / reject / revoke | H01-H03, H05-H06 | 5 |
| I | Workspace selection / switch integration | I01-I02, I04 | 3 |
| K | Spaces (minimal: create, read, place a board) | K01-K02, K15 | 3 |
| | Total | | 28 |

**Work Management (`work-management.md`)**

| Capability | Name | Use cases | Count |
|---|---|---|---|
| A | Board lifecycle | A01-A10, A12-A18, A21-A22, A24, A26 | 21 |
| B | BoardItem lifecycle | B01-B16, B18-B20, B22-B26, B28-B31, B33-B36 | 32 |
| C | BoardField definitions | C01-C02, C04, C06-C16, C22 | 15 |
| D | Field values | D01-D12 | 12 |
| E | Groups and grouping | E01-E06 | 6 |
| F | Ordering | F01-F04 | 4 |
| G | Checklists | G01-G02 | 2 |
| H | Query, filter and sort foundation | H01-H19 | 19 |
| I | Table view | I01-I03 | 3 |
| J | Kanban view | J01-J06 | 6 |
| K | Calendar view | K01 | 1 |
| L | Timeline view | L01 | 1 |
| N | Form input | N01-N05 | 5 |
| O | Activity and event production | O01, O04 | 2 |
| P | Realtime synchronization | P01-P04 | 4 |
| T | Performance and hardening | T01 | 1 |
| | Total | | 134 |

Version 0.1.0 holds 193 use cases.

### 12.2 Later versions (capabilities only)

- 0.2: Documents and Collaboration.
- 0.3: Governance, Accounts and the operator console.
- 0.4: Automation and Integrations.
- 1.0: general availability, with real Billing, Analytics and the deploy pipeline.
