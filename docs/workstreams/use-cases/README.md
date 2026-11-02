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
  - docs/delivery/product-delivery-lifecycle.md
  - docs/workstreams/product-roadmap.md
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
- Requirement IDs of `docs/workstreams/executions/work-management/work-management.spec.md` (the only retained execution spec) are cited, not restated.
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
| DONE | Every line of the acceptance standard (§7) holds on one commit of `develop`. |
| BLOCKED | Waits on a named decision or dependency. |

## 7. Acceptance standard

The four-line standard for DONE (backend, frontend, design, real end to end), the evidence rules, and who accepts what are owned by [`docs/delivery/product-delivery-lifecycle.md`](../../delivery/product-delivery-lifecycle.md) (DEL-003 to DEL-005). A use case's pull request sets DONE; the lead reads the diff and the Evidence line and merges. Versions are accepted by the owner (DEL-012).

The design canvas holding the artboards is the owner's claude.ai artifact `18L4jUXGfdXh7Wn9YQvA2x`. Artboard ids in the catalog refer to it.

## 8. System assessment (2026-10-30, develop at `cc416f805`; Identity, Workspaces, Documents, Governance and Work Management re-counted 2026-11-01; Accounts, Workspaces, Documents, Collaboration and Work Management adjusted again 2026-11-01 for decisions 3 and 7 and the Work Management index fixes; Identity counted again 2026-11-02 after the 0.1.0 sign-in, session and second-step real specs and again after the independent acceptance of its second, third, fourth and fifth batches; Workspaces counted again 2026-11-02 after the independent acceptance of its third, fourth and fifth batches)

| Context | Use cases | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | DONE | BLOCKED |
|---|---|---|---|---|---|---|---|
| Accounts | 42 | 16 | 22 | 2 | 2 | 0 | 0 |
| Identity | 61 | 6 | 10 | 12 | 6 | 23 | 4 |
| Workspaces | 91 | 6 | 13 | 45 | 12 | 13 | 2 |
| Governance | 75 | 15 | 19 | 14 | 21 | 0 | 6 |
| Work Management | 171 | 10 | 1 | 13 | 143 | 3 | 1 |
| Documents | 82 | 12 | 4 | 43 | 20 | 0 | 3 |
| Collaboration | 85 | 19 | 7 | 37 | 17 | 0 | 5 |
| Automation | 79 | 11 | 26 | 21 | 20 | 0 | 1 |
| Integrations | 55 | 13 | 20 | 11 | 11 | 0 | 0 |
| Billing | 99 | 56 | 27 | 8 | 7 | 0 | 1 |
| Analytics | 61 | 28 | 22 | 5 | 4 | 0 | 2 |
| Platform and Foundation | 58 | 4 | 33 | 4 | 16 | 0 | 1 |
| **Total** | **959** | **196** | **204** | **215** | **279** | **39** | **26** |

Thirty-nine use cases are DONE: Work Management J02 (set by the pull request that made the card's ⋯ button touch-reachable, 2026-11-03), Workspaces E02 and H05, closed 2026-11-02 after the independent acceptance at develop `4131c937a` found no gap in their lines 1–3 (CI covers their Integration classes and the build run of #286 ran their real specs), Work Management B09, accepted 2026-11-02 by an independent acceptance agent that did not build it at develop `ab1246eb3` (the same run kept Work Management B04 at HARDENING: the update validator caps a title at 200 where the Domain and the catalog say 500, and no artboard draws the title editor; the Work Management counts were re-taken with awk from the index),  Workspaces G04, G05, G06, H01 and H03 and Identity B06 and B07, accepted 2026-11-02 by a fifth independent acceptance agent that did not build them at develop `e368b0dc2` (the invitation and join pages and the forgot and reset password pages were fixed in the two changes before it; the counts were re-taken with awk from the context indexes and agree with this table), Identity B09, D01 and I01, accepted 2026-11-02 by a third independent acceptance agent that did not build them at develop `28ef68aaf` (the same run kept Identity B06 and B07 at HARDENING: a visitor who opens or reloads `/forgot-password` or `/reset-password` directly is sent to sign-in as if the session had ended; the counts were re-taken with awk from the context indexes and agree with this table, Work Management included: 171 index rows, the six-field retired rows such as A27 and A28 are not use cases), Workspaces F02 and B01, accepted 2026-11-02 by a fourth independent acceptance agent that did not build them at develop `8d139a73e` (the same run kept Workspaces G04, G05, G06, H01 and H03 and Work Management B36, A16, B28, B09 and C07 at HARDENING, each with its gap in the State line), Workspaces F05, G01, I04 and K01, accepted 2026-11-02 by an independent acceptance agent that did not build them at develop `a6708f501` (the same run held Workspaces F02, G04, G05, G06, H01, H03 and B01 and Work Management B36 and A16 at HARDENING, each with its gap in the State line; F02 and B01 were accepted by the later run named above), Identity C01, C02, C04, D03, H03 and J02, accepted 2026-11-02 by an independent acceptance agent that did not build them (the same run did not accept Identity B04, B05, B06, B07, B09, D01, I01, I02 and J01; their gaps are in their State lines), Identity B04, B05, I02 and J01, accepted 2026-11-02 by a second independent acceptance agent that did not build them at develop `876ebfb57` (the same run held Identity B06, B07, B09, D01 and I01 at HARDENING, each with its gap in the State line), and Identity C03, D02, H01, J03, K04, K05 and Work Management A24, each accepted 2026-11-01 by an acceptance agent that did not build it, and Identity H02 and K03, which that agent rejected on one line each (a concurrent activation, the missing provider step-up screen) and the fix lane accepted again after closing those lines; an independent acceptance agent confirmed H02 and K03 on all five lines on 2026-11-02 at `6ac912d0c` (see their State lines). The same run kept Work Management B36, B09, C07, A16 and B28 and sixteen Workspaces use cases at HARDENING, each with its gaps in the State line (the later run named above re-judged B36, B09, C07, A16 and B28 and kept them at HARDENING). Another independent acceptance agent, at develop `47df9399c`, judged Work Management D04, D05, D06, B09, I02 and J02 and kept all six at HARDENING, each with its gaps in the State line.

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

Lanes, pull requests, release stages and the defect policy are owned by [`docs/delivery/product-delivery-lifecycle.md`](../../delivery/product-delivery-lifecycle.md). Two catalog duties stay here:

- The pull request that implements a use case also applies that use case's "Canonical updates required" text to the owning product doc and moves the use case's state in this catalog (DEL-005).
- Defects that leak data across tenants or permissions, lose data, or lose concurrent writes are fixed before new capability in the same context (DEL-013).

## 11. Maintenance

- Change a use case's state when its evidence changes, in the same pull request.
- Do not add sprint status, assignees or percentages (`docs/workstreams/README.md` §5).
- When a capability reaches DONE for every use case, its durable rules belong in the product doc; the catalog keeps the evidence pointers only.

## 12. Version scope

Which release holds each use case, the acceptance journeys, the order of the work and the provisional windows are in [`docs/workstreams/product-roadmap.md`](../product-roadmap.md). The catalog state stays the single progress measure: a version is cut when every use case in its scope is DONE and the owner has accepted it ([`product-delivery-lifecycle.md`](../../delivery/product-delivery-lifecycle.md) DEL-014). This catalog carries no scope lists, counts of progress, percentages, assignees or dates beyond §8 (§11).
