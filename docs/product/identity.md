---
document_id: PROD-IDENTITY
document_type: product-context
status: active
owner: identity
applies_to:
  - identity
  - authentication
  - sessions
  - credentials
  - mfa
  - oauth
  - api-tokens
  - user-security
evidence:
  - PRODUCT.md
  - docs/product/product-model.md
  - docs/product/contexts/accounts.md
  - docs/product/contexts/workspaces.md
  - docs/architecture/bounded-context-map.md
  - backend/src/Notrelix.Domain/Identity/
  - backend/tests/Notrelix.Domain.Tests/
  - backend/tests/Notrelix.Application.Tests/
  - frontend/packages/features/auth/
review_on:
  - authentication-model-change
  - session-lifecycle-change
  - credential-or-token-change
  - mfa-change
  - oauth-linking-change
  - identity-deletion-or-anonymization-change
  - api-token-scope-change
  - account-or-workspace-membership-boundary-change
---

# Identity Context

> **Identity proves who or what is acting and owns authentication/security lifecycle.**
>
> It does not decide whether an authenticated principal may perform an arbitrary product operation.

This document is the canonical product owner for Identity semantics.

# 1. Mission

Identity owns stable principal identity and security lifecycle needed to authenticate:

```text
human users
sessions
credentials
MFA methods
OAuth-linked identities
service/API tokens
verification/reset tokens
security settings
```

Identity does not become the general authorization engine.

# 2. Does not own

```text
Account membership / Account administration
→ Accounts

Workspace membership / invitations
→ Workspaces

resource permissions / sharing / policy
→ Governance

commercial entitlement
→ Billing

business resources
→ owning product contexts
```

# 3. ID-001 — Authentication and authorization are separate

Authentication answers:

```text
Who/what is acting?
Is the presented credential/session valid?
```

Authorization answers later:

```text
May this principal perform action X on resource Y?
```

Identity MUST NOT embed Workspace role checks or arbitrary resource access rules into credential verification.

# 4. Principal identity

A principal requires a stable identifier independent of mutable presentation data such as email or display name.

Cross-context references should use stable identity, not mutable email as the sole durable key.

# 5. User

A User represents the durable human principal identity in Notrelix.

User lifecycle is distinct from:

- Account membership;
- Workspace membership;
- active session;
- subscription status;
- resource ownership.

# 6. ID-002 — Mutable email is not universal identity

Email may be:

- login identifier;
- contact channel;
- invitation target;
- provider attribute.

It MUST NOT be treated as the only durable identity across contexts.

# 7. User profile

Profile/presentation data may include name/avatar/preferences where product-approved.

Profile state is not credential state.

Account preferences owned by Identity are the signed-in person's locale, time zone and theme. They are
the person's own, apply across every workspace, are changed only by that person, and are read and saved
with a version so two devices cannot silently overwrite each other.

The person's phone number is an Identity account fact (D15, accepted 2026-10-23), saved with the same
preferences write and version. It is stored in normalized E.164 form: `+` then 8 to 15 digits, first digit
non-zero; spaces, hyphens, dots and parentheses typed by the person are removed, any other character is
rejected, and a blank value clears the phone. It is private to the person by default. The flag
`phoneVisibleToWorkspaceMembers` (default false) is the person's opt-in to let the members of Workspaces
they share see it. The phone is shown to another person only when the flag is true AND the reader is an
active, non-guest member of a Workspace the person is also an active, non-guest member of; it is never
shown to a guest, never on a share link or public view, and never used as a login or recovery identifier.

Job title is NOT an Identity fact: it is a per-Workspace member profile owned by Workspaces
(`docs/product/workspaces.md`). Date format, the default view and the sidebar default are client-side
presentation, not Identity preferences. The notification preference matrix is owned by Collaboration.

The person's profile photo is an Identity fact. It is either an external address or an uploaded image. An
uploaded image is PNG, JPEG or WebP, recognised from its bytes (never from the declared type or file name),
at most 2,000,000 bytes; anything else, including SVG and GIF, is refused (`identity.avatar.unsupported-format`
415, `identity.avatar.too-large` 413). The person replaces or removes only their own photo; changing the
display name does not touch the photo. A replaced or removed object is not deleted at request time (the
request could still roll back); a background sweep removes avatar objects older than a grace period that no
user row references. The photo is served only through an authorised read: the person and people who share an
active Workspace with them can read it, everyone else, and every user without an uploaded photo, gets the
same not-found answer.

The person's default Workspace (the one that opens with the app) is an Identity fact stored on the profile;
the choice is validated and cleared by Workspaces (`docs/product/workspaces.md`).

