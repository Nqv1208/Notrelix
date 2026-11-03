---
document_id: WRK-PRODUCT-ROADMAP
document_type: workstream-roadmap
status: active
owner: engineering-delivery
applies_to:
  - use-case-catalog
  - release-scope
  - delivery-sequencing
  - lanes
evidence:
  - PRODUCT.md
  - docs/delivery/product-delivery-lifecycle.md
  - docs/workstreams/use-cases/README.md
  - docs/workstreams/cross-team-dependencies.md
  - docs/architecture/bounded-context-map.md
review_on:
  - release-scope-change
  - owner-decision-recorded
  - catalog-state-recount
  - lane-throughput-measured
---

# Product roadmap: from 0.1.0 to 1.0

This is a plan. It is not product, architecture or delivery authority (`DOC-013`). It allocates every use case of the catalog to one release and says in what order the work runs. The rules for acceptance, lanes and releases are in [`product-delivery-lifecycle.md`](../delivery/product-delivery-lifecycle.md). Product meaning and vision are in [`PRODUCT.md`](../../PRODUCT.md).

Counts come from the catalog index at develop `3916a25a5` (2026-11-03). Re-take them at each release's R0 (DEL-009). The catalog state stays the only progress measure; this document carries no assignees and no dates beyond the provisional windows in section 6.3.

# 1. Strategy to general availability

1. **Order by dependency** ([`cross-team-dependencies.md`](cross-team-dependencies.md) section 5 and 6): foundation and Work Management, then Documents and Collaboration, then organisation and governance, then Automation and Integrations, then commerce and operations.
2. **Each release is usable by one group of people end to end.** A release is never a half-built layer.
3. **The allocation says when a use case must be DONE, not when its code may start.** Code of a later release may exist earlier; its proof is due in its own release.
4. **Earlier releases run against later contexts through their default contract** (DEL-015). Billing is the main case: apart from the seat gate (`BIL-F01`), no plan gate applies before 1.0 and the entitlement port allows by default.
5. **One use case, one release.** Pairs that describe the same behaviour from two contexts stay together (section 3.2).

# 2. Releases at a glance

| Release | What a user can do | Contexts | Use cases | Still to do |
|---|---|---|---:|---:|
| 0.1.0 | A team works on boards: sign in with a second factor, create a workspace, invite people, build a board with fields and views, move work, and see it live. | Identity 30, Workspaces 28, Governance 10, Work Management 134, Billing 1, Platform 17 | 220 | 125 |
| 0.2 | The team writes pages, comments, mentions people, attaches files and works from an Inbox. | Work Management 10, Documents 81, Collaboration 83, Platform 1 | 175 | 175 |
| 0.3 | An organisation is governed: Teams, Spaces, onboarding and Home, custom roles and sharing, admin and operator consoles, provider sign-in, profile and API tokens. | Accounts 42, Identity 30, Workspaces 63, Governance 64, Work Management 17, Platform 4 | 220 | 220 |
| 0.4 | Work reacts and connects: automation rules, provider connections, inbound and outbound events. | Identity 1, Work Management 4, Automation 79, Integrations 55, Platform 7 | 146 | 146 |
| 1.0 | The product can be sold and run: real Billing, dashboards and reports, the rest of the platform, deploy, backup and observability. | Work Management 6, Billing 98, Analytics 61, Platform 28 | 193 | 193 |
| After 1.0 | Waits for an architecture decision with a real consumer, or a deferral already decided. | Governance 1, Documents 1, Collaboration 2, Platform 1 | 5 | 5 |
| | | **Total** | **959** | **864** |

State of the allocated use cases today:

| Release | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | DONE | BLOCKED | Total |
|---|---:|---:|---:|---:|---:|---:|---:|
| 0.1.0 | 0 | 0 | 3 | 122 | 95 | 0 | 220 |
| 0.2 | 31 | 12 | 82 | 45 | 0 | 5 | 175 |
| 0.3 | 46 | 67 | 78 | 17 | 0 | 12 | 220 |
| 0.4 | 27 | 52 | 32 | 33 | 0 | 2 | 146 |
| 1.0 | 91 | 67 | 13 | 19 | 0 | 3 | 193 |
| After 1.0 | 1 | 0 | 0 | 0 | 0 | 4 | 5 |
| **Total** | **196** | **198** | **208** | **236** | **95** | **26** | **959** |

Provisional windows (section 6.3): 0.1.0 about 1 week; each later release 3 to 6 weeks.

# 3. Allocation of the 959 use cases

Notation, as in the former catalog scope: a capability letter alone is every use case of that capability (`C`); `A-E` is every use case of capabilities A to E; `A01-A03` is a range inside one capability; `A01-A10, A12` lists the rest. Prefixes: `ACC` Accounts, `IDN` Identity, `WSP` Workspaces, `GOV` Governance, `WM` Work Management, `DCT` Documents, `COL` Collaboration, `AUT` Automation, `INT` Integrations, `BIL` Billing, `ANA` Analytics, `PLT` Platform. Every use case appears in exactly one table.

## 3.1 Tables

### Release 0.1.0

0.1.0 is the 192 use cases the owner approved for Work Management core, sign-in and session, workspaces and members, plus 28 foundation use cases the owner added on 2026-11-03: Governance `B01`, `D01`, `D03-D04`, `E01`, `E04-E06`, `F01`, `I01`; Platform `A01-A03`, `C01-C02`, `D03`, `F01`, `G01`, `G02`, `G08`, `I01-I04`, `K01-K03`; Billing `F01`. It was built on unaccepted authorization, realtime, CSRF, outbox and idempotency foundations; these 28 close that gap. On 2026-11-03 the owner moved `IDN-D04` and `GOV-D02` to 0.3 (section 5).

| Context | Use cases | Count |
|---|---|---:|
| `IDN` | A01-A03, B01-B07, B09, C, D01-D03, D05, H, I, J01-J03, K03-K05 | 30 |
| `WSP` | A01-A02, A04, B01, B04, C01, D03-D04, E, F02, F05, G01, G03-G06, H01-H03, H05-H06, I01-I02, I04, K01-K02, K15 | 28 |
| `GOV` | B01, D01, D03-D04, E01, E04-E06, F01, I01 | 10 |
| `WM` | A01-A10, A12-A18, A21-A22, A24, A26, B01-B16, B18-B20, B22-B26, B28-B31, B33-B36, C01-C02, C04, C06-C16, C22, D-L, N01-N05, O01, O04, P, T01 | 134 |
| `BIL` | F01 | 1 |
| `PLT` | A, C01-C02, D03, F01, G01-G02, G08, I01-I04, K | 17 |
| | **Total** | **220** |

### Release 0.2

| Context | Use cases | Count |
|---|---|---:|
| `WM` | O02-O03, R | 10 |
| `DCT` | A-J, K01-K02, L | 81 |
| `COL` | A-D, E01-E03, E05, F, G, H01-H06, H08 | 83 |
| `PLT` | G05 | 1 |
| | **Total** | **175** |

### Release 0.3

| Context | Use cases | Count |
|---|---|---:|
| `ACC` | A-H | 42 |
| `IDN` | A04, B08, D04, E-G, J04-J05, K01-K02, K06-K11, L01-L04, L06-L08, M | 30 |
| `WSP` | A03, B02-B03, C02, D01-D02, D05-D06, F01, F03-F04, F06-F10, G02, G07-G09, H04, I03, I05-I06, J, K03-K14, L-N | 63 |
| `GOV` | A01-A03, B02-B05, C, D02, D05-D15, E02-E03, E07-E13, F02-F05, G, H, I02-I03 | 64 |
| `WM` | A11, A19-A20, A23, A25, A27, B17, B21, B27, C03, C05, C17-C21, N06 | 17 |
| `PLT` | B04, G07, I05, J03 | 4 |
| | **Total** | **220** |

