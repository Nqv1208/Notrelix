---
document_id: PROD-ACCOUNTS
document_type: product-context
status: active
owner: accounts
applies_to:
  - accounts
  - account-administration
  - enterprise-administration
evidence:
  - PRODUCT.md
  - docs/product/product-model.md
  - docs/architecture/bounded-context-map.md
  - backend/src/Notrelix.Domain/Accounts/
  - backend/src/Notrelix.Application/
  - backend/tests/Notrelix.Domain.Tests/
  - backend/tests/Notrelix.Application.Tests/
  - frontend/packages/features/account/
review_on:
  - account-lifecycle-change
  - account-scope-change
  - account-membership-change
  - account-domain-or-identity-provider-change
  - scim-or-provisioning-change
  - region-or-data-residency-change
  - workspace-routing-change
  - billing-account-relationship-change
  - account-deletion-or-retention-change
---

# Accounts Context

> **Accounts owns the durable customer/organization administration boundary above individual Workspaces.**
>
> It provides a stable Account identity and enterprise-level administration without collapsing Account, Identity, Workspace, Governance, Billing, or Integrations into one concept.

This document is the canonical product owner for Accounts semantics.

It does not define backend aggregate implementation, frontend component structure, provider protocol mechanics, or Billing source-of-truth behavior.

# 1. Mission

Accounts represents the customer/organization-level administrative boundary in Notrelix.

It exists for facts that are:

```text
broader than one Workspace
not the user's authentication identity
not generic resource authorization
not merely a billing-provider customer object
```

Accounts allows enterprise administration without inventing a synthetic Workspace for global concerns.

# 2. Context boundary

Accounts may own durable administration concepts whose lifecycle belongs to the Account scope, including:

- Account;
- account-level membership/administration;
- account invitation;
- organization domain claim/verification;
- account-level identity-provider configuration;
- SCIM/directory provisioning configuration;
- account region/data-location preference;
- account-to-Workspace routing;
- Account owner/admin invariants.

Exact source structure may evolve. Ownership follows semantic lifecycle and mutation authority.

# 3. Does not own

```text
user credentials/session/MFA
→ Identity

Workspace lifecycle and Workspace membership
→ Workspaces

resource permission/policy/sharing
→ Governance

plan/subscription/entitlement/payment source of truth
→ Billing

Boards/Items
→ Work Management

Pages/Blocks
→ Documents
```

# 4. ACC-001 — Account is not a Workspace

Account and Workspace are distinct product concepts.

Do not:

- implement Account as a special Workspace;
- store account-wide settings on an arbitrary “primary Workspace”;
- require fake Workspace IDs for Account-level operations.

# 5. ACC-002 — Account scope is explicit

Account-scoped commands, queries, authorization resources, caches, events, and background work establish Account scope directly.

Workspace scope must not be reused merely because existing infrastructure assumes a Workspace.

# 6. Account identity

Account identity is stable independently of:

- Workspace creation/deletion;
- display-name change;
- member changes;
- subscription upgrade/downgrade;
- external provider customer IDs.

Stable identity is the anchor for administrative relationships.

# 7. Account presentation identity

Account may expose product-visible metadata such as:

- name;
- slug;
- legal name;
- Account type.

These must not acquire hidden security/commercial meaning unless explicitly defined.

# 8. Current source evidence

Current source has an `Account` aggregate with product-like fields including `Name`, `Slug`, `LegalName`, `Status`, `Type`, `DefaultRegionCode`, and `PlanCode`, plus lifecycle operations such as create, rename, archive/close, suspend, activate, delete, restore, region update, and plan-code update.

Those facts are current implementation evidence. They are not automatically permanent product authority.

# 9. ACC-003 — Source placement does not automatically expand Account ownership

A field or type under `Domain/Accounts` may be:

```text
approved Account semantic
derived convenience
compatibility mirror
transitional placement
source debt
```

The product contract decides which interpretation survives.

# 10. Account type

Current source recognizes Personal, Team, and Enterprise Account types.

If these remain product semantics, they should represent meaningful Account behavior or administration rather than merely visual labels.

