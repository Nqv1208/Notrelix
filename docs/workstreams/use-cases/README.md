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
3. **Design**: an approved artboard exists for each screen and state, and the build matches it within 1 px at 1440, 1024 and 390 px, in light and dark.
4. **End to end**: one journey per use case runs against the real backend and covers the main flow and one rejection.
5. **Owner check**: the product owner clicks through the use case on the development stack and accepts it.

The design canvas holding the artboards is the owner's claude.ai artifact `18L4jUXGfdXh7Wn9YQvA2x`. Artboard ids in the catalog refer to it.

## 8. System assessment (2026-10-30, develop at `cc416f805`; Identity, Workspaces, Documents, Governance and Work Management re-counted 2026-11-01)

| Context | Use cases | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | BLOCKED |
|---|---|---|---|---|---|---|
| Accounts | 42 | 15 | 22 | 2 | 2 | 1 |
| Identity | 61 | 6 | 12 | 25 | 14 | 4 |
| Workspaces | 91 | 5 | 14 | 60 | 7 | 5 |
| Governance | 75 | 15 | 19 | 14 | 21 | 6 |
| Work Management | 171 | 11 | 2 | 13 | 144 | 1 |
| Documents | 82 | 10 | 4 | 43 | 20 | 5 |
| Collaboration | 85 | 17 | 7 | 37 | 17 | 7 |
| Automation | 79 | 11 | 26 | 21 | 20 | 1 |
| Integrations | 55 | 13 | 20 | 11 | 11 | 0 |
| Billing | 99 | 56 | 27 | 8 | 7 | 1 |
| Analytics | 61 | 28 | 22 | 5 | 4 | 2 |
| Platform and Foundation | 58 | 4 | 33 | 4 | 16 | 1 |
| **Total** | **959** | **191** | **208** | **243** | **283** | **34** |

No use case is DONE: none has a recorded owner check or design-fidelity proof.

Patterns that repeat across contexts, each detailed with file paths in the context files:

- Competing writes without an expected version, so a stale write silently overwrites (Work Management items, workspace members, automation rules, page rename, comments, billing plan change, account rename).
- Creates without an idempotency key, so a retry can duplicate (workspace create, invitations, page create, comment create).
- Unbounded reads that load a whole board, page tree, member list or comment thread.
- Frontend surfaces that run only on the mock backend (Billing, Analytics dashboards, custom roles, automation and integrations packages).
- Routes whose handlers throw `NotImplementedException` (page move, calendar sync) and facts that are stored but never acted on (five automation triggers, sharing policy, API token scopes).
- Real end-to-end proof covers about one use case in six.

## 9. Decisions only the product owner can make

These block use cases in several contexts. They are security-principal, tenant or data-destruction choices (`AGENTS.md` §13), so no lead decision was taken.

| # | Decision | Blocks |
|---|---|---|
| 1 | Platform operator principal: decided and built 2026-10-31 (a separate account, its own sign-in scheme and database login, an audit row per action; UC-ACC-D01, D02 HARDENING, UC-BIL-A03, D02, D03, R09 INTEGRATING). Still open: what the customer sees as the audit actor, consent-based support access, operator MFA | UC-BIL-L05, UC-ANA-X02 |
| 2 | Anonymous principal and abuse controls (rate limit, captcha, spam handling) for public forms, share links, public pages and public comments | UC-WM-N06, UC-GOV-G03, UC-GOV-G05, UC-DCT-A15, UC-COL-H08 |
| 3 | Deletion and retention: account, workspace and page deletion windows, export before deletion, identity anonymization | UC-ACC-D05, UC-WSP-D03, UC-WSP-D04, UC-DCT-A17, UC-DCT-L05, UC-COL-F04, UC-COL-F05 |
| 4 | Custom roles: place in the Governance decision order, reach into Private resources, relation to Deny rules; the moderator permission for comments | UC-GOV-C04, UC-GOV-C05, UC-COL-C02, UC-COL-F07 |
| 5 | Sign-in policy: company SSO enforcement and the verified-domain rule, required MFA owner, SCIM provisioning principal, second-factor recovery | UC-IDN-B08, UC-IDN-J04, UC-IDN-J05, UC-WSP-F10, UC-WSP-J04 |
| 6 | API token scope vocabulary and whether a token may call person-level routes | UC-IDN-L05 |
| 7 | Account suspension effect on its workspaces | UC-WSP-D06 |

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