### Release 0.4

| Context | Use cases | Count |
|---|---|---:|
| `IDN` | L05 | 1 |
| `WM` | B32, Q | 4 |
| `AUT` | A-N | 79 |
| `INT` | A-N | 55 |
| `PLT` | B02, C03, G03-G04, H01-H03 | 7 |
| | **Total** | **146** |

### Release 1.0

| Context | Use cases | Count |
|---|---|---:|
| `WM` | M, S, T02-T05 | 6 |
| `BIL` | A-E, F02-F04, G-X | 98 |
| `ANA` | A-X | 61 |
| `PLT` | B01, B03, D01-D02, D04, E, F02, G06, H05, J01-J02, L-P | 28 |
| | **Total** | **193** |

### After 1.0

| Context | Use cases | Count |
|---|---|---:|
| `GOV` | A04 | 1 |
| `DCT` | K03 | 1 |
| `COL` | E04, H07 | 2 |
| `PLT` | H04 | 1 |
| | **Total** | **5** |

| Use case | Why it waits |
|---|---|
| `PLT-H04` | Ordered delivery for a consumer that needs it: ADR waits for a real consumer |
| `DCT-K03` | Live co-editing with cursors: ADR waits for a real consumer |
| `COL-E04` | Typing and cursor indicators: depends on live co-editing |
| `COL-H07` | Provider comment sync: integration execution principal ADR waits for a real consumer |
| `GOV-A04` | Field-level permission: deferred by lead decision L11 behind Work Management field work |

## 3.2 Pairs kept in one release