Do not attach implicit permission or Billing rules to Account type without explicit policy.

# 11. Account lifecycle

Conceptually, Accounts may distinguish:

```text
active
suspended
closed/archived
deleted
restored where policy allows
```

A trial/commercial phase may be represented only if the product clearly distinguishes Account lifecycle from Billing lifecycle.

# 12. ACC-004 — Account lifecycle is not Billing lifecycle

Plan upgrades, downgrades, cancellations, provider payment status, or entitlement changes do not automatically create, replace, close, or delete the Account.

Billing owns commercial lifecycle. Accounts owns Account lifecycle.

# 13. Active Account

An active Account means the Account itself is operational according to Account policy.

It does not imply:

- every Workspace is active;
- every member is authorized;
- every paid feature is entitled;
- every provider integration is healthy.

# 14. Account suspension

Suspension is an Account-level administrative state that may affect Workspaces, sessions, provisioning, integrations, or background work through explicit cross-context policy.

# 15. ACC-005 — Suspension effects are coordinated, not hidden cascades

Accounts owns the fact that the Account is suspended.

Other contexts own their reactions.

Do not encode the product workflow as arbitrary direct mutation of every downstream aggregate.

# 16. Closure/archive

Closure/archive means the Account no longer operates normally while records/data may remain retained according to policy.

Closure is distinct from irreversible physical deletion.

# 17. Account deletion

Account deletion is a high-impact product/legal workflow. It must define:

```text
retention
reversibility
Workspace/content consequences
Identity/member references
Billing relationship
Integrations/provisioning consequences
audit/commercial evidence
```

# 18. ACC-006 — Account deletion is not generic soft delete

A reusable soft-delete mechanism can implement part of lifecycle behavior.

It does not define what Account deletion means.

# 19. Restore

If restore is supported, define:

- which Account state returns;
- what downstream access resumes;
- what data cannot be restored;
- how provisioning/Billing/Identity relationships reconcile.

Restoring a database row alone is not enough.

# 20. Account Member

An Account Member represents Account-level organization/customer participation or administration by an Identity.

It is separate from Workspace membership.

# 21. ACC-007 — Account membership and Workspace membership are distinct

Account membership can express organization-wide administration.

Workspace membership expresses collaboration access within one Workspace.

One may influence the other through explicit onboarding/policy, but they remain distinct lifecycle facts.

# 22. Member identity

Account Member references Identity.

Accounts does not own password, MFA, session, OAuth login, or other authentication security state.

# 23. Account role

Account roles may express organization-wide administration such as owner/admin/member where the product supports those concepts.

They do not automatically grant access to every resource in every Workspace.

# 24. ACC-008 — Account role is not universal authorization

Account role can be an authorization input.

It cannot silently bypass:

- Workspace membership;
- resource policy;
- context-specific protected operations.

# 25. Account owner

Account owner is a high-impact Account relationship.

If product policy requires at least one owner/admin, Accounts owns that invariant.

# 26. Ownership transfer

Ownership transfer, if supported, should be explicit, authorized, and auditable.

Do not infer ownership from “first member” or provider state.

# 27. Account invitation

Account invitation is Account-level onboarding into the organization/customer boundary.

It is distinct from a Workspace invitation.

# 28. ACC-009 — Account invitation does not imply Workspace membership

Accepting an Account invitation may establish Account membership.

Workspace membership must still follow Workspaces policy unless a specific product onboarding flow explicitly provisions it.

# 29. Invitation identity

An invitation may contain Account scope, invited email/identity hint, intended Account role, inviter, and expiry.

Identity resolution/creation belongs to the cross-context acceptance workflow.

# 30. Invitation lifecycle

Account invitation lifecycle may include:

```text
pending
accepted
revoked
expired
```

if product-approved.

Expired/revoked invitations must not remain valid access artifacts.

# 31. Organization domain

An Account Domain represents an organization-level domain association/claim.

Potential product use:

- enterprise discovery;
- SSO routing;
- organization onboarding;
- provisioning constraints.

