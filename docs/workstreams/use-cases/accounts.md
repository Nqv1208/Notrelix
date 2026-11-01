---
document_id: WRK-UC-ACCOUNTS
document_type: workstream-use-case-catalog
status: active
owner: identity-accounts-team
applies_to:
  - accounts
  - use-case-catalog
  - capability-acceptance
evidence:
  - docs/workstreams/use-cases/README.md
  - docs/product/accounts.md
  - docs/workstreams/teams/identity-accounts.md
  - docs/workstreams/executions/identity-accounts/identity-accounts.spec.md
  - docs/workstreams/executions/identity-accounts/identity-accounts.tests.md
  - docs/workstreams/executions/identity-accounts/identity-accounts.certification.md
  - backend/src/Notrelix.API/Endpoints/Accounts/
  - backend/src/Notrelix.Application/Features/Accounts/
  - backend/src/Notrelix.Domain/Accounts/
  - frontend/packages/features/account/
review_on:
  - account-lifecycle-change
  - account-membership-change
  - account-domain-or-identity-provider-change
  - scim-or-provisioning-change
  - region-or-data-residency-change
  - capability-state-change
---

# Use cases: Accounts

Product meaning: [accounts](../../product/accounts.md). Team: [identity-accounts](../teams/identity-accounts.md). Execution pack: [spec](../executions/identity-accounts/identity-accounts.spec.md), [tests](../executions/identity-accounts/identity-accounts.tests.md), [certification](../executions/identity-accounts/identity-accounts.certification.md). Permission and role meaning: [governance](../../product/governance.md). Sign-in, session, MFA, profile and the personal "My account" pages are Identity use cases: see [identity.md](identity.md). This file covers the Account (organization) boundary only.

Requirement ids cited below are `IAREQ024` to `IAREQ036` (Account capability, current Account, tenant isolation) and `CORE-CERT-004`, `CORE-CERT-005` of the pack. The test plan ids are `IA-TST-ACCOUNT-*`.

# 1. Assessment

## 1.1 Capability map

Letters follow the Accounts list in the team spec (section 4).

| Letter | Capability | Use cases | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | DONE | BLOCKED |
|---|---|---|---|---|---|---|---|---|
| A | Account create | 3 | 2 | 0 | 1 | 0 | 0 | 0 |
| B | Account read | 3 | 3 | 0 | 0 | 0 | 0 | 0 |
| C | Account update | 2 | 1 | 1 | 0 | 0 | 0 | 0 |
| D | Account lifecycle transition | 6 | 1 | 2 | 0 | 2 | 0 | 1 |
| E | Account-context resolution | 3 | 0 | 2 | 1 | 0 | 0 | 0 |
| F | Account switch / transition behavior | 2 | 1 | 1 | 0 | 0 | 0 | 0 |
| G | Account-scoped frontend state isolation | 3 | 1 | 2 | 0 | 0 | 0 | 0 |
| H | Account-level administration integration | 20 | 6 | 14 | 0 | 0 | 0 | 0 |
| | Total | 42 | 15 | 22 | 2 | 2 | 0 | 1 |

No use case is DONE: none has an owner check or a 1-px design diff. HARDENING is given only to D01 and D02, which `frontend/e2e/real/operator-suspension.real.e2e.spec.ts` runs on the real stack through the operator console (suspend, a refused tenant write with `account.suspended`, reads still open, lift, a write accepted again). No other `frontend/e2e/real` spec exercises an Account use case (`email-confirmation.real.e2e.spec.ts` covers registration, which creates the personal Account as a side effect).

How "IMPLEMENTING" is used here: for lifecycle, member, invitation, domain, IdP, SCIM, region and route use cases the Domain aggregate and its tests exist, but no Application command, query or endpoint exists. They cannot run at all.

## 1.2 The ten most important gaps

1. The only Accounts API is `PUT /api/v1/accounts/rename`. There is no way to read an Account, list my Accounts, create a Team Account, or run any lifecycle, member, invitation, domain, SSO, SCIM, region or routing action.
2. The frontend has no Account administration surface and no Account rename screen. The design canvas has no organization-admin artboard (`Final-Account` and `Final-M-Account` are the personal pages, owned by Identity).
3. The frontend treats `User.id` as the Account id (`frontend/apps/web/src/providers/app-providers.tsx`, `frontend/packages/foundation/query/src/account-scope.ts`). The backend already returns the real `accountId` in the bootstrap result. Cache isolation is therefore proven against the wrong key.
4. Server-side current-Account choice at bootstrap is not deterministic: it takes `FirstOrDefault()` over memberships without an order (`backend/src/Notrelix.Infrastructure/CrossContext/Identity/Bootstrap/IdentityBootstrapReadAdapter.cs`).
5. No competing-write protection on Account changes: `RenameAccountCommand` carries no expected version (`ACC-020`).
6. Account slug is not unique: `idx_account_slug` is a plain index (`backend/src/Notrelix.Infrastructure/Data/Configurations/Account/AccountConfiguration.cs`), and personal Account slugs come from the display name with no collision handling.
7. Suspension and closure have no enforcement point. A suspended Account can still be renamed, and no other context reacts to the fact (`ACC-005`, `ACC-019`).
8. Owner and last-owner rules exist only as pure Domain helpers (`AccountOwnerRules`). No handler serializes concurrent owner changes, as the Workspaces context does with its owner lock.
9. No audit trail of Account administration is wired or readable (product doc section 84).
10. No cross-tenant negative test exists for Account use cases other than RLS conformance (`backend/tests/Notrelix.Integration.Tests/Rls/AppRoleConformanceTests.cs`); no real end-to-end spec covers Accounts.

## 1.3 Defects in shipped code

| Defect | Where |
|---|---|
| Missing expected version on a competing write | `backend/src/Notrelix.Application/Features/Accounts/Accounts/Commands/RenameAccount/RenameAccountCommand.cs`, `backend/src/Notrelix.API/Contracts/Accounts/Requests/RenameAccountRequest.cs` |
| Rename is allowed on a Suspended Account (only Closed is blocked) | `backend/src/Notrelix.Domain/Accounts/Accounts/Account.cs` |
| Non-unique slug; no collision handling on generation | `AccountConfiguration.cs` (above), `backend/src/Notrelix.Application/Features/Accounts/Provisioning/AccountProvisioningService.cs` |
| Non-deterministic Account choice at bootstrap | `IdentityBootstrapReadAdapter.cs` (above) |
| Dead code: lifecycle, plan code, region, member, invitation, domain, IdP, SCIM, route aggregates have no caller | `backend/src/Notrelix.Domain/Accounts/**` (only `Rename`, `Create`, `AccountMember.Create` are used) |
| `Trialing` is treated as admissible for new members, a Billing coupling the product doc calls a smell | `backend/src/Notrelix.Application/Features/Accounts/Members/AccountMembershipActions.cs` |
| Free-text statuses where a closed set is needed: IdP `Status`, `ScimDirectory.Status`, `ScimSyncRun.Status`, `AccountRegion.DataResidencyMode` and `MigrationStatus` | `backend/src/Notrelix.Domain/Accounts/IdentityProviders/`, `Scim/`, `Regions/` |
| Frontend Account id is `User.id` (temporary model still live) | `app-providers.tsx`, `account-scope.ts` |
| `AccountRoleBaseline` gives `BillingAdmin` and `SecurityAdmin` no permission at all; the roles exist but are inert | `backend/src/Notrelix.Application/Features/Governance/Authorization/WorkspaceRoleBaseline.cs` |
| No `GET` for an Account: the client cannot show the Account name it can rename | `backend/src/Notrelix.API/Endpoints/Accounts/` |

Not found as defects: rename is authorized through `ManageAccount` in the pipeline and is idempotency-partitioned by Account (`IAccountRequest`); no mock-only Account path exists because no Account screen exists.

## 1.4 Enterprise baseline

Compared products and the points taken from them:

- Slack Enterprise Grid: one Org Primary Owner who alone transfers ownership; Org Owners; Org Admins; SSO configuration is Owner-only. Sources: [Types of roles in Slack](https://slack.com/help/articles/201912948), [Understand the Primary Owner role](https://slack.com/intl/en-gb/help/articles/360038161033-Understand-the-Primary-Owner-role).
- Atlassian organization (Atlassian Guard): verify a domain by DNS TXT, HTTPS file or IdP sync, then claim matching accounts as managed accounts; review before claiming. Source: [Verify a domain to manage accounts](https://support.atlassian.com/user-management/docs/verify-a-domain-to-manage-accounts).
- GitHub Enterprise: enterprise owners see all admins and members, verify domains, and read an audit log of administrative events with a retention period. Source: [Accessing the audit log for your enterprise](https://docs.github.com/en/enterprise-server@3.13/admin/monitoring-activity-in-your-enterprise/reviewing-audit-logs-for-your-enterprise/accessing-the-audit-log-for-your-enterprise).

| Table-stakes feature | Product doc | This catalog |
|---|---|---|
| Single accountable owner and explicit ownership transfer | Mentioned (sections 25, 26) | H09 (enterprise baseline) |
| List my organizations and switch | Implied (section 88) | B02, F01 (enterprise baseline) |
| Domain proof by DNS record, one claimant per domain | Principle only (sections 31 to 34, 77) | H10 (enterprise baseline), Part 5 rule |
| Member directory with roles and status | Section 20 | H02 |
| Administrative audit log readable by owners | Principle only (section 84) | H19 (enterprise baseline) |
| Legal name for invoices and contracts | Field exists (section 7) | C02 (enterprise baseline) |
| Self-serve creation of a second team organization | Journey only (section 67) | A02 (enterprise baseline) |
| Closed organization is read-only and reopenable | Concept only (section 16) | D03, D04 |

Not matched on purpose: org-wide 2FA enforcement and session policy (Identity and Governance own them), account deletion and data export (owner decision, see Part 4).

# 2. Index

Capability letters: A Account create, B Account read, C Account update, D Account lifecycle transition, E Account-context resolution, F Account switch / transition behavior, G Account-scoped frontend state isolation, H Account-level administration integration.

Column values: BE and FE say `yes`, `partial`, `domain only` (aggregate and tests, no command or endpoint) or `no`. Design is `none` when the canvas has no artboard. Real e2e is `yes` for D01 and D02 (`operator-suspension.real.e2e.spec.ts`), `partial` for A01 and `none` for every other row.

| ID | Use case | Capability | Actors | BE | FE | Design | Real e2e | State | Gap |
|---|---|---|---|---|---|---|---|---|---|
| UC-ACC-A01 | Provision the personal Account at registration | A | New user (system action) | yes | n/a | none | partial | INTEGRATING | No Account assertion in a real spec; slug collision |
| UC-ACC-A02 | Create an additional Team Account (enterprise baseline) | A | Verified user | no | no | none | none | DEFINED | Command, endpoint, screen |
| UC-ACC-A03 | Reject a duplicate Account identifier | A | Any creator | partial | no | none | none | DEFINED | Unique index, distinct error |
| UC-ACC-B01 | Read the current Account | B | Account member | no | no | none | none | DEFINED | Query, endpoint, screen |
| UC-ACC-B02 | List my Accounts (enterprise baseline) | B | Signed-in user | partial | no | none | none | DEFINED | Query returns one id only |
| UC-ACC-B03 | See the derived plan label | B | Account member | domain only | no | none | none | DEFINED | Billing fact, no PlanCode writer |
| UC-ACC-C01 | Rename the Account | C | Account Owner, Admin | yes | no | none | none | IMPLEMENTING | Expected version, UI, suspended rule |
| UC-ACC-C02 | Set the legal name (enterprise baseline) | C | Account Owner, Admin | partial | no | none | none | DEFINED | Update operation, command |
| UC-ACC-D01 | Suspend an Account | D | Platform operator | yes | yes | none | yes | HARDENING | Reactions of other contexts (D06), tenant banner, integrations in the impact preview |
| UC-ACC-D02 | Lift a suspension | D | Platform operator | yes | yes | none | yes | HARDENING | Same as D01 |
| UC-ACC-D03 | Close an Account | D | Account Owner | domain only | no | none | none | IMPLEMENTING | Command, impact preview, reactions |
| UC-ACC-D04 | Reopen a closed Account | D | Account Owner | domain only | no | none | none | IMPLEMENTING | Command, reactions |
| UC-ACC-D05 | Delete and restore an Account | D | Account Owner | domain only | no | none | none | BLOCKED | Owner deferred deletion |
| UC-ACC-D06 | Other contexts react to suspension and closure | D | System | no | no | none | none | DEFINED | Consumers in four contexts |
| UC-ACC-E01 | Resolve the current Account for a request | E | Any signed-in actor | yes | partial | none | none | INTEGRATING | Non-deterministic bootstrap pick; FE uses User.id |
| UC-ACC-E02 | Deny access to another Account identically | E | Any actor | yes | no | none | none | IMPLEMENTING | FE denied state; cross-tenant test per use case |
| UC-ACC-E03 | Carry Account context in background work | E | Worker | partial | n/a | none | none | IMPLEMENTING | Per-consumer proof |
| UC-ACC-F01 | Switch the active Account | F | Multi-Account user | no | no | none | none | DEFINED | List, switcher, reset |
| UC-ACC-F02 | Reset Account scope when the principal changes | F | Signed-in user | n/a | yes | none | none | IMPLEMENTING | Real Account id; real e2e |
| UC-ACC-G01 | Account id in every account-scoped query key | G | Frontend | n/a | yes | none | none | IMPLEMENTING | Real Account id; coverage of all packages |
| UC-ACC-G02 | A late response from the old Account is discarded | G | Frontend | n/a | yes | none | none | IMPLEMENTING | Real Account id |
| UC-ACC-G03 | Realtime and local state reset on Account change | G | Frontend | n/a | no | none | none | DEFINED | Subscription teardown rule |
| UC-ACC-H01 | Open Account administration with a scope indicator | H | Owner, Admin, SecurityAdmin | no | no | none | none | DEFINED | Shell, route, artboards |
| UC-ACC-H02 | List Account members | H | Owner, Admin | no | no | none | none | DEFINED | Paged query, screen |
| UC-ACC-H03 | Invite a person at Account scope | H | Owner, Admin | domain only | no | none | none | IMPLEMENTING | Command, email, screen |
| UC-ACC-H04 | Accept an Account invitation | H | Invited person | domain only | no | none | none | IMPLEMENTING | Command, token page |
| UC-ACC-H05 | Join the Account through a Workspace invitation | H | Invited person | yes | partial | none | none | IMPLEMENTING | Owned by Workspaces flow; no Account view |
| UC-ACC-H06 | Revoke or expire an invitation | H | Owner, Admin, system | domain only | no | none | none | IMPLEMENTING | Command, expiry job |
| UC-ACC-H07 | Change a member's Account role | H | Owner, Admin | domain only | no | none | none | IMPLEMENTING | Command, lock, screen |
| UC-ACC-H08 | Suspend, reactivate or remove a member | H | Owner, Admin | domain only | no | none | none | IMPLEMENTING | Command, lock, downstream reactions |
| UC-ACC-H09 | Transfer Account ownership (enterprise baseline) | H | Account Owner | no | no | none | none | DEFINED | Operation, step-up, screen |
| UC-ACC-H10 | Add and verify an organization domain (enterprise baseline) | H | Owner, SecurityAdmin | domain only | no | none | none | IMPLEMENTING | Proof mechanism, command, screen |
| UC-ACC-H11 | Publish the primary verified domain to consumers | H | Workspaces (consumer) | yes | n/a | none | none | IMPLEMENTING | No producer test file; no domain writer yet |
| UC-ACC-H12 | Turn domain auto-join on or off | H | Owner, SecurityAdmin | domain only | no | none | none | IMPLEMENTING | Command, screen |
| UC-ACC-H13 | Configure the Account identity provider | H | Owner, SecurityAdmin | domain only | no | none | none | IMPLEMENTING | Command, validation state, Identity link |
| UC-ACC-H14 | Route sign-in to the Account IdP by domain | H | Identity (consumer) | no | no | none | none | DEFINED | Public fact, deterministic rule |
| UC-ACC-H15 | Configure a SCIM directory and its token | H | Owner, SecurityAdmin | domain only | no | none | none | IMPLEMENTING | Command, token shown once |
| UC-ACC-H16 | Run a SCIM sync and deprovision people | H | System | domain only | no | none | none | IMPLEMENTING | Worker, per-owner calls, status |
| UC-ACC-H17 | Set the default data region | H | Owner, Admin | domain only | no | none | none | IMPLEMENTING | Region list, command, screen |
| UC-ACC-H18 | Manage Account-to-Workspace routes | H | Owner, Admin | domain only | no | none | none | IMPLEMENTING | Command, screen, Workspaces link |
| UC-ACC-H19 | Read the Account administration audit trail (enterprise baseline) | H | Owner, SecurityAdmin | no | no | none | none | DEFINED | Audit writes, query, screen |
| UC-ACC-H20 | Show "not entitled" apart from "not authorized" | H | Any admin | no | no | none | none | DEFINED | Billing fact, UI states |

Endpoints of this context: `PUT /api/v1/accounts/rename` (C01). Endpoints with no use case: none. Account-scoped endpoints owned by Workspaces (`/api/v1/accounts/{accountId}/workspaces`, `/slug-check`, `/resolve`) are cited as evidence in E01 and are catalogued in `workspaces.md`.

# 3. Use cases

Shared evidence for the whole file: BE tests `backend/tests/Notrelix.Domain.Tests/Accounts/**`, `backend/tests/Notrelix.Application.Tests/Features/Accounts/**`, `backend/tests/Notrelix.Integration.Tests/Accounts/RenameAccountPipelineAuthorizationTests.cs`, `backend/tests/Notrelix.Integration.Tests/Rls/AppRoleConformanceTests.cs`. Design: the canvas holds no organization-admin artboard; `Final-Account` and `Final-M-Account` are personal pages (Identity). Real e2e: none.

Unless a block says otherwise, every Account admin write follows the Part 5 rules: Account scope from the trusted context (never the body), server-side authorization, an `expectedVersion` on the Account row, a stale write changes nothing, a semantic no-op bumps no version and emits nothing, and an audit record on success. Every use case in group H is gated by the Account lifecycle rule R-ACC-LIFE (writes refused when the Account is Suspended or Closed, except reopen and lift).

## A. Account create

### UC-ACC-A01 Provision the personal Account at registration
- Capability: A Account create
- Actors and permission: new user; no permission (system action inside the registration request).
- Preconditions: registration input is valid (Identity rules, see [identity.md](identity.md)).
- Input: user id, display name, time. No Account input from the client.
- Output: one Account (type Personal, status Active, slug from display name) and one AccountMember with role Owner; the grant projection for the Owner is written in the same transaction as the User.
- Main flow: 1. Identity registration creates the User. 2. It calls the Accounts public action `IAccountProvisioningActions`. 3. Accounts creates the Account and Owner member and syncs the access grant. 4. The request transaction commits all of it.
- No-op / rejection / concurrency / retry: any failure rolls the whole registration back (no User without an Account). A retry with the same idempotency key has no second effect. A slug that already exists must not fail registration: generate a unique slug (see A03).
- UI states: none in Accounts; registration screens belong to Identity.
- Rules: [accounts](../../product/accounts.md) sections 67, ACC-001, ACC-007; IAREQ024, IAREQ025, IAREQ027; CORE-CERT-004; boundary exception BOUND-TX-004.
- Acceptance: Given a valid registration, When it commits, Then exactly one Personal Account and one Owner member exist for that user. Given the Accounts step throws, When the request ends, Then no User row exists. Given two users with the same display name, When both register, Then both succeed with different slugs.
- Evidence: BE `AccountProvisioningService`, `IAccountProvisioningActions` · FE registration flow (Identity) · Design none · Tests `AccountProvisioningServiceTests`, `RegisterTests`, `AccountTests` · Real e2e `email-confirmation.real.e2e.spec.ts` (registers; asserts no Account fact)
- State: INTEGRATING — slug collision case, an Account assertion in the real spec, and the owner check.

### UC-ACC-A02 Create an additional Team Account (enterprise baseline)
- Capability: A Account create
- Actors and permission: any signed-in user with a confirmed email; no Account permission exists yet, so the check is "authenticated and verified email" (Part 5, R-ACC-CREATE).
- Preconditions: user is signed in; email confirmed; the user has fewer than the allowed number of owned Accounts (Part 5).
- Input: name (1 to 160 characters), slug (optional, 3 to 63 characters; otherwise generated), legal name (optional, up to 200).
- Output: Account type Team, status Active; creator becomes the only Owner member; no Workspace and no Billing object is created (optional follow-up through their own contracts).
- Main flow: 1. User opens "Create organization". 2. Enters the name. 3. Server creates Account and Owner member in one transaction. 4. Client receives the Account id and may switch to it (F01).
- No-op / rejection / concurrency / retry: invalid name returns a validation error; duplicate slug returns a slug conflict (A03); unconfirmed email returns permission denied. A retry with the same idempotency key returns the first Account and creates no second one.
- UI states: loading, validation error, slug conflict, permission denied (unconfirmed email), pending, offline.
- Rules: accounts section 67, ACC-001, ACC-002; IAREQ025, IAREQ027.
- Acceptance: Given a verified user, When they create "Acme", Then the Account and an Owner member exist and the user can read it (B01). Given the same idempotency key twice, When sent, Then one Account exists.
- Evidence: BE `Account.Create` (no command) · FE none · Design none · Tests `AccountTests` (Create cases) · Real e2e none
- State: DEFINED — command, endpoint (`POST /api/v1/accounts`), UI, artboard.

### UC-ACC-A03 Reject a duplicate Account identifier
- Capability: A Account create
- Actors and permission: creator of an Account (A01, A02).
- Preconditions: a slug is requested or generated.
- Input: slug.
- Output: unique slug stored; or a conflict error that names the slug as the conflicting field (not the display name or legal name).
- Main flow: 1. Normalize slug. 2. Check uniqueness inside the transaction. 3. Store, or return conflict. For generated slugs, append a short suffix until unique.
- No-op / rejection / concurrency / retry: two simultaneous creates of the same slug: one wins by the unique index, the other gets the conflict. Display name and legal name may repeat.
- UI states: validation error (slug field), pending.
- Rules: accounts section 74; R-ACC-SLUG (Part 5).
- Acceptance: Given slug "acme" exists, When another Account asks for "acme", Then it fails with a slug conflict and no row is added. Given a concurrent pair, When both commit, Then exactly one row exists.
- Evidence: BE `AccountConfiguration` (non-unique `idx_account_slug`), `Slug.Create` · FE none · Design none · Tests `AccountTests` (slug format only) · Real e2e none
- State: DEFINED — unique index and migration, conflict error code, concurrent-create test.

## B. Account read

### UC-ACC-B01 Read the current Account
- Capability: B Account read
- Actors and permission: any active Account member; `ViewWorkspace`-level baseline for members (Account role baseline).
- Preconditions: current Account resolved (E01).
- Input: none (Account from context).
- Output: id, name, slug, legal name, type, status, default region code, my role, version.
- Main flow: 1. Client calls `GET /api/v1/accounts/current`. 2. Server resolves and authorizes. 3. Returns the Account.
- No-op / rejection / concurrency / retry: non-member and unknown Account both get the identical NotFound. Reads are safe to retry.
- UI states: loading, success, permission denied (not found), error. The client must never invent a name from the user's name.
- Rules: IAREQ025, IAREQ031, IAREQ033; ACC-002; accounts section 7.
- Acceptance: Given a member, When they read, Then they see their Account and role. Given a user of Account B, When they ask for Account A's id, Then the response equals the response for a random id.
- Evidence: BE none (bootstrap returns `accountId` only) · FE none · Design none · Tests `GetBootstrapTests` (accountId) · Real e2e none
- State: DEFINED — query, endpoint, FE query hook.

### UC-ACC-B02 List my Accounts (enterprise baseline)
- Capability: B Account read
- Actors and permission: signed-in user, own memberships only.
- Preconditions: signed in.
- Input: none.
- Output: list of Accounts where the user has an Active membership: id, name, type, status, my role.
- Main flow: 1. Client calls `GET /api/v1/accounts`. 2. Server lists active memberships. 3. Client feeds the switcher (F01).
- No-op / rejection / concurrency / retry: Suspended memberships are listed as unavailable, Removed ones are not listed. Bounded read: at most 100 rows, with a cursor.
- UI states: loading, empty (cannot happen after A01), success, error.
- Rules: ACC-007, ACC-008; IAREQ028, IAREQ036.
- Acceptance: Given a user in two Accounts, When they list, Then both appear with their roles and no other Account appears.
- Evidence: BE `IdentityBootstrapReadAdapter` (reads memberships, returns one id) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — query and endpoint, switcher design.

### UC-ACC-B03 See the derived plan label
- Capability: B Account read
- Actors and permission: Account member.
- Preconditions: Billing context exposes a plan fact for the Account.
- Input: none.
- Output: plan name shown read-only, read from Billing; never from `Account.PlanCode`.
- Main flow: 1. Admin screen asks Billing for the Account's plan label. 2. Shows it next to the Account name.
- No-op / rejection / concurrency / retry: if Billing has no fact, show nothing. Accounts exposes no command that writes `PlanCode`.
- UI states: loading, success, empty.
- Rules: ACC-004, ACC-016; accounts sections 51, 52, 94, 99; Part 5 R-ACC-PLAN.
- Acceptance: Given Billing changes the plan, When the screen reloads, Then it shows the new label and the Account row is unchanged.
- Evidence: BE `Account.UpdatePlanCode` (no caller) · FE none · Design none · Tests `AccountTests` (plan code) · Real e2e none
- State: DEFINED — Billing read contract (see [billing.md](billing.md)); decide removal of `PlanCode` (Part 4).

## C. Account update

### UC-ACC-C01 Rename the Account
- Capability: C Account update
- Actors and permission: Account Owner and Admin; `ManageAccount`. Workspace roles do not apply.
- Preconditions: Account is not Closed (and, by Part 5, not Suspended); Account resolved from context.
- Input: `name` (required, trimmed, 1 to 160 characters); `expectedVersion` (to add).
- Output: name changed, version +1, `AccountRenamed` fact, audit record. Slug does not change.
- Main flow: 1. Admin opens Account settings and edits the name. 2. `PUT /api/v1/accounts/rename`. 3. Pipeline authorizes `ManageAccount`. 4. Domain renames, pipeline commits. 5. 204 returned; screen shows the new name.
- No-op / rejection / concurrency / retry: same name after trim is a no-op: 204, no version, no event. Member role gets Forbidden with the Account left unchanged. Closed Account: business-rule error `Accounts_Account_CannotRenameClosed`. Stale `expectedVersion`: 409 conflict, nothing changes (to build). Same idempotency key: one effect.
- UI states: loading, validation error, permission denied, read-only (Member), conflict, pending, offline, success.
- Rules: accounts sections 55, 56, 81, 82, ACC-017, ACC-020; IAREQ024.
- Acceptance: Given an Admin, When they rename, Then the name changes and one event is raised. Given a Member, When they rename, Then Forbidden and the name is unchanged. Given the same name, When sent, Then no version bump. Given a stale version, When sent, Then 409 and no change. Given a user of Account B, When they rename Account A, Then NotFound, never a write.
- Evidence: BE `PUT /api/v1/accounts/rename` `RenameAccountEndpoint`, `RenameAccountCommandHandler`, `RenameAccountCommandValidator`, `AccountRoleBaseline` · FE none (generated client has the path) · Design none · Tests `RenameAccountCommandHandlerTests`, `RenameAccountPipelineAuthorizationTests`, `AccountTests` (Rename cases) · Real e2e none
- State: IMPLEMENTING — expected version, Suspended rule, screen and artboard, real e2e.

### UC-ACC-C02 Set the legal name (enterprise baseline)
- Capability: C Account update
- Actors and permission: Account Owner and Admin; `ManageAccount`.
- Preconditions: Account Active.
- Input: legal name (optional, up to 200 characters, empty clears it); `expectedVersion`.
- Output: legal name changed, version +1, `AccountLegalNameChanged` fact (new), audit record.
- Main flow: 1. Admin edits "Legal name" in Account settings. 2. `PUT /api/v1/accounts/legal-name`. 3. Saved.
- No-op / rejection / concurrency / retry: same value is a no-op. Stale version gives 409. Legal name carries no security or commercial meaning (ACC section 7).
- UI states: validation error, permission denied, read-only, conflict, pending, success.
- Rules: accounts section 7; ACC-017, ACC-020.
- Acceptance: Given an Admin, When they set "Acme Inc.", Then B01 returns it. Given the same value, When sent, Then no version bump.
- Evidence: BE `Account.Create(legalName)` only; no update operation · FE none · Design none · Tests `AccountTests` (legal name on create) · Real e2e none
- State: DEFINED — domain operation, command, endpoint, UI.

## D. Account lifecycle transition

### UC-ACC-D01 Suspend an Account
- Capability: D Account lifecycle transition
- Actors and permission: the platform operator (owner decision 2026-10-31): a separate operator account with its own sign-in scheme and database login, never a tenant user (`backend/docs/architecture/security-tenancy-authorization.md` section 12.4).
- Preconditions: Account exists and is not Deleted.
- Input: Account id, reason (required, up to 500 characters), `expectedVersion`.
- Output: status Suspended, version +1, `AccountSuspended` fact with the previous status and reason, audit record. No content is deleted.
- Main flow: 1. Operator selects the Account and sees the impact preview (members, Workspaces, integrations). 2. Confirms with the reason. 3. Accounts sets Suspended. 4. Other contexts react (D06).
- No-op / rejection / concurrency / retry: already Suspended is a no-op. Stale version gives 409.
- UI states: destructive confirmation (impact preview), conflict, pending; members of the Account see a suspended banner and read-only state.
- Rules: accounts sections 14, 15, 72, ACC-005, ACC-019; IAREQ026, IAREQ030.
- Acceptance: Given an Active Account, When an operator suspends it, Then status is Suspended, one event is raised and no data is deleted.
- Evidence: BE `POST /operator/v1/accounts/{accountId}/suspend` (`Operator.Account.Suspend`, `OperatorAccountService`), the `AccountSuspended` access fact and the write refusal in `AccessPolicyEngine` · FE `frontend/apps/operator` (tenant detail, impact preview with two counts, reason dialog) · Design none (the canvas has no operator artboard) · Tests Domain `AccountOperatorLifecycleTests` (+`OperatorAccountTests`, `PlanRevisionTests`: 20 in all), Application `OperatorAccountServiceTests` (10: success, each rejection, no-op without audit, stale version), Integration `OperatorConsoleIntegrationTests` (29 on a real Postgres with the operator login: suspend blocks a workspace-scoped and a resource-scoped tenant write and keeps reads, audit row with actor, reason, before, after and time), Architecture `AccountSuspensionWriteGateArchitectureTests` (every tenant-scoped write loads the facts) · Real e2e `frontend/e2e/real/operator-suspension.real.e2e.spec.ts` (3 real specs passed 2026-11-01)
- State: HARDENING — Other contexts do not react yet (D06: sessions, integrations, background and automation writes, anonymous form and webhook writes still run on a suspended Account); no suspended banner or read-only presentation in the tenant UI (the refusal shows as the generic error toast with the server message); the impact preview shows members and workspaces only; no design artboard exists for the console.

### UC-ACC-D02 Lift a suspension
- Capability: D Account lifecycle transition
- Actors and permission: the platform operator (same principal as D01).
- Preconditions: Account is Suspended.
- Input: Account id, `expectedVersion`.
- Output: status Active, version +1, `AccountActivated` fact, audit record.
- Main flow: 1. Operator opens the Account. 2. Lifts the suspension. 3. Other contexts resume (D06).
- No-op / rejection / concurrency / retry: already Active is a no-op. Closed Accounts are reopened only through D04.
- UI states: confirmation, conflict, pending.
- Rules: accounts sections 13, 14, 19; ACC-005.
- Acceptance: Given a Suspended Account, When lifted, Then status is Active and members can write again.
- Evidence: BE `POST /operator/v1/accounts/{accountId}/lift-suspension` (`Operator.Account.LiftSuspension`) · FE `frontend/apps/operator` tenant detail · Design none · Tests as D01 (a Closed Account can be neither suspended nor reactivated: `AccountOperatorLifecycleTests`; lifting an Active one is a no-op) · Real e2e `frontend/e2e/real/operator-suspension.real.e2e.spec.ts`
- State: HARDENING — same limits as D01.

### UC-ACC-D03 Close an Account
- Capability: D Account lifecycle transition
- Actors and permission: Account Owner only; action `ManageAccountLifecycle` (new, Owner only); requires recent step-up authentication (owner decision 48, A4 pattern).
- Preconditions: Account Active or Suspended.
- Input: confirmation text (the Account name), `expectedVersion`.
- Output: status Closed, version +1, `AccountArchived` fact, audit record. Data is retained. The Account and its Workspaces become read-only; no content is deleted.
- Main flow: 1. Owner opens Danger zone. 2. Sees the impact preview (members, Workspaces, integrations, billing). 3. Types the Account name and re-authenticates. 4. Server closes; other contexts react (D06).
- No-op / rejection / concurrency / retry: already Closed is a no-op. Missing step-up gives a step-up-required error. Stale version gives 409. Last-owner rules do not block closing.
- UI states: destructive confirmation, validation error, permission denied, conflict, pending, read-only banner afterwards.
- Rules: accounts sections 16, 72, 73, ACC-005, ACC-006, ACC-019; IAREQ026, IAREQ030; R-ACC-LIFE (Part 5).
- Acceptance: Given an Owner who re-authenticated, When they close the Account, Then status is Closed and writes in the Account fail. Given an Admin, When they try, Then Forbidden.
- Evidence: BE `Account.Archive` (no caller) · FE none · Design none · Tests `AccountTests` · Real e2e none
- State: IMPLEMENTING — command, endpoint, step-up, impact preview, reactions.

### UC-ACC-D04 Reopen a closed Account
- Capability: D Account lifecycle transition
- Actors and permission: Account Owner; `ManageAccountLifecycle`; step-up.
- Preconditions: Account is Closed and not Deleted. Reads stay possible for Owners while closed so they can reach this action.
- Input: `expectedVersion`.
- Output: status Active, version +1, `AccountActivated` fact, audit record. Workspaces and members resume as they were; provisioning and integrations stay paused until an admin re-enables them.
- Main flow: 1. Owner signs in; sees "This organization is closed". 2. Chooses Reopen and re-authenticates. 3. Server reopens; other contexts resume (D06).
- No-op / rejection / concurrency / retry: already Active is a no-op. Stale version gives 409.
- UI states: closed banner, confirmation, conflict, pending.
- Rules: accounts section 19; R-ACC-LIFE.
- Acceptance: Given a Closed Account, When its Owner reopens it, Then members can write again.
- Evidence: BE `Account.Activate` (accepts Closed; no caller) · FE none · Design none · Tests `AccountTests` · Real e2e none
- State: IMPLEMENTING — command, endpoint, closed-state screens, reactions.

### UC-ACC-D05 Delete and restore an Account
- Capability: D Account lifecycle transition
- Actors and permission: not decided.
- Preconditions: not decided.
- Input: not decided.
- Output: not decided.
- Main flow: not defined.
- No-op / rejection / concurrency / retry: the Domain has `Delete` and `Restore` (soft delete). Product doc sections 17, 18, 19 say soft delete does not define Account deletion.
- UI states: none; no control is shown.
- Rules: accounts ACC-006; sections 17, 83; owner decision 48 (A8).
- Acceptance: Given the current release, When an Owner looks for "Delete organization", Then no control exists and no endpoint exists.
- Evidence: BE `Account.Delete`, `Account.Restore` (no caller) · FE none · Design none · Tests `AccountTests` · Real e2e none
- State: BLOCKED — owner deferred Account deletion and data export (Part 4). Needs a legal and retention decision: retention period, what is purged, Billing and audit evidence, identity references.

### UC-ACC-D06 Other contexts react to suspension and closure
- Capability: D Account lifecycle transition
- Actors and permission: system (consumers in Workspaces, Identity, Integrations, Automation).
- Preconditions: Accounts committed a Suspended, Closed or reactivation fact.
- Input: fact with Account id, new status, time (no member lists, no secrets).
- Output: each owner applies its own reaction: Workspaces become read-only; Identity may limit sessions; Integrations and Automation pause; reactivation resumes them.
- Main flow: 1. Outbox publishes the fact after commit. 2. Each consumer claims the message once. 3. Each applies its policy.
- No-op / rejection / concurrency / retry: duplicate delivery has no second effect. A consumer failure retries and never rolls back the Account fact. Out-of-order facts are resolved by the version in the fact.
- UI states: banners in affected contexts (read-only, paused).
- Rules: accounts ACC-005, ACC-018, ACC-019; sections 64, 65; IAREQ030; IA-TST-ACCOUNT-INT-002.
- Acceptance: Given an Account is suspended, When consumers run, Then Workspaces reject writes and no aggregate is mutated by Accounts directly.
- Evidence: BE `AccountSuspendedDomainEvent`, `AccountArchivedDomainEvent` (raised, no mapper, no consumer) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — public fact contract, four consumers, tests.

## E. Account-context resolution

### UC-ACC-E01 Resolve the current Account for a request
- Capability: E Account-context resolution
- Actors and permission: any signed-in actor; resolution is separate from authorization.
- Preconditions: valid session.
- Input: Account id hint from the route (`/api/v1/accounts/{accountId}/...`) or the account header; never from the body.
- Output: the Account scope for the request, or a rejection.
- Main flow: 1. Middleware reads the hint. 2. Tenant context records it. 3. The pipeline checks membership (grant projection) before any handler runs. 4. RLS enforces the same boundary in the database.
- No-op / rejection / concurrency / retry: no hint on an Account-scoped endpoint gives a rejection. A hint for an Account the actor does not belong to gives the same NotFound as an unknown Account. Reads retry safely.
- UI states: loading (bootstrap), permission denied (NotFound page), offline.
- Rules: ACC-002, ACC-017; IAREQ031, IAREQ032, IAREQ033; CORE-CERT-005; IA-TST-ACCOUNT-APP-001.
- Acceptance: Given a member, When they call an Account-scoped endpoint with their Account id, Then it works. Given a body that names another Account id, When sent, Then the body value is ignored. Given a user with two memberships and no default, When bootstrap runs, Then the same Account is chosen every time.
- Evidence: BE `HttpRequestContextMiddleware`, `TryGetAccountIdHint`, `IAccountRequest`, `AccountScopedEndpointAttribute`, `IdentityBootstrapReadAdapter`; Workspaces endpoints `GET /api/v1/accounts/{accountId}/workspaces`, `POST /api/v1/accounts/{accountId}/workspaces`, `GET /api/v1/accounts/{accountId}/workspaces/slug-check` · FE `useAccountId`, bootstrap `accountId` unused · Design none · Tests `GetBootstrapTests`, `RenameAccountPipelineAuthorizationTests` · Real e2e none
- State: INTEGRATING — deterministic choice (order by default Workspace, then Owner role, then name), frontend using the real id, real e2e.

### UC-ACC-E02 Deny access to another Account identically
- Capability: E Account-context resolution
- Actors and permission: any actor.
- Preconditions: actor holds no active membership of the target Account.
- Input: any Account-scoped request naming that Account.
- Output: the identical NotFound as for an unknown id; no write; no existence leak in message, timing class or headers.
- Main flow: 1. Pipeline finds no grant. 2. Returns NotFound. 3. Even if a handler bug skipped it, RLS returns zero rows.
- No-op / rejection / concurrency / retry: Suspended and Removed members are denied the same way for writes; Removed also for reads.
- UI states: permission denied rendered as "not found".
- Rules: ACC-017; IAREQ033, IAREQ034; IA-TST-ACCOUNT-APP-002.
- Acceptance: Given Account A and Account B, When a user of B asks for A, Then status and body equal those for a random id. Given the same user, When a direct SQL query runs under B's context, Then zero rows of A appear.
- Evidence: BE pipeline authorization, RLS `007_policies_platform.sql` (`ops.has_account_access`, `ops.is_account_admin`) · FE none · Design none · Tests `RenameAccountPipelineAuthorizationTests`, `AppRoleConformanceTests` · Real e2e none
- State: IMPLEMENTING — cross-tenant negative test for each Account endpoint as it is added; frontend denied state.

### UC-ACC-E03 Carry Account context in background work
- Capability: E Account-context resolution
- Actors and permission: worker principal.
- Preconditions: a message or job carries an Account id.
- Input: Account id in the message envelope.
- Output: the worker acts inside that Account only.
- Main flow: 1. Consumer filter sets the tenant context from the envelope. 2. Handler runs under RLS.
- No-op / rejection / concurrency / retry: a message without an Account id fails to the dead-letter path; "null means all Accounts" and "first Account" are forbidden.
- UI states: none.
- Rules: IAREQ035; NRX-004, NRX-009.
- Acceptance: Given a message for Account A, When it runs, Then it cannot read or write Account B rows.
- Evidence: BE `TenantContextConsumeFilter` (Infrastructure messaging) · FE none · Design none · Tests `TenantContextConsumeFilterScopedEventTests`, `WorkerPrincipalTests` · Real e2e none
- State: IMPLEMENTING — an Accounts-specific consumer (D06, H16) must prove it.

## F. Account switch / transition behavior

### UC-ACC-F01 Switch the active Account
- Capability: F Account switch / transition behavior
- Actors and permission: signed-in user with two or more active memberships.
- Preconditions: B02 returned more than one Account.
- Input: chosen Account id.
- Output: the client's active Account changes; all Account-scoped server state is discarded; the next requests carry the new id. The server keeps no "current Account" session state.
- Main flow: 1. User opens the Account switcher. 2. Picks an Account. 3. Client clears Account-scoped cache and realtime (G01 to G03). 4. Loads B01 for the new Account. 5. Server authorizes each request by membership.
- No-op / rejection / concurrency / retry: choosing the active Account is a no-op. Choosing an Account the user lost access to returns NotFound and the client falls back to the previous Account. Switching twice quickly keeps only the last choice.
- UI states: loading, empty (single Account: no switcher), permission denied, offline.
- Rules: IAREQ036; accounts section 88; Part 5 R-ACC-SWITCH.
- Acceptance: Given Accounts A and B, When the user switches to B, Then no A data is visible and no A response lands in B state.
- Evidence: BE none · FE none (`prepareAccountTransition` exists) · Design none · Tests none · Real e2e none
- State: DEFINED — list query, switcher design, real id, e2e.

### UC-ACC-F02 Reset Account scope when the principal changes
- Capability: F Account switch / transition behavior
- Actors and permission: signed-in user; sign-out and sign-in belong to [identity.md](identity.md).
- Preconditions: auth state changes (sign-in, sign-out, session expiry, user change).
- Input: new Account id or null.
- Output: the cache is reset before children subscribe; no data of the earlier principal remains.
- Main flow: 1. `AccountScopeBridge` reads the id. 2. `lifecycle.prepareAccountTransition` runs in a layout effect. 3. Queries start under the new scope.
- No-op / rejection / concurrency / retry: same id again is a no-op.
- UI states: loading.
- Rules: IAREQ034, IAREQ036; FE-STATE-038, FE-STATE-039; team spec section 12.
- Acceptance: Given user 1 signs out and user 2 signs in, When pages load, Then no user 1 data is shown.
- Evidence: BE n/a · FE `frontend/apps/web/src/providers/app-providers.tsx`, `frontend/apps/web/src/composition/application-lifecycle.ts` · Design none · Tests `frontend/apps/web/src/composition/__tests__/account-isolation.integration.test.ts` · Real e2e none
- State: IMPLEMENTING — key must be the real Account id (today `User.id`); a real e2e.

## G. Account-scoped frontend state isolation

### UC-ACC-G01 Account id in every account-scoped query key
- Capability: G Account-scoped frontend state isolation
- Actors and permission: frontend code.
- Preconditions: an active Account id exists.
- Input: resource key parts.
- Output: keys shaped `["account", accountId, resource, ...]`.
- Main flow: 1. Hooks call `accountQueryKey` or `accountQueryKeys.*`. 2. The key guard rejects keys without scope.
- No-op / rejection / concurrency / retry: `useAccountId` throws if no Account is active.
- UI states: none.
- Rules: QRY-002; FE-STATE-038; IAREQ034.
- Acceptance: Given any account-scoped hook, When its key is built, Then position 1 is the Account id.
- Evidence: BE n/a · FE `frontend/packages/features/account/src/query/keys.ts`, `frontend/packages/foundation/query/src/query-key-scope.ts` · Design none · Tests `account-query-keys.unit.test.ts`, `query-key-scope.unit.test.ts` · Real e2e none
- State: IMPLEMENTING — real Account id; a repository-wide scan that every account-scoped key uses the helper.

### UC-ACC-G02 A late response from the old Account is discarded
- Capability: G Account-scoped frontend state isolation
- Actors and permission: frontend code.
- Preconditions: a request started under Account A and finished after the switch to B.
- Input: response of A.
- Output: it cannot populate B's state because its key is A-scoped.
- Main flow: 1. Cache reset cancels in-flight work. 2. Late data lands only under A's key, which is no longer observed.
- No-op / rejection / concurrency / retry: mutations in flight at switch time roll back optimistic patches.
- UI states: none visible.
- Rules: agent contract section 8.5; FE-STATE-039.
- Acceptance: Given a slow A query, When the user switches to B, Then B's screen shows only B data.
- Evidence: BE n/a · FE `account-scope.ts`, `application-lifecycle.ts` · Design none · Tests `account-isolation.integration.test.ts` · Real e2e none
- State: IMPLEMENTING — real Account id; real e2e with two Accounts.

### UC-ACC-G03 Realtime and local state reset on Account change
- Capability: G Account-scoped frontend state isolation
- Actors and permission: frontend code.
- Preconditions: a realtime subscription and local derived state exist for the old Account.
- Input: Account change.
- Output: subscriptions are closed and reopened for the new Account; route, selection and drafts of the old Account are cleared.
- Main flow: 1. Lifecycle closes the Account-scoped channels. 2. Clears local stores. 3. Opens channels for the new Account.
- No-op / rejection / concurrency / retry: an event from the old channel after the switch is dropped by scope check.
- UI states: loading, offline.
- Rules: agent contract sections 8.5, 8.6; IAREQ034.
- Acceptance: Given Account A events arrive after the switch, When handled, Then they change nothing in B.
- Evidence: BE n/a · FE none specific · Design none · Tests none · Real e2e none
- State: DEFINED — written reset list per store and channel, tests.

## H. Account-level administration integration

### UC-ACC-H01 Open Account administration with a scope indicator
- Capability: H Account-level administration integration
- Actors and permission: Account Owner, Admin, SecurityAdmin; members see a read-only subset.
- Preconditions: Account resolved (E01).
- Input: none.
- Output: an Account administration area, separate from Workspace settings, always showing the Account name and my role; sections hidden or disabled by role (server stays the authority).
- Main flow: 1. User opens "Organization" from the Account switcher or system rail. 2. Sees sections: General, Members, Domains, Single sign-on, Provisioning, Region, Workspaces, Audit, Danger zone.
- No-op / rejection / concurrency / retry: a plan-gated section shows "not entitled" (H20), not "no permission".
- UI states: loading, success, permission denied, read-only, offline.
- Rules: accounts sections 86, 87, 88, 101; ACC-017; DESIGN rules on scope indicators.
- Acceptance: Given an Admin, When they open the area, Then the Account name is visible on every section. Given a Member, When they open it, Then no write control is enabled.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — artboards (1440, 1024, 390; light, dark), route, nav entry.

### UC-ACC-H02 List Account members
- Capability: H Account-level administration integration
- Actors and permission: Owner, Admin (SecurityAdmin read).
- Preconditions: Account resolved.
- Input: paging cursor, search text, role and status filters.
- Output: members with display name, email, role, status, joined date; pending invitations in a separate tab.
- Main flow: 1. Admin opens Members. 2. Server returns a page (50 rows). 3. Admin filters.
- No-op / rejection / concurrency / retry: a Member role sees only the count. Removed members are excluded unless filtered. No unbounded read.
- UI states: loading, empty, success, permission denied, error.
- Rules: accounts sections 20, 22, 85; ACC-007.
- Acceptance: Given 120 members, When the page loads, Then at most 50 rows return with a cursor. Given a user of another Account, When asking, Then NotFound.
- Evidence: BE none (`AccountMember` aggregate, `IAccountDbContext.AccountMembers`) · FE none · Design none · Tests `AccountMemberTests` · Real e2e none
- State: DEFINED — query, endpoint, screen.

### UC-ACC-H03 Invite a person at Account scope
- Capability: H Account-level administration integration
- Actors and permission: Owner, Admin; `ManageAccount`. Only an Owner may invite another Owner.
- Preconditions: Account Active.
- Input: email, role (Admin, Member, BillingAdmin, SecurityAdmin; Owner only by an Owner), expiry (default 7 days, 1 to 30).
- Output: a Pending invitation and an email with a single-use token link; `AccountInvitationCreated` fact. No Workspace membership.
- Main flow: 1. Admin enters emails and roles. 2. Server creates invitations. 3. Email is sent after commit.
- No-op / rejection / concurrency / retry: an existing pending invitation for the same email returns that invitation (no duplicate); an existing Active member returns a clear "already a member"; expiry not positive is rejected (`Accounts_Invitation_ExpiryMustBePositive`). Resend is revoke plus new.
- UI states: validation error, permission denied, pending, success, conflict.
- Rules: accounts sections 27 to 30, ACC-009; IAREQ027.
- Acceptance: Given an Admin, When they invite "a@x.com", Then one Pending invitation exists and an email is queued. Given the same call again, When sent, Then no second invitation.
- Evidence: BE `AccountInvitation.Create` (no command) · FE none · Design none · Tests `AccountInvitationTests` · Real e2e none
- State: IMPLEMENTING — command, endpoint, email template, screen.

### UC-ACC-H04 Accept an Account invitation
- Capability: H Account-level administration integration
- Actors and permission: the invited person (identity resolved by the Identity workflow, see [identity.md](identity.md)).
- Preconditions: invitation Pending and not past `ExpiresAt`; the signed-in email equals the invited email.
- Input: invitation token.
- Output: invitation Accepted; an Active AccountMember with the invited role; `AccountInvitationAccepted` and `AccountMemberAdded` facts; grant projection updated; no Workspace membership.
- Main flow: 1. Person opens the link. 2. Signs in or registers. 3. Confirms. 4. Accounts accepts and adds the member in one transaction.
- No-op / rejection / concurrency / retry: accepting twice is a no-op for the same user; a revoked, expired or used token returns one generic "invitation is no longer valid"; a different email is refused. Two simultaneous accepts: one wins.
- UI states: loading, success, error (invalid link), permission denied.
- Rules: accounts ACC-009, section 30; `Accounts_Invitation_NotPending`, `Accounts_Invitation_HasExpired`.
- Acceptance: Given a Pending invitation, When accepted, Then the person is an Active member and sees no Workspace unless invited there. Given an expired token, When used, Then it fails and no member is created.
- Evidence: BE `AccountInvitation.Accept` (no command) · FE none · Design none · Tests `AccountInvitationTests` · Real e2e none
- State: IMPLEMENTING — anonymous token lookup scope in RLS, command, page.

### UC-ACC-H05 Join the Account through a Workspace invitation
- Capability: H Account-level administration integration
- Actors and permission: invited person; the Workspaces acceptance flow is the caller.
- Preconditions: Account is Active or Trialing (Trialing to be removed, Part 4).
- Input: account id, user id, inviter, time (from Workspaces).
- Output: an Active AccountMember with role Member, created once; the grant projection synced.
- Main flow: 1. Workspaces `AcceptInvitation` calls `IAccountMembershipActions.EnsureAccountMembershipAsync`. 2. Accounts checks admission and adds the member inside the caller's transaction (BOUND-TX-002).
- No-op / rejection / concurrency / retry: an already Active member is a no-op. A non-existent or non-admissible Account fails with `Accounts_Membership_AccountNotFound` or `Accounts_Membership_AccountNotAdmissible`. A Suspended or Removed member fails with `Accounts_Membership_NotActive`.
- UI states: error shown in the Workspace invitation page.
- Rules: ACC-007, ACC-009; BOUND-TX-002.
- Acceptance: Given a Workspace invitation, When accepted, Then the person holds Member role in the Account. Given a Closed Account, When accepted, Then it fails and no Workspace membership is created.
- Evidence: BE `AccountMembershipActions`, `IAccountMembershipFacts` · FE Workspaces invitation page (see `workspaces.md`) · Design none · Tests `AccountMembershipActionsTests`, `AccountMembershipFactsProviderTests` · Real e2e none
- State: IMPLEMENTING — an Account member view (H02) that shows the result; real e2e.

### UC-ACC-H06 Revoke or expire an invitation
- Capability: H Account-level administration integration
- Actors and permission: Owner, Admin (revoke); system job (expire).
- Preconditions: invitation Pending.
- Input: invitation id.
- Output: status Revoked or Expired; the token stops working at once.
- Main flow: 1. Admin selects Revoke. 2. Server sets Revoked. 3. A scheduled job marks past-expiry invitations Expired.
- No-op / rejection / concurrency / retry: revoking an already Revoked invitation is a no-op; an Accepted one cannot be revoked; an Expired one cannot be revoked (`Accounts_Invitation_HasExpired`).
- UI states: confirmation, conflict, success.
- Rules: accounts section 30.
- Acceptance: Given a Pending invitation, When revoked, Then acceptance fails with the generic invalid message.
- Evidence: BE `AccountInvitation.Revoke`, `Expire` (no caller) · FE none · Design none · Tests `AccountInvitationTests` · Real e2e none
- State: IMPLEMENTING — command, expiry job, screen.

### UC-ACC-H07 Change a member's Account role
- Capability: H Account-level administration integration
- Actors and permission: Owner (any role); Admin (Member, BillingAdmin, SecurityAdmin only); `ManageAccount`.
- Preconditions: target member Active.
- Input: member id, new role, `expectedVersion`.
- Output: role changed, `AccountMemberRoleChanged` fact, grant projection synced, audit record.
- Main flow: 1. Admin picks a member. 2. Selects a role. 3. Server locks the Account owner set, validates, saves.
- No-op / rejection / concurrency / retry: same role is a no-op. Demoting the last Owner fails with `Accounts_Owner_CannotDowngradeLastOwner`. Inactive member fails with `Accounts_Member_CannotChangeRoleOfInactive`. Concurrent demotions are serialized by an owner lock so zero Owners can never result. Stale version gives 409.
- UI states: validation error, permission denied, conflict, pending, success.
- Rules: accounts ACC-008, ACC-015, sections 23, 25, 75, 81, 82; ACC-020.
- Acceptance: Given one Owner, When they demote themselves, Then it fails and the role is unchanged. Given two Owners and two parallel demotions, When both run, Then one succeeds and one fails.
- Evidence: BE `AccountMember.ChangeRole`, `AccountOwnerRules.EnsureCanDowngradeOwner` (no command) · FE none · Design none · Tests `AccountMemberTests`, `AccountOwnerRulesTests` · Real e2e none
- State: IMPLEMENTING — command, owner lock (as the Workspaces `WorkspaceOwnerUpdateLocker`), concurrency test.

### UC-ACC-H08 Suspend, reactivate or remove a member
- Capability: H Account-level administration integration
- Actors and permission: Owner, Admin (not Owners); `ManageAccount`.
- Preconditions: target member exists.
- Input: member id, action, `expectedVersion`.
- Output: member Suspended, Active, or Removed; facts `AccountMemberSuspended`, `AccountMemberActivated`, `AccountMemberRemoved`; grant projection synced; Account sessions of a removed member lose Account access at the next check. Workspace memberships are not deleted by Accounts; Workspaces reacts.
- Main flow: 1. Admin chooses the action and confirms. 2. Server validates owner rules and saves. 3. Workspaces and Identity react through facts.
- No-op / rejection / concurrency / retry: repeating the same state is a no-op. Suspending or removing the last Owner fails (`Accounts_Owner_CannotSuspendLastOwner`, `Accounts_Owner_CannotRemoveLastOwner`). A Removed member cannot be activated (`Accounts_Member_CannotActivateRemoved`); a new invitation is needed. Concurrent calls are serialized by the owner lock.
- UI states: destructive confirmation, permission denied, conflict, pending.
- Rules: accounts sections 25, 42, 75, ACC-007, ACC-012; IAREQ030.
- Acceptance: Given the last Owner, When removal is attempted, Then it fails. Given an Admin removes a Member, When done, Then the Member lists no Account and the grant is gone.
- Evidence: BE `AccountMember.Suspend`, `Activate`, `Remove` (no command) · FE none · Design none · Tests `AccountMemberTests`, `AccountOwnerRulesTests` · Real e2e none
- State: IMPLEMENTING — commands, lock, downstream reactions in Workspaces (its member standing mapper) and Identity.

### UC-ACC-H09 Transfer Account ownership (enterprise baseline)
- Capability: H Account-level administration integration
- Actors and permission: current Account Owner; `ManageAccountLifecycle`; step-up.
- Preconditions: target is an Active member.
- Input: target member id, `expectedVersion`, whether the current Owner keeps Owner or becomes Admin.
- Output: target becomes Owner; the current Owner becomes Admin unless they keep Owner; audit record; both receive an email.
- Main flow: 1. Owner opens Members, chooses "Make owner". 2. Re-authenticates. 3. Server promotes the target first, then demotes the caller when requested, in one transaction.
- No-op / rejection / concurrency / retry: target already Owner is a no-op. Never inferred from "first member" or provider state. Stale version gives 409; the owner lock serializes it.
- UI states: destructive confirmation, validation error, conflict, pending.
- Rules: accounts sections 25, 26, ACC-015; Part 5 R-ACC-OWNER.
- Acceptance: Given Owner A and Member B, When A transfers to B and demotes themselves, Then B is Owner, A is Admin, and the Account still has one Owner at every moment.
- Evidence: BE `AccountMember.ChangeRole` only; no transfer operation · FE none · Design none · Tests `AccountMemberTests` · Real e2e none
- State: DEFINED — command, step-up, screen. Reference product: Slack primary owner (Part 1).

### UC-ACC-H10 Add and verify an organization domain (enterprise baseline)
- Capability: H Account-level administration integration
- Actors and permission: Owner, SecurityAdmin; `ManageAccountSecurity` (new). Entitlement may apply (H20).
- Preconditions: Account Active.
- Input: domain name (lowercase, valid hostname, not a public mail provider).
- Output: a Pending AccountDomain and a DNS TXT token shown once (only a hash is stored). After the DNS check succeeds: Verified, `VerifiedAt` set, `AccountDomainVerified` fact. A failed check keeps it Pending; an admin may Reject it.
- Main flow: 1. Admin enters the domain. 2. Sees the TXT record to publish. 3. Presses Verify (or a job retries). 4. Server looks up DNS. 5. On a match, the domain is Verified.
- No-op / rejection / concurrency / retry: verifying a Verified domain is a no-op. A domain Verified by another Account stays Pending here and shows "already claimed". Verification is never inferred from a user's email. Lookup timeout shows "not verified yet", not failed.
- UI states: loading, pending, validation error, conflict (already claimed), success, permission denied.
- Rules: accounts sections 31 to 34, 77, ACC-010; R-ACC-DOMAIN (Part 5).
- Acceptance: Given the TXT record exists, When Verify runs, Then status is Verified. Given another Account holds it, When this Account verifies, Then it stays Pending with "already claimed".
- Evidence: BE `AccountDomain.Create`, `Verify`, `Reject` (no command) · FE none · Design none · Tests `AccountDomainTests` · Real e2e none
- State: IMPLEMENTING — DNS check port, command, screen, uniqueness of Verified domain. Reference product: Atlassian verify-a-domain (Part 1).

### UC-ACC-H11 Publish the primary verified domain to consumers
- Capability: H Account-level administration integration
- Actors and permission: consumers (Workspaces sharing page) through the public fact; no user actor.
- Preconditions: none.
- Input: Account id.
- Output: the earliest verified domain (ties by name), or null.
- Main flow: 1. Consumer calls `IAccountDomainFacts.GetPrimaryVerifiedDomainAsync`. 2. Accounts reads and returns.
- No-op / rejection / concurrency / retry: pending and rejected domains never qualify; unknown Account gives null. Read-only; retry safe.
- UI states: none in Accounts.
- Rules: accounts ACC-010, section 32.
- Acceptance: Given domains "b.com" verified first and "a.com" later, When asked, Then "b.com". Given only pending domains, When asked, Then null.
- Evidence: BE `AccountDomainFactsProvider`, `IAccountDomainFacts` · FE Workspaces share page (consumer) · Design none · Tests none for the provider (consumer tests in Workspaces) · Real e2e none
- State: IMPLEMENTING — a producer test; a writer (H10) so the fact is ever non-null.

### UC-ACC-H12 Turn domain auto-join on or off
- Capability: H Account-level administration integration
- Actors and permission: Owner, SecurityAdmin; `ManageAccountSecurity`.
- Preconditions: domain is Verified.
- Input: domain id, flag, `expectedVersion`.
- Output: flag changed; people who sign in with a verified email of this domain join the Account as Member only (never a Workspace); audit record.
- Main flow: 1. Admin toggles. 2. Server validates and saves.
- No-op / rejection / concurrency / retry: same value is a no-op. Enabling on an unverified domain fails with `Accounts_Domain_CannotEnableAutoJoinUnverified`.
- UI states: validation error, permission denied, conflict, success.
- Rules: accounts ACC-009, ACC-010, section 33; R-ACC-DOMAIN.
- Acceptance: Given a Pending domain, When auto-join is enabled, Then it fails. Given a Verified domain with auto-join, When a verified user of that domain signs in, Then they become an Account Member, not a Workspace member.
- Evidence: BE `AccountDomain.EnableAutoJoin`, `DisableAutoJoin` (no command) · FE none · Design none · Tests `AccountDomainTests` · Real e2e none
- State: IMPLEMENTING — command, the join reaction at sign-in (with Identity), screen.

### UC-ACC-H13 Configure the Account identity provider
- Capability: H Account-level administration integration
- Actors and permission: Owner, SecurityAdmin; `ManageAccountSecurity`; entitlement may apply (H20).
- Preconditions: at least one Verified domain (needed for routing).
- Input: provider type (SAML or OIDC), name (up to 120), issuer (up to 300), SSO URL, certificate reference, JIT provisioning flag, `expectedVersion`.
- Output: IdP saved as Draft; after validation Enabled or Disabled; audit record. Secrets and certificates stay out of events and client contracts.
- Main flow: 1. Admin enters metadata. 2. Server saves locally (Draft). 3. A provider test runs. 4. Admin enables.
- No-op / rejection / concurrency / retry: invalid provider type fails with `Accounts_IdentityProvider_InvalidProviderType`. Local save and provider validation are separate states: "saved", "validation pending", "validation failed". One enabled IdP per Account in this release.
- UI states: loading, validation error, pending (validation), permission denied, conflict, success.
- Rules: accounts sections 35, 36, 78, ACC-011; Part 5 R-ACC-SSO. Authentication itself is Identity: [identity.md](identity.md).
- Acceptance: Given valid metadata, When saved, Then status is Draft and no sign-in routes to it. Given validation passes and the admin enables it, Then H14 routes to it.
- Evidence: BE `AccountIdentityProvider.Create`, `Enable`, `Disable`, `UpdateCertificate` (no command) · FE none · Design none · Tests `AccountIdentityProviderTests` · Real e2e none
- State: IMPLEMENTING — command, validation port, closed status set (enum), screen.

### UC-ACC-H14 Route sign-in to the Account IdP by domain
- Capability: H Account-level administration integration
- Actors and permission: Identity (consumer) on behalf of an unauthenticated person.
- Preconditions: an Enabled IdP and a Verified domain exist.
- Input: email domain.
- Output: a public fact "this domain signs in with IdP X"; Identity then authenticates.
- Main flow: 1. Sign-in form asks Identity for sign-in methods. 2. Identity asks Accounts for the routing fact. 3. Accounts answers from the Verified domain and the Enabled IdP.
- No-op / rejection / concurrency / retry: the rule is deterministic: one Verified domain maps to at most one Enabled IdP. No match returns "no SSO" with no hint of which Accounts exist.
- UI states: none in Accounts.
- Rules: accounts sections 37, 70, ACC-010, ACC-011.
- Acceptance: Given domain "acme.com" Verified and an Enabled IdP, When "a@acme.com" is identified, Then SSO is offered. Given an unknown domain, When identified, Then the response equals the no-SSO response.
- Evidence: BE none in Accounts (Identity `IdentifySignIn` returns `SsoSignInDto`) · FE sign-in (Identity) · Design none · Tests none · Real e2e none
- State: DEFINED — Accounts public fact; see [identity.md](identity.md) for the sign-in side.

### UC-ACC-H15 Configure a SCIM directory and its token
- Capability: H Account-level administration integration
- Actors and permission: Owner, SecurityAdmin; `ManageAccountSecurity`; entitlement may apply.
- Preconditions: an Enabled IdP (the directory may link to it).
- Input: name (up to 120), optional IdP link, enable or disable, rotate token, `expectedVersion`.
- Output: a directory with base URL and a bearer token shown once (only a hash is stored); status Active, Disabled or Error; audit record.
- Main flow: 1. Admin creates the directory. 2. Copies the URL and the token. 3. Configures the external directory.
- No-op / rejection / concurrency / retry: rotating revokes the old token at once. Disabling stops all sync. The token never appears in events, logs or client queries after creation.
- UI states: loading, success (token shown once), validation error, permission denied, conflict.
- Rules: accounts sections 38 to 41, ACC-011, ACC-012, section 85; R-ACC-SCIM.
- Acceptance: Given a created directory, When the token is rotated, Then calls with the old token fail. Given a later read, When the screen loads, Then the token is not returned.
- Evidence: BE `ScimDirectory.Create`, `Enable`, `Disable`, `UpdateCredentials`, `MarkError` (no command) · FE none · Design none · Tests `ScimDirectoryTests` · Real e2e none
- State: IMPLEMENTING — command, SCIM protocol endpoint, token hashing proof, screen.

### UC-ACC-H16 Run a SCIM sync and deprovision people
- Capability: H Account-level administration integration
- Actors and permission: system (external directory calls with the bearer token); Owner and SecurityAdmin read the result.
- Preconditions: directory Active.
- Input: directory changes (create, update, deactivate).
- Output: a sync run with status Pending, Running, Completed, Failed or Cancelled and counters for users created, updated, disabled; each change applied by its owner: Identity (user), Accounts (member), Workspaces (membership). A deactivated person is suspended in the Account and loses sessions; the person is never hard-deleted.
- Main flow: 1. Directory calls the SCIM endpoint. 2. A run starts. 3. Each target owner validates and applies. 4. The run records counts and any error text.
- No-op / rejection / concurrency / retry: a repeated change is a no-op. Partial success is shown as such, with the failed items. Deprovisioning the last Owner is refused and reported. Retries are idempotent by external id.
- UI states: loading, success, partial failure, error, empty (no runs).
- Rules: accounts sections 41, 42, 71, 90, ACC-012; R-ACC-SCIM.
- Acceptance: Given a deactivation in the directory, When the run completes, Then the member is Suspended, their sessions end, and their content stays. Given the last Owner is deactivated, When the run executes, Then that item fails and others still apply.
- Evidence: BE `ScimSyncRun.Start`, `Complete`, `Fail`, `Cancel`; `ScimDirectory.RecordSync` (no caller) · FE none · Design none · Tests `ScimSyncRunTests`, `ScimDirectoryTests` · Real e2e none
- State: IMPLEMENTING — worker, SCIM endpoints, closed status set, screen.

### UC-ACC-H17 Set the default data region
- Capability: H Account-level administration integration
- Actors and permission: Owner, Admin; `ManageAccount`. Entitlement may apply.
- Preconditions: Account Active; target region is in the supported list.
- Input: region code, `expectedVersion`.
- Output: default region for new data changes, `AccountDefaultRegionChanged` fact, audit record. Existing data does not move.
- Main flow: 1. Admin opens Region. 2. Reads that existing data stays where it is. 3. Picks a region and confirms.
- No-op / rejection / concurrency / retry: same region is a no-op. An unsupported region is refused; the system never picks another region silently.
- UI states: loading, destructive confirmation (explains new data only), validation error, permission denied, conflict.
- Rules: accounts sections 43 to 45, 79, ACC-013; R-ACC-REGION.
- Acceptance: Given region EU, When set to US, Then new Workspaces default to US and existing ones stay in EU. Given an unsupported code, When set, Then it fails with no change.
- Evidence: BE `Account.UpdateDefaultRegion`, `AccountRegion` (no command) · FE none · Design none · Tests `AccountTests`, `AccountRegionTests` · Real e2e none
- State: IMPLEMENTING — supported region list, command, screen, Workspaces read of the default.

### UC-ACC-H18 Manage Account-to-Workspace routes
- Capability: H Account-level administration integration
- Actors and permission: Owner, Admin; `ManageAccount`.
- Preconditions: Account Active; a linked Workspace must belong to this Account (Workspaces confirms).
- Input: route slug, Workspace id, default flag, `expectedVersion`.
- Output: route created, linked, unlinked, set or unset as default, deleted or restored; facts for each.
- Main flow: 1. Admin opens Workspaces routing. 2. Adds a route and links a Workspace. 3. Marks one route as default.
- No-op / rejection / concurrency / retry: at most one default route per Account; setting a second default moves it. An empty Workspace id is refused (`Accounts_WorkspaceRoute_InvalidWorkspaceId`). A route never creates or deletes a Workspace.
- UI states: loading, empty, validation error, permission denied, conflict, success.
- Rules: accounts sections 46, 47, ACC-014; R-ACC-ROUTE.
- Acceptance: Given two routes, When the second is made default, Then only it is default. Given a Workspace of another Account, When linked, Then it fails.
- Evidence: BE `WorkspaceRoute.Create`, `LinkWorkspace`, `SetAsDefault` and others (no command) · FE none · Design none · Tests `WorkspaceRouteTests` · Real e2e none
- State: IMPLEMENTING — command, Workspaces ownership check, screen.

### UC-ACC-H19 Read the Account administration audit trail (enterprise baseline)
- Capability: H Account-level administration integration
- Actors and permission: Owner, SecurityAdmin; `ViewAccountAudit` (new).
- Preconditions: audit records exist.
- Input: date range, actor, action type, cursor.
- Output: who did what, to which Account object, when; secrets are never included. Retention follows the governed audit policy.
- Main flow: 1. Admin opens Audit. 2. Filters. 3. Reads the page.
- No-op / rejection / concurrency / retry: read-only; bounded pages of 50. Audit is not user-facing activity.
- UI states: loading, empty, success, permission denied, error.
- Rules: accounts sections 84, 85; R-ACC-AUDIT; reference product GitHub Enterprise audit log (Part 1).
- Acceptance: Given a rename and a role change, When the trail loads, Then both appear with actor and time. Given a Member, When opening it, Then permission denied.
- Evidence: BE none for Accounts (platform audit store exists in `backend/src/Notrelix.Infrastructure/Data/Audit/`) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — which facts write audit records, query, screen.

### UC-ACC-H20 Show "not entitled" apart from "not authorized"
- Capability: H Account-level administration integration
- Actors and permission: any admin.
- Preconditions: a feature (SSO, SCIM, region, audit) is plan-gated by Billing.
- Input: Account id, feature key.
- Output: the UI shows "not included in your plan" with an upgrade path, separate from "you do not have permission".
- Main flow: 1. Screen asks Billing for the entitlement fact. 2. Shows the gate or the controls.
- No-op / rejection / concurrency / retry: the server rejects a write with an entitlement error that differs from the forbidden error. Billing outage fails closed for writes and shows "unknown".
- UI states: not entitled, permission denied, loading, error.
- Rules: accounts sections 80, 89, ACC-016.
- Acceptance: Given an Owner on a plan without SSO, When they open Single sign-on, Then they see "not included in your plan". Given a Member, When they open it, Then they see "no permission".
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Billing entitlement contract (see [billing.md](billing.md)), two distinct error codes, screens.

# 4. Product decisions applied

Product decision (owner, 2026-10-26): Account decisions A1 to A8 are accepted. Used here: no Account deletion and no data export in this release (D05 is BLOCKED and shows no control); high-impact actions need recent step-up authentication (the "revoke others needs step-up" pattern is reused for D03, D04 and H09). The other items (job title, device as session, IP and user agent, accent and sidebar) belong to Identity.

Lead decision (2026-10-30): A new Account permission set is defined. `ManageAccount` (exists): Owner and Admin. `ManageAccountSecurity` (new): Owner and SecurityAdmin, for domains, SSO and SCIM. `ManageAccountLifecycle` (new): Owner only, for close, reopen and ownership transfer. `ViewAccountAudit` (new): Owner and SecurityAdmin. `BillingAdmin` keeps only Billing-owned actions (see [billing.md](billing.md)); it gets no other Account action.

Lead decision (2026-10-30): Any signed-in user with a confirmed email may create a Team Account and becomes its only Owner (A02). Enterprise Accounts are created by a sales-assisted flow owned by Billing, not by this context. The cap on owned Accounts per user is 10.

Lead decision (2026-10-30): Account type (Personal, Team, Enterprise) carries no permission or billing rule by itself. It is a label that Billing and Governance may read.

Lead decision (2026-10-30): The slug is unique across all Accounts, lower case, 3 to 63 characters, and cannot change after creation. Only the display name and legal name change. Generated slugs get a short random suffix on collision.

Lead decision (2026-10-30): `AccountStatus.Trialing` is not an Account lifecycle phase. Trial belongs to Billing. The status is deprecated: new code treats it as Active, and a later migration maps it to Active and removes it.

Lead decision (2026-10-30): `Account.PlanCode` is a derived summary written only by a Billing reaction, never by an Accounts command or a client. Reads of the plan go to Billing (B03).

Lead decision (2026-10-30): Suspension is a platform-operator action. Decided and built 2026-10-31 (owner): the operator is a separate account with its own sign-in scheme, signing key and database login, every action is audited; D01 and D02 are HARDENING. The audit actor shown to the customer is not decided (the trail is operator-only today).

Lead decision (2026-10-30): Closing an Account makes it and its Workspaces read-only, retains all data, pauses provisioning and integrations, and is reversible by an Owner (D03, D04). No purge happens in this release.

Lead decision (2026-10-30): Every Account administration write carries `expectedVersion` of the Account row (or of the child aggregate), and fails closed with a conflict on mismatch. Owner changes also take a row lock on the Account owner set.

Lead decision (2026-10-30): Account invitations default to 7 days and may be set from 1 to 30 days. Resend is revoke plus a new invitation. The signed-in email must equal the invited email.

Lead decision (2026-10-30): Ownership transfer promotes the target first, then optionally demotes the caller, inside one transaction, with step-up (H09). The Account always keeps at least one Owner.

Lead decision (2026-10-30): A domain is proven by a DNS TXT token (shown once, only the hash stored). Only one Account may hold a domain as Verified. Others stay Pending and see "already claimed". Auto-join adds an Account Member only, never a Workspace member.

Lead decision (2026-10-30): One Enabled IdP per Account in this release. SSO routing maps a Verified domain to that IdP. SSO and SCIM are plan-gated by Billing entitlement.

Lead decision (2026-10-30): The SCIM bearer token and the DNS token are shown once, stored only as hashes, rotatable, and never carried in events, logs or client reads. SCIM deprovisioning suspends the Account membership and ends sessions. It never hard-deletes a person.

Lead decision (2026-10-30): A region change sets the default for new data only. Migration of existing data is not offered in this release. Unsupported regions are refused.

Lead decision (2026-10-30): The active Account is a client choice carried in the route or header. The server keeps no current-Account session state, and each request is authorized by membership (F01).

Lead decision (2026-10-30): No public integration contract for Account facts is created until a stable consumer exists, except the three producer-owned public surfaces that already exist (membership facts, membership action, primary verified domain) and the lifecycle fact in D06.

# 5. Canonical updates required

Text for `docs/product/accounts.md` (and Governance where noted) to absorb.

- **R-ACC-PERM.** Account administration actions are `ManageAccount` (Owner, Admin), `ManageAccountSecurity` (Owner, SecurityAdmin), `ManageAccountLifecycle` (Owner only, step-up), `ViewAccountAudit` (Owner, SecurityAdmin). Workspace roles never satisfy them. Add the matrix to the Governance Account baseline.
- **R-ACC-CREATE.** A signed-in user with a confirmed email may create a Team Account and becomes its only Owner. At most 10 owned Accounts per user. Enterprise Accounts are created through the Billing sales flow.
- **R-ACC-SLUG.** The Account slug is globally unique, lower case, 3 to 63 characters, immutable. A conflict is reported on the slug field only.
- **R-ACC-PLAN.** `PlanCode` and `Trialing` are derived and deprecated; Billing is the only commercial writer. Accounts exposes no plan command.
- **R-ACC-LIFE.** Writes to an Account and its children are refused when the Account is Suspended or Closed. Exceptions: lift a suspension, reopen a closed Account, and reads. Suspension is by platform operator; closure and reopening by an Owner with step-up. Other contexts react through a published fact (ACC-005).
- **R-ACC-CONC.** Every Account admin write carries an expected version and fails closed on mismatch. Owner-set changes are serialized.
- **R-ACC-OWNER.** At least one active Owner at all times. Transfer promotes before it demotes, in one transaction, with step-up and an audit record.
- **R-ACC-INVITE.** Invitation expiry defaults to 7 days (1 to 30). The accepting email must match. Revoked, expired and used tokens all return the same generic message.
- **R-ACC-DOMAIN.** Verification by DNS TXT token (hash stored). One Verified holder per domain. Pending and Rejected never route or auto-join. Auto-join adds an Account Member only.
- **R-ACC-SSO.** One Enabled IdP per Account. Routing maps one Verified domain to at most one Enabled IdP. Local save and provider validation are separate states. SSO is plan-gated.
- **R-ACC-SCIM.** Directory token shown once, hashed, rotatable. Each change is applied by the target owner. Deprovisioning suspends and ends sessions; it never deletes the person. Run status is Pending, Running, Completed, Failed or Cancelled, with partial failure listed.
- **R-ACC-REGION.** Default region is for new data only. Unsupported regions are refused. No silent substitution.
- **R-ACC-ROUTE.** Route slugs are unique per Account; at most one default; a linked Workspace must belong to the Account; routes never create or delete Workspaces.
- **R-ACC-AUDIT.** Every Account admin write and the lifecycle facts produce a governed audit record readable by Owner and SecurityAdmin; secrets are never recorded.
- **R-ACC-SWITCH.** Active Account is a client choice in route or header; server authorizes by membership; the client discards all Account-scoped state on change.
- **R-ACC-DELETE.** Account deletion and data export are out of scope until a retention and legal decision exists (owner decision 48, A8).

# 6. Not covered

| Product-doc section | Reason |
|---|---|
| Sections 1 to 3, 6, 8, 9 (mission, boundary, identity, source evidence) | Context statements, exercised through A01, E01 and Part 5 rules, no separate behaviour. |
| Section 10, Account type | A decision only (Part 4): the type carries no rule. Reachable through A01, A02. |
| Section 11 and 53, lifecycle states `Trialing` | Deprecated by Part 4. No use case drives it. |
| Sections 17 to 19, delete and restore | D05, BLOCKED. |
| Section 50 to 52 and 99, Billing relation | B03, H20 and R-ACC-PLAN. Billing owns the behaviour: [billing.md](billing.md). |
| Section 60, Integrations classification | Routing of ownership only; generic providers are in `automation-integrations.md`. |
| Sections 61 and 91, Analytics | Derived by Analytics, see `analytics.md`; no Accounts action. |
| Sections 62 and 63, Account facts | Domain events exist; public contracts only for D06 and the three existing surfaces (Part 4). Per-fact catalogs belong to a contract change when consumers exist. |
| Section 92, service extraction | Architecture topic, not a user-visible use case. |
| Sections 93 to 98, change-impact lists | Review checklists, applied when a use case changes; see `review_on` in front matter. |
| Sections 100 to 105, checklists and the final rule | Each checkbox maps to acceptance lines above: identity (A01, E01), lifecycle (D), member and owner (H07 to H09), domain and IdP (H10 to H14), region (H17), deletion (D05). |
| Region migration of existing data (sections 44, 45, 98) | Decided out of scope (Part 4); no use case. |
| Org-wide 2FA or session policy | Identity and Governance own them. |
| Account member bulk import and export | Not required by the product doc; revisit with SCIM (H16). |