Changing display profile must not silently change security identity.

# 8. User lifecycle

Identity should define the difference between:

```text
active
disabled/suspended
deleted/anonymized
```

where supported.

A disabled User may remain referenced historically by other contexts.

# 9. ID-003 — Identity deletion does not cascade business history

Deleting/anonymizing Identity does not automatically delete:

- Workspace content;
- comments/history;
- audit evidence;
- Billing records;
- resource ownership history.

Each owning context applies retention/reference policy.

# 10. Authentication credential

Credentials prove identity.

Examples:

```text
password verifier
OAuth/SSO provider subject
MFA method
session/refresh credential
API token
one-time reset/verification token
```

Raw secret material is not ordinary product data.

# 11. ID-004 — Secret material never becomes ordinary Domain/client data

Passwords, refresh tokens, MFA seeds, OAuth secrets, API-token secrets, signing keys, and one-time secrets must remain protected through approved security infrastructure.

Domain/product contracts may expose:

- credential identity;
- status;
- creation/expiry metadata;
- safe display suffix/fingerprint.

They must not expose raw reusable secrets.

# 12. Password

If password authentication is supported, Identity owns password credential lifecycle semantics:

```text
set
verify
change
reset
invalidate where needed
```

Password hashing implementation is technical/security infrastructure.

# 13. Password reset

Reset is a security-sensitive one-time workflow.

It must define:

- token identity;
- expiry;
- one-time use;
- replay rejection;
- resulting session/credential invalidation policy.

Accepted policy (2026-10-23): a successful reset proves control of the account, so it (a) revokes every active session and every access token issued before it, exactly as a password change does, and (b) lifts the account's sign-in lockout. It does not lift the lockout of the client address, which also protects other accounts. A reset is recorded in the person's security activity.

# 14. Email verification

Email verification token lifecycle is separate from User identity lifecycle.

Verified email is evidence about an address, not proof of arbitrary Account/Workspace ownership.

# 15. One-time tokens

One-time security tokens MUST fail after successful consumption or expiry.

Retry/idempotency around callbacks must not create a second valid authority.

# 16. Session

A Session represents authenticated continuity for a principal/device/client context.

It has explicit lifecycle independent from User existence.

# 17. ID-005 — Session lifecycle is explicit

Session semantics include as applicable:

```text
created
active
refreshed/rotated
expired
revoked
invalidated by security event
```

A revoked/expired session must not silently become valid again.

# 18. Refresh rotation

Refresh-token rotation must avoid accidentally creating two independent long-lived valid authorities unless a bounded overlap window is explicitly designed.

Replay of a rotated/revoked refresh credential must fail according to security policy.

# 19. Session revocation

Revocation may target:

- one session;
- all sessions for User;
- sessions affected by credential/security change.

The scope of invalidation must be explicit.

# 20. ID-006 — Security events invalidate the intended authorities

A password reset, MFA/security event, account-link change, or administrative identity action must define which sessions/tokens remain valid.

Do not leave stale authority active accidentally.

Turning the second factor off ends every session of the person, the one that asked included (confirmed 2026-10-30); the person signs in again.

Turning the second factor on is the reverse: starting the enrollment and activating it each need a single-use step-up proof of their own (a session alone may not enrol a factor, or a stolen session could lock the owner out). When the factor becomes active every other session of the person ends; the session that enrolled it stays, and the person is told how many other devices were signed out (confirmed 2026-10-30).

# 21. Session claims

Claims may cache identity/security hints for performance.

They are not permanently authoritative when:

- Workspace membership changes;
- Governance policy changes;
- Billing entitlement changes.

# 22. MFA

MFA strengthens authentication.

Identity owns:

```text
method enrollment
verification
activation
disable/remove
recovery semantics
method status
```

MFA does not define product permissions.

# 23. ID-007 — MFA lifecycle is security lifecycle, not Workspace policy

A Workspace or Account may require MFA through policy.

Identity owns the actual MFA method and verification state.

Governance/Accounts may own the requirement policy.

# 24. MFA enrollment

Enrollment should distinguish:

```text
method created/pending
verified
active
disabled/removed
```

where applicable.

An unverified enrollment must not be treated as active MFA.

Starting and activating an enrollment need a step-up proof each (ID-006); a wrong code leaves the pending method and the proof alone.

# 25. MFA removal

Removing the last required MFA method can be blocked by security policy.

The requirement source may come from Account/Governance policy, while Identity owns the method lifecycle.