A string that matches member emails is not automatically a verified Account domain.

# 32. ACC-010 — Domain claim and Identity authentication are separate

Accounts may own claimed/verified organization-domain relationships.

Identity owns how a principal authenticates.

A domain can influence routing without becoming authentication state.

# 33. Domain verification

Verification establishes confidence that the Account controls a domain through an approved mechanism.

Do not infer verification solely from the presence of a user with a matching email.

# 34. Domain lifecycle

Possible semantics:

```text
pending verification
verified
failed
revoked/removed
```

if supported.

Domain verification status is not user authentication status.

# 35. Account identity-provider configuration

An Account-level Identity Provider configuration represents the enterprise administrative relationship between an Account and an authentication source.

# 36. ACC-011 — Account IdP configuration does not make Accounts the Identity context

Accounts may own:

```text
which IdP is configured for the Account
organization-level routing/configuration relation
```

Identity owns:

```text
authentication
session
credential/security behavior
```

Provider protocol implementation remains technical.

# 37. SSO routing

If Account domain, routing, or Workspace rules select an IdP, the selection semantics must be explicit and deterministic.

Do not rely on arbitrary provider ordering.

# 38. SCIM / directory provisioning

SCIM represents Account-level enterprise provisioning when product-approved.

It may coordinate Account Members, Identities, and Workspace membership without absorbing those target owners.

# 39. ACC-012 — Provisioning orchestrates ownership; it does not absorb it

A provisioning sync can cause:

```text
Identity create/update/disable
Account membership change
Workspace membership change
```

Each target owner still validates its own lifecycle and invariants.

# 40. SCIM directory

A directory represents the Account's relationship with an external provisioning source.

Its lifecycle may include configured/enabled/disabled and health/sync state where product-visible.

Provider tokens and protocol mechanics remain implementation details.

# 41. SCIM sync run

A sync execution may expose lifecycle such as queued, running, completed, failed, or partially completed if users/admins need that state.

Do not define this lifecycle solely from worker implementation.

# 42. Deprovisioning

External directory deprovisioning must define effects on:

- Identity;
- Account membership;
- Workspace memberships;
- sessions;
- owned resources;
- historical/audit references.

Do not hard-delete the person as a generic shortcut.

# 43. Region / data location

An Account-level region can represent administrative/data-location intent.

It may influence future Workspace/resource placement or data-residency policy.

# 44. ACC-013 — Region is an Account policy/input, not a silent data migration

Changing the Account's default region does not automatically move all existing data.

Distinguish:

```text
default placement for new data
from
migration of existing data
```

# 45. Region changes

A region change can require eligibility checks, infrastructure capability, asynchronous migration, temporary restrictions, or explicit confirmation.

Do not model a complex migration as an instant settings toggle if the system cannot guarantee it.

# 46. Workspace routing

Account-level Workspace routing represents organization-wide rules relating Account administration to Workspace selection/placement/onboarding.

Possible inputs can include domain, region, IdP, or enterprise provisioning policy.

# 47. ACC-014 — Workspace routing does not make Accounts the Workspace owner

Accounts may own organization-wide defaults/routing.

Workspaces owns Workspace lifecycle and Workspace membership.

# 48. Account rules

Accounts may own rules that are genuinely Account administration invariants.

General permission/policy/sharing belongs to Governance.

# 49. ACC-015 — Administrative invariant is not generic authorization policy

Example:

```text
Account must retain at least one owner
→ Accounts
```

Example:

```text
member may edit Board
→ Governance + Work Management resource semantics
```

# 50. Account and Billing

Billing references stable Account identity for commercial relationships.

Account identity must not derive from provider customer/subscription identity.

# 51. Current PlanCode evidence

Current Account source has a mutable `PlanCode`.

This must not automatically make Accounts the commercial source of truth.

If retained, classify its meaning explicitly as one of:

```text
derived summary
denormalized convenience
compatibility mirror
transitional source debt
```

unless product governance intentionally changes Billing ownership.

# 52. ACC-016 — Billing is the authoritative commercial owner

Plan, subscription, entitlement, usage, and payment lifecycle belong to Billing.