| Pair | Release |
|---|---|
| `WM-R` with `COL-A`, `COL-B` | 0.2 |
| `IDN-L01` to `L03` with `GOV-H05` to `H07` | 0.3 |
| `WM-A19`, `A20`, `A23` with `WSP-G07`, `G08` and `GOV-D07` | 0.3 |
| `WSP-F10` with `ACC-H15`, `H16` | 0.3 |
| `IDN-B08` with `ACC-H13`, `H14` | 0.3 |
| `IDN-D04` with `IDN-L08` (ending a person's access needs the Accounts caller) | 0.3 |
| `WSP-D06` with `ACC-D06` | 0.3 |
| `AUT-D09` with `INT-F03` | 0.4 |

## 3.3 Allocations that differ from capability order

These follow a dependency rather than the capability. The owner may reverse any of them.

| Use case | Release | Reason |
|---|---|---|
| `PLT-G05` | 0.2 | Queued email serves Collaboration notification delivery |
| `PLT-B04`, `PLT-G07`, `PLT-I05`, `PLT-J03` | 0.3 | Account switch reset pairs with Accounts; draft sweep with onboarding; closing a subscription when access ends with `GOV-E09`; rate limits with the abuse controls of owner decision 2 |
| `PLT-B02`, `PLT-C03`, `PLT-G03`, `PLT-G04`, `PLT-H01` to `H03` | 0.4 | Automation and Integrations need a bounded background principal, consumer dedup, stale-claim recovery, retry with dead letter and declared ordering (`cross-team-dependencies.md` section 14) |
| `IDN-L05` | 0.4 | Token scopes (owner decision 6) matter when automation and integrations call the API; tokens without scopes ship in 0.3 |
| `WM-N06` | 0.3 | Public forms wait for the anonymous principal (owner decision 2) with share links |
| `IDN-D04`, `GOV-D02` | 0.3 | Owner decision 2026-11-03: ending access when a person is disabled needs the Accounts caller; the Team picker in the Share dialog needs Teams (`WSP-K`) and an artboard |

## 3.4 Platform use cases left in 1.0 that earlier releases already exercise

`PLT-B01`, `B03`, `D01`, `D02`, `D04`, `E`, `F02` and `G06` are exercised by code that 0.1.0 and later releases already run. They stay allocated to 1.0 for their DONE proof, as the owner chose; the acceptance journeys of each release exercise them indirectly. This is a risk (section 8).

# 4. Per release

## 4.1 Release 0.1.0: a team works on boards

**Goal.** A person registers, signs in with a second factor, creates a workspace, invites colleagues, and the team builds a board with fields, groups, items and four views, moves work on Kanban, edits without losing writes, and sees changes live.

**Scope.** 220 use cases: integrating 3, hardening 122, done 95. Still to do: 125.

**Owner decisions needed before it starts.** None open. Decisions 1, 3 and 7 are made.

**Dependencies.** Identity then Workspaces then Governance baseline then Work Management; Platform underneath all. The seat gate (`BIL-F01`) is the one real plan limit; every other entitlement check allows by default.

**Acceptance journeys** (owner, on the candidate; from `PRODUCT.md` section 20 journeys A, B, C):

| # | Journey | Passes when |
|---|---|---|
| 1 | Register, confirm the email from the link, sign in, sign out | Five wrong passwords lock sign-in; reset password works |
| 2 | Turn on a second factor, sign out, sign in with a code, then with a recovery code, then turn it off | Each step needs the right factor; codes regenerate |
| 3 | Stay idle, then act; revoke the session from another device | The session refreshes silently; a revoked session returns to sign-in with a message |
| 4 | Create a workspace, invite a person by email, they register through the link and join; change their role; remove them | The removed person loses access at once |
| 5 | Delete a workspace with typed-name confirmation, then restore it within 30 days | Members lose access on delete and regain it on restore |
| 6 | Build a board: fields of each type, groups, items, Table, Kanban, Calendar and Timeline views, filter and sort | All views show the same items |
| 7 | Drag an item to another Kanban column | Table and Calendar agree after reload; a second browser sees it without reloading |
| 8 | Two browsers edit the same item | The second save is refused with the current value shown; no write is lost |
| 9 | A Viewer tries to edit; a non-member opens the board address; a member is granted then removed from a private board | Edit is refused; the non-member sees the same NotFound as for a missing board; access ends at once |
| 10 | Submit the internal form; import a CSV; bulk edit; export | Items match the file; one bad row refuses nothing silently |

**Exit criteria.** DEL-014, plus: every protected endpoint of the scope declares its authorization and has a cross-tenant negative test; realtime recovers after a dropped connection; the quality investments marked 0.1.0 in section 7 have landed.

## 4.2 Release 0.2: the team writes and talks

**Goal.** Pages with blocks and a page tree, links between work and knowledge, comments with replies and mentions, attachments, watching, Inbox and notifications, all live.

**Scope.** 175 use cases: defined 31, implementing 12, integrating 82, hardening 45, blocked 5. Still to do: 175.

**Owner decisions needed before it starts.** Decision 2 (anonymous principal and abuse controls) for `DCT-A15` and `COL-H08`; decision 4 (moderator permission) for `COL-C02` and `COL-F07`; the owner context of favourites for `DCT-A16`. Per DEL-010, a use case whose decision is missing at R0 moves to 0.3.

**Dependencies.** 0.1.0 Work Management, Governance resource authorization and realtime. Collaboration reads Identity. `WM-O03` emits events that Analytics consumes only in 1.0 (producer side only). Notification email uses the email outbox (`PLT-G05`).

**Acceptance journeys** (journeys D and E):

| # | Journey | Passes when |
|---|---|---|
| 1 | Create, nest, move and reorder pages | The tree survives reload and a second user's move |
| 2 | Write every block type; two people edit; browse versions and restore one | A stale block write is refused; restore is guarded |
| 3 | Embed a board and an item in a page | A person without access to the board sees a placeholder, never its data |
| 4 | Comment on an item and on a page; reply, edit, delete, react | The same target access rules apply on both |
| 5 | Mention a person | They are notified once, even after a retry; a mention does not grant access |
| 6 | Open the Inbox: read, done, snooze, mute, reply; save preferences | The unread count is right after switching workspace |
| 7 | Attach a file to an item and download it; watch an item | A watcher is notified of a change |
| 8 | A second browser during all of the above | Comments and page changes arrive live; a dropped connection recovers |
| 9 | Move a page to the trash and restore it | Restore works inside 30 days |
| 10 | Remove a member's access while they have a page open | They cannot save or comment afterwards |

**Exit criteria.** DEL-014, plus: no Documents or Collaboration read bypasses Governance; unbounded thread and page reads are bounded.

## 4.3 Release 0.3: an organisation is governed

**Goal.** Teams and Spaces, onboarding and Home, custom roles, sharing and guests, workspace and account administration, operator console, Google and Microsoft sign-in, profile, API tokens.

**Scope.** 220 use cases: defined 46, implementing 67, integrating 78, hardening 17, blocked 12. Still to do: 220.

**Owner decisions needed before it starts.** Decision 2 for `WM-N06`, `GOV-G03`, `GOV-G05`; decision 4 (custom roles in the decision order) for `GOV-C04`, `GOV-C05`; decision 5 (sign-in policy, verified domain, SCIM principal, second-factor recovery) for `IDN-B08`, `IDN-J04`, `IDN-J05`, `WSP-F10`, `WSP-J04`; where the job title lives (Identity or Workspaces); the admin-plane decisions for `GOV-H13`, `GOV-H14`; retention log and expiry for `GOV-D11`, `GOV-D12`; what the customer sees as the operator audit actor (decision 1, open part). Per DEL-010, a blocked use case without its decision at R0 moves to the next release.

**Dependencies.** 0.1.0 Governance baseline and Identity. Accounts depends on Identity. The plan limit (`WSP-A03`), plan label (`ACC-B03`), "not entitled" (`ACC-H20`) and `GOV-E11` run against the default Billing contract (DEL-015).

**Acceptance journeys:**

| # | Journey | Passes when |
|---|---|---|
| 1 | New owner completes four-step onboarding, sees the getting-started card and Home | Recent items, pins and activity show; the card dismisses |
| 2 | Create a Team and a Space, add members, give the Team access to a board | Members reach the board; others get NotFound |
| 3 | Create a custom role, assign it, ask "why can I not do this?" | The explanation matches the decision |
| 4 | Invite a guest to one board | The guest sees only that board |
| 5 | Create a share link, open it signed out, revoke it, then turn links off for the workspace | Each step takes effect at once |
| 6 | Open workspace settings, sharing and security, read and export the audit log | The export matches what the screen shows |
| 7 | Account administration: members, verified domain, identity provider | Domain routing and auto-join follow the settings |
| 8 | Suspend an account as operator | The account is read-only end to end; nothing is deleted; lifting restores it |
| 9 | Sign in with Google and with Microsoft; link and unlink; change password and email | Step-up is asked where the product says |
| 10 | Edit profile, photo and preferences; issue, use and revoke an API token | A revoked token fails at once |

**Exit criteria.** DEL-014, plus: every Governance action is in the role baseline; an Owner-only action stays Owner-only; permission changes end access at once in the API, caches and realtime.

## 4.4 Release 0.4: work reacts and connects

**Goal.** A person authors an automation rule; a provider connection runs; inbound events are verified and deduplicated; outbound operations run with retry.

**Scope.** 146 use cases: defined 27, implementing 52, integrating 32, hardening 33, blocked 2. Still to do: 146.

**Owner decisions needed before it starts.** Decision 6 (token scope vocabulary and person-level routes) for `IDN-L05`. Automation runs as one named person in version one (decision of 2026-10-30).

**Dependencies.** Work Management events (`O`), Governance, Platform messaging (the 0.4 Platform use cases in section 3.3). Automation and Integrations meet in `AUT-D09` and `INT-F03`. Entitlement checks (`AUT-M`, `INT-M`) run against the default contract.

**Acceptance journeys** (journeys F and G):

| # | Journey | Passes when |
|---|---|---|
| 1 | Author a rule: item change, condition, update a field | The change appears on the board marked as automation-caused |
| 2 | Make an action fail | Retries follow the policy; history shows failed with the reason |
| 3 | The rule's person loses access to the board | The next run is refused and recorded |
| 4 | Disable then enable a rule | A disabled rule never runs; no run is lost on enable |
| 5 | Connect a provider and revoke the connection | Health shows; revoked access stops provider calls |
| 6 | Deliver one signed inbound webhook twice | One effect; a bad signature is refused |
| 7 | Send an outbound message from a rule | One message per run, also after a retry |
| 8 | Provider returns a rate limit and a failure | Errors are normalised; retry respects the limit |
| 9 | Import from a provider; sync an item both ways | A conflict is reconciled by the stated rule |
| 10 | Watch an execution live | Status changes arrive without reloading |

**Exit criteria.** DEL-014, plus: no run executes with more rights than its person has; a duplicate message has one effect; secrets never appear in logs or screens.

## 4.5 Release 1.0: sold and run

**Goal.** A customer subscribes and pays; limits and seats are enforced; dashboards and reports show only what the viewer may see; the operator can deploy, back up, restore and observe the service.

**Scope.** 193 use cases: defined 91, implementing 67, integrating 13, hardening 19, blocked 3. Still to do: 193. The largest release; the owner may cut scope at R0 (DEL-010).

**Owner decisions needed before it starts.** The plan tier list (the artboard shows four tiers, the catalog three); operator MFA and consent-based support access (decision 1, open part) for `BIL-L05`, `ANA-X02`; the usage metric for `ANA-G02`.

**Dependencies.** Billing replaces the default entitlement contract in every earlier context (DEL-015). Analytics reads the source contracts of all contexts. Platform use cases left for 1.0 (section 3.4).

**Acceptance journeys** (journeys H and I):

| # | Journey | Passes when |
|---|---|---|
| 1 | A free workspace upgrades to Pro in the provider test mode, then downgrades | Entitlements change with the subscription; recovery path is shown |
| 2 | Invite beyond the seat limit | Refused with a seat message; "not entitled" differs from "not authorised" |
| 3 | View invoices and the payment method; fail a payment | Grace and delinquency follow the rules; paying recovers |
| 4 | Replay a provider webhook; let drift occur | One effect; reconciliation corrects the drift |
| 5 | Build a dashboard over board data | Freshness is shown; a person without access sees no data |
| 6 | Export a report | The export respects the viewer's authorisation |
| 7 | Operator signs in with a second factor and moves an account to another plan | One audit row per action |
| 8 | Deploy the candidate to staging, upgrade from 0.4 | Migrations run; the stack reports healthy |
| 9 | Back up and restore into a new environment | Data and access match the source |
| 10 | Open a board of the published size on desktop and the native app | Load time is inside the budget |

**Exit criteria.** DEL-014, plus: staging deploy path reactivated; a restore drill passed; alerts fire on outbox age and dead letters.

# 5. Owner decisions by release

| Decision (`use-cases/README.md` section 9) | Needed before | Blocks |
|---|---|---|
| 2 Anonymous principal and abuse controls | 0.2 and 0.3 | `DCT-A15`, `COL-H08`, `WM-N06`, `GOV-G03`, `GOV-G05` |
| 4 Custom roles and moderator permission | 0.2 (moderator) and 0.3 (roles) | `COL-C02`, `COL-F07`, `GOV-C04`, `GOV-C05` |
| 5 Sign-in policy | 0.3 | `IDN-B08`, `IDN-J04`, `IDN-J05`, `WSP-F10`, `WSP-J04` |
| Where job title lives | 0.3 | Identity profile and Workspaces member |
| 6 API token scopes | 0.4 | `IDN-L05` |
| Plan tier list; operator MFA and support access | 1.0 | Billing plans, `BIL-L05`, `ANA-X02` |

Decisions recorded in the roadmap (DEL-010):

| Date | Decision | Effect |
|---|---|---|
| 2026-11-03 | `IDN-D04` (end access when a person is disabled) and `GOV-D02` (give a Team access, with the Team picker in the Share dialog) move from 0.1.0 to 0.3 | 0.1.0 is 220 use cases, 0.3 is 220; the total stays 959; the catalog rows of both say "release 0.3 by owner decision 2026-11-03" |
| 2026-11-03 | `PLT-G08`: when Redis is down, token revocation stays fail-closed and the answer is 503 `auth.revocation_unavailable` (retryable), not 500 | No change of scope; the use case stays in 0.1.0 |

# 6. How the work runs

- **Lanes.** One lane owns one capability group and ships one pull request (DEL-006); at most 3 run in parallel (DEL-007). Lanes below name the group, not the engineer.
- **Capacity.** Assumed 10 to 15 use cases per lane per day for use cases that are already HARDENING. DEFINED and IMPLEMENTING use cases take longer; 0.3 and 1.0 hold the most of them. Re-measure after 0.1.0 and replace this assumption with the measured number.

## 6.1 Release 0.1.0 in three waves

| Wave | Lane | Use cases | Still to do |
|---|---|---|---:|
| W1 | Identity rest and Workspaces rest | 58 (`IDN` 30, `WSP` 28) | 24 |
| W1 | Work Management board lifecycle (`WM-A`) | 21 | 20 |
| W1 | Foundation: Governance, Platform, Billing | 28 | 28 |
| W2 | Work Management items (`WM-B`) | 32 | 31 |
| W2 | Fields and values (`WM-C`, `WM-D`) | 27 | 27 |
| W2 | Views, filter, table (`WM-H`, `WM-I`) | 22 | 22 |
| W3 | Groups, ordering, checklists, Kanban, Calendar, Timeline (`WM-E`, `F`, `G`, `J`, `K`, `L`) | 20 | 20 |
| W3 | Intake, events, realtime, performance (`WM-N`, `O`, `P`, `T`) | 12 | 12 |
| W3 | CI e2e and docs-check wiring (section 7), then R2 and R3 | | |

The foundation lane runs in W1 so that every later wave builds on authorization, tenant and realtime behaviour that is already proven. Identity has only 8 use cases left, so it shares a lane with Workspaces. Sequential critical path: about 5 to 8 working days.

## 6.2 Later releases, lane groups

| Release | Lanes (each one pull request) | Waves |
|---|---|---|
| 0.2 | Pages and hierarchy (`DCT-A` to `C`); blocks and editor (`DCT-D` to `H`); permissions, versions, realtime, recovery (`DCT-I` to `L`); comments (`COL-A` to `D`, `WM-R`); Inbox, realtime and hardening (`COL-E` to `H`, `PLT-G05`) | 2 |
| 0.3 | Accounts; Identity rest; Teams and Spaces (`WSP-K`, `L`); onboarding, Home, settings (`WSP-J`, `M`, `N`); members and invitations rest (`WSP-A` to `I`); Governance roles and sharing (`GOV-C`, `D`, `G`); Governance evaluation, admin UI, audit (`GOV-A`, `B`, `E`, `F`, `H`, `I`); Work Management rest; operator console | 3 |
| 0.4 | Automation model and execution (`AUT-A` to `J`); Automation UI and hardening (`AUT-K` to `N`); Integrations connections and OAuth (`INT-A` to `E`); Integrations operations and UI (`INT-F` to `N`); Platform messaging; `WM-Q`, `WM-B32` | 2 |
| 1.0 | Billing core (`BIL-A` to `J`); Billing provider and operations (`BIL-K` to `X`); Analytics foundation (`ANA-A` to `G`); Analytics reports and UI (`ANA-H` to `X`); Platform rest; release engineering | 3 |

## 6.3 Provisional windows

| Release | Window | Basis |
|---|---|---|
| 0.1.0 | about 1 week | 125 use cases to do, 122 of the 220 already HARDENING |
| 0.2 | 3 to 4 weeks | Mostly INTEGRATING and DEFINED |
| 0.3 | 4 to 6 weeks | The most DEFINED and IMPLEMENTING use cases; owner decisions |
| 0.4 | 3 to 5 weeks | Automation and Integrations are still IMPLEMENTING |
| 1.0 | 4 to 6 weeks | Largest scope; real payment provider |

These windows are provisional. Replace them with measured throughput after 0.1.0.

# 7. Quality investments

| Investment | Release | Needs |
|---|---|---|
| Run mock and fidelity e2e in CI; run real e2e nightly | 0.1.0, before R3 | CI minutes and a runner for the real stack |
| Run `make docs-check` in CI | 0.1.0 | One CI job; today it runs only locally |
| Reactivate staging delivery so the owner can accept a candidate there | 0.1.0 R3 | Owner infrastructure or credentials; `ci.yml` must again produce the `release-candidate` input |
| Close the CI drift that remains: `tools/deliveryctl` guards written for six workflows; the required-proof TRX check not wired into CI | 0.1.0 | Code change in `tools/deliveryctl` and `ci.yml`; docs now describe the single `ci.yml` and `CI gate` |

Until staging exists, the candidate runs from the tagged commit on a stack the owner can reach (DEL-009).

# 8. Risks and assumptions

| # | Risk or assumption | Effect | Mitigation |
|---|---|---|---|
| 1 | The 10 to 15 per lane per day rate is unproven | Windows slip | Measure in 0.1.0; DEL-008 proposes a scope cut when 15 use cases behind |
| 2 | 0.1.0 foundations are not yet accepted | Late defects in 0.1.0 | Foundation lane first (W1) |
| 3 | No e2e in CI | Regressions between merges | Quality investments 1 and 2 |
| 4 | No staging | Owner accepts on a local stack | Quality investment 3 |
| 5 | Open owner decisions (section 5) | 0.3 holds the most blocked use cases | Owner decides at R0; DEL-010 moves what stays blocked |
| 6 | Default entitlement hides Billing integration until 1.0 | A large late integration risk | 0.4 journeys run on the default; 1.0 journeys 1 to 4 run the real contract |
| 7 | 1.0 is the largest release | Overrun | Owner may cut scope at R0 |
| 8 | Platform use cases in 1.0 are exercised earlier (section 3.4) | Foundation defects seen late | Section 3.3 moved the ones later releases need; owner may move more |
| 9 | Missing artboards for DEFINED use cases | Design lines (DEL-003) cost more | The owner draws or accepts the screenshot review |
| 10 | Shared files and one heavy-run lock | Throughput ceiling below 3 lanes | Lead orders lanes (DEL-007) |
| 11 | Assumption: one lead, at most 3 lanes, owner free for R4 within two working days | | |
| 12 | Catalog states move daily | Counts here age | Re-take at each R0 |

# 9. From the former backend roadmap

[`backend-roadmap.md`](backend-roadmap.md) (priorities P0 to P6) is superseded by this document. What stays valid and is used here:

| Kept | Where it lives now |
|---|---|
| Order Identity and Accounts, then Workspaces and Governance, then Work Management, then Documents, Collaboration and Billing, then Automation and Integrations, then Analytics | Section 1 and the release order |
| Gates: Workspaces and Governance before Work Management; Documents may start from Workspaces and Governance; Billing from Accounts; Automation per trigger from source events and message delivery; Analytics per metric from source contracts | Section 4 dependencies |
| Platform is an enabling lane, not a serial phase; messaging, idempotency and realtime recovery are due before the contexts that need them | Section 3.3 |
| No service extraction during normal feature sequencing | [`capability-extraction-strategy.md`](../architecture/capability-extraction-strategy.md) |

What it no longer is: a delivery plan with its own phase exits and certification summaries. Those are replaced by the release exits of section 4 and DEL-014.
