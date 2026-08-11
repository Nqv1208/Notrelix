---
title: "Identity Context"
document_class: constitution
normative: true
owner: identity
maturity: FROZEN
conformance: CANONICAL
applies_to: identity
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Identity Context

## Mission

Identity proves who/what is acting and manages authentication/security lifecycle: users, sessions, credentials, MFA/OAuth/SSO linkage and API/service tokens as supported. It does not decide whether an authenticated principal may perform a product operation.

## ID-101 — Authentication and authorization are separate

Successful authentication creates/proves a principal/session. Product access is determined later from Account/Workspace/Governance/entitlement/resource facts. Identity MUST NOT encode workspace role checks inside credential verification.

## ID-102 — Secret material never becomes ordinary Domain/client data

Passwords, refresh tokens, MFA seeds, OAuth/provider secrets and token signing material are handled by approved security/provider infrastructure. Domain contracts may represent credential identity/state/metadata, not expose raw secret values.

## ID-103 — Session/token lifecycle is explicit

Creation, refresh/rotation, expiration, revocation and security invalidation have defined semantics. Reusing a revoked credential/session must fail. Rotation must avoid accepting two independent long-lived authorities accidentally unless a bounded overlap window is intentional.

## ID-104 — External identity linking prevents accidental account takeover

OAuth/SSO linking validates provider/subject identity and collision rules in Application/Identity policy. An email string alone is not universal proof that two identities should merge. Sensitive linking/unlinking/credential changes generate security/audit evidence.

## ID-105 — API/service tokens are scoped identities

A token declares owner/subject, allowed scope/capabilities, lifecycle/expiry/revocation and non-recoverable secret handling. Store only verifiers/hashes where possible. Never log raw token values.

## Scope and authorization

Identity resources can be global/user/account-related depending on operation. The Identity context supplies stable principal identity/security facts; Governance/Workspace policy decides product/resource access. Session claims may cache hints but cannot become permanently authoritative when permissions/membership can change.

## Concurrency/idempotency

Credential/session revocation and provider callback handling tolerate retries. Provider correlation/state values are validated and one-time where required. Security-sensitive state changes use optimistic concurrency or equivalent protection against stale updates.

## Events

Emit durable events for security-relevant completed facts that need downstream action: session/credential revocation, security setting change, account-link change, etc. Do not put raw tokens/secrets in events. Realtime UX may notify session invalidation but server authorization remains authority.

## Deletion/retention

User identity deletion/anonymization must distinguish authentication record removal from business/audit records owned elsewhere. Never cascade-delete workspace/business history solely because a user can no longer authenticate; retention/anonymization policy controls references.

## Forbidden designs

- authorization inside login handler as substitute for product policy;
- plaintext/recoverable token secret persistence without approved need;
- using mutable email as the only durable cross-context user identity;
- provider SDK types in Domain/public product contracts;
- silent session survival after a security event meant to revoke it.

## Tests/change impact

Cover credential validation boundaries, rotation/revocation, replay, provider-link collision, secret non-exposure, security event/audit behavior and session invalidation. Changes impact account/workspace membership resolution, Governance authorization, frontend session bootstrap and integrations/SSO contracts.