Accounts may consume/display derived commercial facts but must not independently mutate a competing source of truth.

# 53. Current Trialing evidence

Current `AccountStatus` includes `Trialing`.

That can be legitimate only if Account product semantics intentionally define an Account lifecycle phase separate from Billing subscription state.

Otherwise it is a coupling smell that should be migrated rather than normalized into product authority.

# 54. Account and Governance

Accounts supplies Account-level resource/member/admin facts.

Governance owns reusable permission/policy/sharing semantics.

# 55. Account administration authorization

High-impact Account operations can include:

- rename Account;
- manage Account owners/members;
- configure domains/SSO/SCIM;
- set region;
- suspend/close/delete Account.

They require Account-scoped authorization.

A Workspace role is not sufficient unless explicit policy maps it.

# 56. ACC-017 — Backend is final authority for Account administration

Frontend may hide or disable unavailable controls for UX.

Server/Application authorization remains authoritative.

# 57. Account and Identity

Identity supplies principal/user identity.

Accounts may reference Identity for members, owners, accepted invitations, administrators, and provisioning.

Credentials and sessions remain Identity-owned.

# 58. Account and Workspaces

An Account may contain or administratively relate to multiple Workspaces.

Accounts can own Account-to-Workspace administrative relationships/defaults.

Workspaces owns each Workspace and its membership lifecycle.

# 59. Workspace creation

If Account controls eligibility/defaults for Workspace creation, the flow may combine Account administration/entitlement facts with the Workspaces creation contract.

The resulting Workspace remains Workspaces-owned.

# 60. Account and Integrations

Classify enterprise provider relationships by product purpose.

```text
organization IdP / SCIM
→ Account administration + Identity relationship

generic external product provider
→ Integrations
```

Protocol alone does not decide context.

# 61. Account and Analytics

Analytics may derive Account-level metrics such as member count, Workspace count, provisioning health, or region distribution.

Those are derived; source Account facts remain Accounts-owned.

# 62. Account facts/events

Potential stable Account facts include:

```text
AccountCreated
AccountRenamed
AccountSuspended
AccountActivated
AccountClosed
AccountDeleted
AccountRestored
AccountMemberAdded/Removed
AccountDomainVerified
AccountIdentityProviderConfigured
AccountRegionChanged
```

Only create public/integration contracts when stable consumers exist.

# 63. ACC-018 — Account events carry stable scope, not full object graphs

Cross-boundary Account facts should carry stable Account identity and the relevant changed fact.

Avoid full aggregate/member lists, credentials, provider tokens, or secrets.

# 64. Consistency

Account-local lifecycle mutation should be atomic within Accounts.

Cross-context consequences of suspension/closure/delete use explicit coordination and durable reactions where eventual consistency fits.

# 65. ACC-019 — Cross-context consequence does not transfer ownership

Example:

```text
Account suspended
→ Workspaces may restrict use
→ Identity may revoke/limit sessions
→ Integrations may pause
```

Accounts still owns the suspension fact; participant contexts own their reactions.

# 66. Strong consistency

If an Account operation requires another context to succeed atomically, the business invariant must be stated explicitly.

Shared database convenience is not sufficient justification.

# 67. Account creation journey

Conceptually:

```text
authorized principal
→ create Account
→ establish stable Account identity
→ establish initial owner/admin relation
→ optional Workspace/Billing setup through explicit contracts
```

Do not hide several context-owned objects inside one Account aggregate.

# 68. Enterprise onboarding journey

Possible enterprise onboarding:

```text
create Account
→ verify organization domain
→ configure IdP
→ configure SCIM
→ establish Account admins
→ provision/route Workspaces
→ attach Billing entitlement
```

Each step remains owned by the relevant context.

# 69. Member onboarding journey

```text
invite at Account scope
→ resolve/create Identity through proper workflow
→ create Account membership
```

Workspace membership remains separate unless explicitly provisioned.

# 70. SSO onboarding journey

```text
resolve Account/domain
→ resolve configured Account IdP
→ Identity authenticates
→ Account/Workspace/Governance determine access
```