# 26. OAuth / linked identity

OAuth/SSO linking associates external provider identity with a Notrelix User.

Identity owns user-authentication linking semantics.

# 27. ID-008 — Provider subject identity outranks email-only linking

External linking must validate stable provider/subject identity and collision rules.

An email string alone is not sufficient universal proof that two external identities should merge.

# 27.1. A provider that gives no verified-email claim cannot create an account

Signing in through a provider may create a new User only when the provider vouches that the email address is verified. A provider that sends no verified-email claim (Microsoft) therefore never creates an account: only an identity that is already linked to a User signs in through it, and a person without an account, or whose address belongs to another account, is refused with the provider-email-not-verified answer. Such a person registers with email and password, or links the provider from the security page, and signs in through it afterwards. The same rule holds for any provider added later (confirmed 2026-10-30).

# 28. Account takeover prevention

Link/unlink flows must defend against:

- provider-subject collision;
- stale callback;
- CSRF/state replay;
- email-only merge;
- losing the last viable login method without explicit flow.

# 29. OAuth versus Integrations

```text
OAuth for authenticating/linking a Notrelix User
→ Identity

OAuth for connecting an external business provider
→ Integrations
```

Protocol does not decide product ownership.

# 30. Account-level SSO

Accounts may own which enterprise IdP is configured for an Account.

Identity owns actual authentication/session/security lifecycle through that provider.

# 31. ID-009 — Account IdP configuration and Identity authentication remain separate

Accounts can route/select an IdP.

Identity authenticates the principal.

Workspaces/Governance later decide access.

# 32. API/service token

An API token represents a non-password credential used by a User/service principal according to product policy.

It must define:

```text
owner/subject
allowed scopes/capabilities
status
expiry
revocation
safe display metadata
```

# 33. ID-010 — API token is a scoped identity credential

An API token MUST NOT be treated as “full user access forever”.

Its scopes, subject, lifecycle, expiry, and revocation are explicit.

# 34. API-token secret

Raw token value is shown only at creation/recovery semantics if product allows it.

Persist only safe verifier/hash where possible.

Never log the raw token.

# 35. API-token scope

Token scope is credential-level capability limitation.

It is not a substitute for:

- Workspace membership;
- Governance authorization;
- resource business rules.

Final authorization still evaluates the operation/resource context.

# 36. Security settings

Identity may own security settings directly tied to authentication/session behavior.

Account-wide security requirements belong to Accounts/Governance when they are policy rather than one User's credential state.

# 37. ID-011 — Security configuration is split by semantic owner

Example:

```text
User has MFA method
→ Identity

Account requires MFA
→ Accounts/Governance policy

Workspace resource edit permission
→ Governance
```

Do not put all “security settings” under one aggregate by naming.

# 38. Authentication success

Successful authentication establishes a principal/session.

It does NOT imply:

```text
Account membership
Workspace membership
resource permission
feature entitlement
```

Those checks occur later.

# 39. ID-012 — Authentication success is not product-access success

A valid User can legitimately have no access to a particular Account, Workspace, Board, Page, or feature.

Do not treat login success as authorization to all known resources.

# 40. Identity and Accounts

Accounts references stable Identity for Account members/admins.

Identity does not own Account membership lifecycle.

# 41. Identity and Workspaces

Workspaces references stable Identity for Workspace members/invite acceptance.

Identity does not own Workspace role or membership status.

# 42. Identity and Governance

Governance consumes principal/security facts as authorization inputs.

Identity does not answer operation-level permission questions.

# 43. Identity and Billing

Billing may associate commercial entities with Account/User where product-approved.

Authentication validity is independent of payment-provider state.

# 44. Identity and Collaboration

Comments/activity may reference historical actor identity.

Identity deletion/anonymization must not corrupt Collaboration history.

# 45. Identity and Integrations

User-authentication provider links belong to Identity.

External business-provider connections belong to Integrations even if OAuth is the technical protocol.

# 46. Identity and Automation

Automation execution may act as:

- initiating User;
- service/system principal;
- automation identity.

The acting principal model must remain explicit for authorization/audit.

# 47. Service/system principal

If non-human principals exist, they need stable identity and explicit credential/access lifecycle.

Do not impersonate a random human User merely because backend automation needs authorization context.

# 48. ID-013 — Acting principal must be explicit

Every security-sensitive operation should be attributable to a meaningful principal class:

```text
human user
API/service token
automation/system
integration/provider callback where translated
```

# 49. Security-sensitive audit

Identity security changes should produce governed audit evidence where policy requires:

- credential reset;
- MFA change;
- session revocation;
- OAuth link/unlink;
- API-token create/revoke;
- User disable/delete.

Audit must not contain reusable secrets.

# 50. Events/facts

Potential stable facts include:

```text
UserCreated
UserDisabled
UserDeleted/Anonymized
SessionRevoked
CredentialChanged
MfaEnabled/Disabled
OAuthIdentityLinked/Unlinked
ApiTokenCreated/Revoked
```

Only expose cross-boundary facts when stable consumers exist.

# 51. ID-014 — Security events never expose reusable secrets

Event/client payloads may carry safe identity/status metadata.

They must not carry:

- raw passwords;
- raw refresh tokens;
- MFA seed;
- raw API token;
- provider client secret.

# 52. Concurrency

Security-sensitive state changes must protect against stale writes.

Examples:

- enabling/disabling MFA;
- revoking token;
- linking/unlinking provider;
- changing security settings.

# 53. ID-015 — Security-sensitive stale writes fail safely

Do not silently overwrite newer security state with stale administrative/client state.

Use explicit concurrency or equivalent protection where needed.

# 54. Idempotency

Provider callbacks, token consumption, session revocation, and unlink operations may be retried.

Retry must not:

- create duplicate linked identities;
- revive revoked sessions;
- consume one-time token twice;
- duplicate security side effects.

# 55. User disable

Disabling Identity may prevent future authentication/use according to security policy.

It is distinct from:

- removing one Workspace membership;
- removing Account membership;
- deleting all business data.

# 56. User deletion/anonymization

Deletion/anonymization must define:

```text
authentication record
profile/personal data
session/token invalidation
historical references
audit retention
external provider links
```

# 57. ID-016 — Identity retention is separate from business-resource retention

A User can become unable to authenticate while their historical business actions/resources remain retained under other contexts' policies.

# 58. Frontend bootstrap

Frontend authentication state may include:

- current principal;
- session validity;
- bootstrap/loading;
- session expired/revoked.

It should not prematurely infer Workspace/resource authorization from authentication alone.

# 59. Session expiry UX

When session expires/revokes:

- protected actions stop;
- user receives appropriate re-authentication flow;
- sensitive stale UI should not continue behaving as authorized.

# 60. Workspace switch

Workspace switch is not an Identity mutation.

Identity remains same principal while Workspaces/Governance scope changes.

# 61. Multiple Accounts/Workspaces

One User may participate in multiple Accounts/Workspaces.

Identity should not store one permanently authoritative “current Workspace” as identity truth.

Public fact: Identity publishes users' sign-in email through `IIdentityUserFacts.FindEmailsAsync` (lower-case, trimmed). It is personal data: Identity does not decide who may see it. A consumer that shows an email owns that visibility rule, applies it before calling the port, and never calls it for a reader the rule excludes. Workspaces shows a member's email only to a Workspace Owner or Admin, for active non-Guest members (docs/product/workspaces.md section 48.2).

# 62. ID-017 — Current Account/Workspace is session/UI context, not principal identity

Selected scope can change during one authenticated session.

Stable identity remains independent.

# 63. Login identifier changes

Changing email/username/login identifier must preserve stable User identity and cross-context references.

Do not create a new User accidentally for a simple identifier change.

# 64. Duplicate identity resolution

Merging identities is high-risk and requires explicit product/security policy.

Do not auto-merge based only on matching email or display data.

# 65. ID-018 — Identity merge is explicit, not heuristic

If merge is supported, define:

- proof;
- conflict handling;
- provider links;
- sessions;
- historical references;
- Account/Workspace memberships;
- audit.

# 66. Recovery

Identity recovery flows should preserve security:

```text
verify recovery authority
invalidate appropriate stale credentials
restore access
record audit
```

Recovery must not bypass normal proof because user “knows” profile information.

# 67. Rate limiting / abuse

Rate limiting, brute-force protection, lockout, and anomaly controls are security/runtime mechanisms.

Identity product semantics define user-visible consequences where applicable.

User-visible consequences of credential lockout (accepted D10):

- repeated wrong credentials temporarily lock further attempts per account and per client address;
- the answer is generic and identical for known and unknown accounts (no account-existence disclosure);
- a locked attempt is not verified, even with the correct credential, until the lock ends;
- the limits are product values accepted on 2026-10-23: an account allows 10 wrong credentials in 15 minutes and is then locked for 15 minutes; a client address allows 50 in 15 minutes and is then locked for 15 minutes (configurable, see backend configuration);
- a second-factor challenge allows 5 wrong codes; after that the challenge is dead and the person signs in again from the password step (the account and address counters still apply);
- no response tells the person how many attempts are left;
- a successful password reset lifts the account's lock but never the client address's lock. An administrator unlock is not offered.