Accounts never performs credential authentication itself.

# 71. SCIM onboarding journey

```text
external directory change
→ provisioning mapping
→ target Identity/AccountMember/Workspace operations
→ per-owner validation
→ sync result
```

Distributed sync may be partially successful; product status should reflect reality.

# 72. Suspension journey

Before Account suspension, the product should communicate relevant impact on access, Workspaces, provisioning, integrations, and reversibility.

Suspension should not silently destroy content.

# 73. Close/delete journey

A high-impact close/delete operation should establish:

```text
authorization
Account scope
retention
Workspace/content impact
Identity/member impact
Billing relationship
Integrations/provisioning impact
recovery/irreversibility
```

# 74. Failure — Account identifier conflict

If slug/identifier uniqueness exists, distinguish display name, legal name, and unique identifier conflicts.

# 75. Failure — owner invariant

Operations that would violate required Account owner/admin invariants fail synchronously and clearly.

# 76. Failure — authorization

Workspace administration does not automatically imply Account administration.

The failure semantics should preserve Account scope.

# 77. Failure — domain verification

Domain verification can be pending, failed, conflicting, or already claimed. It must not become verified until proof succeeds.

# 78. Failure — provider uncertainty

SSO/SCIM/provider validation can fail or become uncertain.

Distinguish:

```text
local configuration saved
provider validation pending
sync unhealthy
```

where relevant.

# 79. Failure — region unsupported

Region change can fail eligibility/compliance/infrastructure constraints.

Do not silently substitute another data location when region has real semantic impact.

# 80. Failure — entitlement

An enterprise feature may be unavailable due to Billing entitlement.

That is not the same as Account authorization failure.

# 81. Concurrency

High-impact Account administration may need explicit concurrency protection, especially owner, lifecycle, region, IdP, and SCIM changes.

# 82. ACC-020 — High-impact Account changes do not silently overwrite stale state

If concurrent changes can create security, ownership, or data-placement ambiguity, conflict should fail closed or reconcile according to explicit product policy.

# 83. Deletion and references

Closing/deleting an Account may leave retained references in audit, commercial evidence, historical activity, and external sync records.

Retention policy determines treatment; do not cascade blindly.

# 84. Audit

High-impact Account administration should generate governed audit evidence where required, including owner changes, IdP/SCIM changes, domain verification, region changes, suspension, closure, and deletion.

Audit is not user-facing activity.

# 85. Privacy/security

Account administration can expose domains, identity-provider metadata, provisioning details, member lists, and enterprise configuration.

Access must be Account-scoped and secrets must not leak into events/client contracts.

# 86. Frontend implications

Account administration should be visibly Account-scoped and distinct from Workspace settings.

Current source has a dedicated `frontend/packages/features/account` package, which is implementation evidence rather than product authority.

# 87. Scope indicator

High-impact Account admin surfaces should make current Account identity clear, especially for:

- SSO;
- SCIM;
- region;
- domains;
- members;
- deletion.

# 88. Workspace relationship UX

Navigation should distinguish:

```text
Account administration
↔ Workspace settings/work
```

rather than presenting every setting under one generic Workspace shell.

# 89. Entitlement versus permission UX

If Account enterprise features are plan-gated, show `not entitled` separately from `not authorized`.

# 90. Async administration

Provisioning, provider validation, and data-region migration may be asynchronous. Product state must not claim completion before the operation is actually complete.

# 91. Analytics implications

Account-level reporting can derive member, Workspace, provisioning, region, and enterprise-administration metrics.

Analytics remains derived.

# 92. Service extraction

Accounts is a semantic extraction seam, but extraction requires stable contracts, explicit data ownership, no foreign writes, and clear Identity/Workspaces/Billing/SCIM relationships.

Bounded-context existence alone does not require a service.

# 93. Current source alignment

Current Domain `Accounts` includes subareas for:

```text
Accounts
Domains
IdentityProviders
Invitations
Members
Regions
Rules
Scim
WorkspaceRoutes
```