The same lockout applies to a signed-in person re-verifying with a second-factor code or the current password (security step-up and password change): a locked attempt is not verified until the lock ends.

Account security page facts (the person's own, never another person's): whether a password exists and when it last changed (only changes made since this was recorded are known), whether a second factor is enabled and how many recovery codes remain, how many sessions are active, and which sign-in providers are linked. Revoking a session signs that device out; revoking another person's session is not possible and is indistinguishable from an unknown session.

# 67A. Security activity

The person can see their own recent security activity (accepted 2026-10-23). It is Identity-owned, user-visible history and is separate from the operator security audit: it never exposes audit details, and audit does not replace it.

- Recorded facts: sign-in succeeded, sign-in failed, password changed, password reset, second factor enabled, second factor disabled, one session revoked, other sessions revoked. A wrong second-factor code during sign-in counts as a failed sign-in.
- A failed sign-in is recorded only for an existing account, so the owner can see someone guessing. An unknown email leaves no record, so the feed cannot be used to learn which emails exist and an attempted email or password is never stored.
- Each line carries its type, the time, the client address and user agent when known, and the session when the event concerns one.
- Only the person sees their own lines, newest first; there is no way to read another person's activity.
- Lines are kept for 90 days and then removed.
- No client address geolocation is offered.
- Registration creates a session and therefore records a sign-in.

# 68. Account suspension relationship

Account suspension may cause Identity/session restrictions according to explicit cross-context policy.

It does not change the User's global identity automatically.

# 69. Workspace removal relationship

Removing a User from one Workspace does not revoke the User globally.

It removes one Workspaces-owned membership/access relationship.

# 70. Commercial entitlement relationship

A valid Identity may lack a paid entitlement.

Login should remain conceptually distinct from Billing feature availability unless product policy explicitly disables Account access.

# 71. Current source alignment

Current Domain Identity is organized into:

```text
Users
Sessions
Tokens
Mfa
OAuth
Profiles
Security
```

Current source also includes `UserSession`, `ApiToken`, token hashes/one-time tokens, MFA methods, and OAuth/security concepts.

This supports a broad authentication/security lifecycle interpretation of Identity.

# 72. Source ambiguity watch

Do not normalize any future source field that starts storing:

- Workspace roles;
- Account authorization;
- feature entitlement

inside User/session claims as permanent authoritative identity state.

Those are cached/derived facts at most unless architecture intentionally changes.

# 73. Testing/evidence

Critical evidence should cover:

```text
authentication boundary
credential validation
session create/refresh/revoke
refresh replay
one-time token replay
MFA lifecycle
OAuth collision/link/unlink
API token lifecycle/scope
secret non-exposure
security audit
User disable/delete/anonymization
stale/concurrent security changes
```

# 74. Change impact

Identity changes may impact:

```text
Accounts
Workspaces
Governance
frontend auth/bootstrap
API authentication
Integrations/SSO
audit
Automation/service principals
```

# 75. Stop conditions

Stop rather than guess if:

- login success is being used as authorization;
- email is becoming the sole durable identity;
- raw reusable secrets enter Domain/client/events;
- Account/Workspace membership is being stored as Identity-owned lifecycle;
- OAuth business-provider connection is being moved into Identity only because it uses OAuth;
- API token scope is treated as universal permission bypass;
- deleting User would cascade-delete unrelated business history;
- session claims become permanent permission truth;
- identity merge is heuristic.

# 76. Related canonical owners

```text
docs/product/contexts/accounts.md
docs/product/contexts/workspaces.md
docs/product/contexts/governance.md
docs/product/contexts/integrations.md
docs/product/contexts/billing.md
docs/product/product-model.md
docs/architecture/contract-boundaries.md
docs/architecture/events-realtime-and-delivery-boundary.md
backend/docs/architecture/security-tenancy-authorization.md
```

# 77. Final Identity rule

For every Identity feature, answer:

```text
What principal/credential is this?
What proves authenticity?
What secret material exists?
What lifecycle invalidates it?
What sessions/tokens are affected?
What Account/Workspace relationship is separate?
What authorization still must happen later?
What audit/retention applies?
What happens on replay/concurrency/deletion?
```

The target is:

> **a stable authentication and security-identity boundary that proves actors strongly without becoming a catch-all permission, membership, billing, or business-resource model.**