This is strong evidence that current implementation treats Accounts as a broader enterprise-administration area rather than one aggregate only.

The product model still decides which of those placements remain canonical.

# 94. Current ambiguity watch

Two current details deserve continued scrutiny:

```text
Account.PlanCode
AccountStatus.Trialing
```

Neither may silently compete with Billing semantics.

If retained, their derived Account meaning must be explicit and non-authoritative for commercial state.

# 95. Change impact — lifecycle

Changing Account lifecycle requires review of:

```text
Workspaces
Identity
Governance
Billing
Integrations
Automation
Analytics
frontend Account/Workspace shell
retention
events
```

# 96. Change impact — membership/roles

Review Identity, Workspace membership, Governance, SCIM, invitations, owner invariants, and audit.

# 97. Change impact — domain/IdP/SCIM

Review Identity authentication, enterprise login routing, provisioning, Workspace membership, security, provider compatibility, audit, and frontend administration.

# 98. Change impact — region

Review infrastructure/data residency, Workspace/resource placement, existing-data migration, operational recovery, and customer communication.

# 99. Change impact — Billing relation

Any Account-owned field that mirrors plan/subscription/entitlement must be reviewed against Billing ownership and may not become a second writable commercial state.

# 100. Account invariants checklist

```text
[ ] stable Account identity
[ ] Account != Workspace
[ ] Account lifecycle explicit
[ ] Account member != Workspace member
[ ] owner/admin invariant explicit
[ ] Billing does not define Account lifecycle
[ ] Identity owns authentication
[ ] Governance owns generic permission policy
[ ] domain verification explicit
[ ] IdP/SCIM boundary explicit
[ ] region semantics explicit
[ ] deletion/retention explicit
```

# 101. Enterprise administration checklist

```text
[ ] operation is truly Account-scoped
[ ] current Account is visible
[ ] authorization owner is known
[ ] entitlement is distinguished from permission
[ ] provider uncertainty is handled
[ ] audit requirement is considered
[ ] downstream Workspace/Identity effects are explicit
[ ] no fake Workspace scope
```

# 102. Testing/evidence

Critical evidence should cover as applicable:

- Account lifecycle/no-op;
- Account-vs-Workspace scope;
- owner/member invariants;
- invitation lifecycle;
- domain verification;
- IdP/SCIM configuration;
- authorization;
- region policy;
- Billing relationship;
- deletion/restore;
- concurrency;
- emitted facts;
- cross-context reactions.

# 103. Stop conditions

Stop rather than guess if:

- Account and Workspace are being merged;
- a Workspace role is assumed to grant Account administration;
- plan/subscription state becomes independently writable in Accounts;
- Account Member and Workspace Member are treated as one lifecycle;
- SSO/SCIM bypasses Identity/Workspaces ownership;
- domain verification is inferred from email alone;
- region implies migration with no migration contract;
- Account deletion relies only on DB cascade;
- provider protocol becomes Account ubiquitous language;
- source conflicts with canonical ownership and no drift classification exists.

# 104. Related canonical owners

```text
PRODUCT.md
docs/product/product-model.md
docs/product/product-experience.md
docs/product/contexts/identity.md
docs/product/contexts/workspaces.md
docs/product/contexts/governance.md
docs/product/contexts/billing.md
docs/product/contexts/integrations.md
docs/architecture/bounded-context-map.md
docs/architecture/data-ownership-and-consistency.md
docs/architecture/contract-boundaries.md
docs/architecture/events-realtime-and-delivery-boundary.md
```

# 105. Final Accounts rule

For every Account-level capability, answer:

```text
Why is this Account-scoped rather than Workspace-scoped?
What Account fact is authoritative?
Which Identity participates?
Which Account members/admins participate?
What authorization applies?
What Billing entitlement applies?
Which Workspaces are affected?
What provider/provisioning relationship exists?
What happens on suspension/closure/deletion?
What data is retained?
Which downstream contexts react?
```

The target is:

> **a stable customer/enterprise administration boundary that coordinates organization-wide behavior without becoming a catch-all for identity, workspace, permission, billing, or provider internals.**
