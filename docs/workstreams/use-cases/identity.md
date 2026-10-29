---
document_id: WRK-UC-IDENTITY
document_type: workstream-use-case-catalog
status: active
owner: identity-accounts-team
applies_to:
  - identity
  - use-case-catalog
  - capability-acceptance
evidence:
  - docs/workstreams/use-cases/README.md
  - docs/product/identity.md
  - docs/workstreams/teams/identity-accounts.md
  - docs/workstreams/executions/identity-accounts/identity-accounts.spec.md
  - docs/workstreams/executions/identity-accounts/identity-accounts.tests.md
  - docs/workstreams/executions/identity-accounts/identity-accounts.certification.md
  - backend/src/Notrelix.API/Endpoints/Identity/
  - backend/src/Notrelix.Application/Features/Identity/
  - backend/src/Notrelix.Domain/Identity/
  - frontend/packages/features/auth/
  - frontend/packages/features/account/
review_on:
  - authentication-model-change
  - session-lifecycle-change
  - credential-or-token-change
  - mfa-change
  - oauth-linking-change
  - api-token-scope-change
  - capability-state-change
---

# Use cases: Identity

Product meaning: [identity](../../product/identity.md). Team: [identity-accounts](../teams/identity-accounts.md). Execution pack: [spec](../executions/identity-accounts/identity-accounts.spec.md), [tests](../executions/identity-accounts/identity-accounts.tests.md), [certification](../executions/identity-accounts/identity-accounts.certification.md), [decision notes](../executions/identity-accounts/decisions/PR-IA-00-decision-notes.md). The Accounts context (account lifecycle, account administration, identity-provider configuration, SCIM directory, required-MFA policy) is in [accounts](accounts.md); this file links to it and does not repeat it.

Requirement ids cited are `IAREQ` ids of the spec: `IAREQ001` to `IAREQ005` (user), `IAREQ010`, `IAREQ011` (profile), `IAREQ012` to `IAREQ023` (authentication, sessions), `IAREQ040` to `IAREQ058` (OAuth, MFA, security), `IAREQ059` to `IAREQ067` (API tokens), `IAREQ079` to `IAREQ083` (API contract) and `IAREQ126` to `IAREQ130` (browser CSRF). Product rules are `ID-001` to `ID-018` of the product doc, quoted by section number.

# 1. Assessment

## 1.1 Capability map

Letters follow the Identity list in the team spec (section 4, Identity), in order. Registration, password recovery and the device list have no line of their own in the team spec; they are filed under the capability they serve (B, C).

| Letter | Capability (team spec) | Use cases | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | BLOCKED |
|---|---|---|---|---|---|---|---|
| A | Session bootstrap | 4 | 1 | 1 | 0 | 2 | 0 |
| B | Login (with registration, email confirmation, password recovery, company SSO) | 9 | 0 | 1 | 3 | 4 | 1 |
| C | Logout (with the device list) | 4 | 0 | 0 | 4 | 0 | 0 |
| D | Session expiration | 5 | 2 | 1 | 2 | 0 | 0 |
| E | OAuth start | 1 | 0 | 0 | 1 | 0 | 0 |
| F | OAuth callback | 2 | 0 | 1 | 1 | 0 | 0 |
| G | Provider linking and unlinking | 2 | 0 | 2 | 0 | 0 | 0 |
| H | MFA enrollment | 3 | 0 | 0 | 3 | 0 | 0 |
| I | MFA challenge | 2 | 0 | 0 | 2 | 0 | 0 |
| J | MFA recovery (with disable) | 5 | 0 | 0 | 3 | 0 | 2 |
| K | Security settings (with step-up, password, sign-in email) | 11 | 2 | 3 | 6 | 0 | 0 |
| L | API token lifecycle | 8 | 3 | 4 | 0 | 0 | 1 |
| M | Identity profile read and update | 5 | 0 | 0 | 5 | 0 | 0 |
| | Total | 61 | 8 | 13 | 30 | 6 | 4 |

No use case is DONE: none has a recorded owner check or a 1 px design diff yet. HARDENING means a `frontend/e2e/real` spec runs the flow against the real backend: the password sign-in (every real spec signs in through `frontend/e2e/real/support/real-session.ts`) and the sign-up with email confirmation (`email-confirmation.real.e2e.spec.ts`). Nothing else in Identity has a real end-to-end spec.

## 1.2 The ten most important gaps

1. **Password reset cannot be finished in the web app.** The backend sends a one-time code (`ForgotPassword.cs`, `ResetPassword.cs` take email and code); the web page says "link" and no route or surface takes the code. Only `createUseResetPassword` exists in the package (UC-IDN-B07).
2. **API token scopes are never enforced.** Tokens are issued with `scopes: null` and the authentication handler builds a principal for the issuing user with the workspace claim; no code reads `ApiTokenScopes.Allows`. ID-010 says a token is never "full user access" (UC-IDN-L05).
3. **No API token UI.** Three endpoints exist, no screen (UC-IDN-L01 to L03).
4. **Company single sign-on is a stub.** `IdentifySignIn` always answers "password"; the mock backend invents an SSO path that the real backend cannot serve (UC-IDN-B08).
5. **Refresh has no replay detection and no absolute lifetime.** A replayed rotated credential simply fails; the family stays valid, and each refresh creates a new 30-day session, so a stolen chain lives forever (UC-IDN-D03, D05).
6. **Disabling a person does not end their access.** Domain `Deactivate` and `Suspend` exist; no use case calls them; sessions and access tokens are not revoked, only a refresh is blocked (UC-IDN-D04, L08).
7. **Revoke-other-sessions has no step-up** although owner decision 48 requires one (UC-IDN-C04).
8. **Changing the sign-in email needs no proof.** No step-up, no notice to the old address, no session revocation, and no web screen (UC-IDN-K09).
9. **Provider link and unlink have no screen**, and the security page cannot show or change linked providers (UC-IDN-G01, G02).
10. **No mobile token transport** (owner decision 47, `X-Notrelix-Client: mobile`): the backend does not read the header, and refresh reads only the cookie (UC-IDN-A04).

## 1.3 Defects in shipped code

| Defect | Where | Use case |
|---|---|---|
| Reset flow mismatch: email carries a code, the page promises a link, no reset page | `backend/src/Notrelix.Application/Features/Identity/Auth/Commands/ForgotPassword/ForgotPassword.cs`; `frontend/packages/features/auth/src/web/components/auth-forgot-surface.tsx`; `frontend/apps/web/src/routes/` | B07 |
| Forgot-password answers an error only when the account exists and the mail fails to send: a side channel that names real accounts | `ForgotPassword.cs` (`email-send-failed` branch) | B06 |
| Register answers "Email is already in use" (accepted trade-off, owner sign-off pending) | `backend/src/Notrelix.Application/Features/Identity/Registration/Commands/Register/RegisterCommandHandler.cs` | B03 |
| Same disclosure on email change, to a signed-in person | `Profiles/Commands/UpdateEmail/UpdateEmail.cs` | K09 |
| Email change: no step-up, no notice to the old address, login email changes before the new one is proven | `UpdateEmail.cs`; purpose `ChangeSecurityIdentity` exists in `Security/DTOs/StepUpTypes.cs` and nothing uses it | K09 |
| Revoke-others needs no step-up (owner decision 48) | `Sessions/Commands/RevokeOtherSessions/RevokeOtherSessions.cs` | C04 |
| API token scopes are not enforced; `scopes: null` at issue; expiry optional (a token may never expire) | `ApiTokens/Commands/CreateApiToken/CreateApiTokenCommand.cs`; `backend/src/Notrelix.Infrastructure/Auth/ApiTokens/ApiTokenAuthenticationHandler.cs` | L02, L04, L05 |
| Unbounded reads: the session list returns every session ever created, revoked and expired included; the token list has no page | `Sessions/Queries/GetUserSessions/GetUserSessionsQuery.cs`; `ApiTokens/Queries/ListApiTokens/ListApiTokensQuery.cs` | C02, L01 |
| Refresh: no reuse detection, a new 30-day session per refresh (sliding forever), the session id changes at every refresh | `Auth/Commands/RefreshToken/RefreshToken.cs` | D01, D03, D05 |
| Refresh reads only the cookie; the mobile bearer transport of decision 47 is absent | `backend/src/Notrelix.API/Endpoints/Identity/Auth/Commands/RefreshTokenEndpoint.cs` | A04 |
| Dead code: `IdentifySignInQueryHandler` always returns password; user disable and delete exist in Domain with no Application caller | `Auth/Queries/IdentifySignIn/IdentifySignIn.cs`; `backend/src/Notrelix.Domain/Identity/Users/User.cs` | B08, D04 |
| Mock-only frontend paths: company SSO round trip, security flows against `account-security.handlers.ts` | `frontend/packages/dev/mock-backend/src/contexts/account/` | B08 |
| Endpoints with no explicit rate-limit policy attribute: `/auth/logout`, `/auth/mfa/enrollment/start`, `/auth/mfa/disable`, `/auth/mfa/recovery-codes/regenerate`, `/auth/email-verification/confirm`, `/auth/email-verification/request`, `/auth/me`, `/auth/bootstrap`, `/profile/*` | `backend/src/Notrelix.API/Endpoints/Identity/Auth/Commands/` | J02, J03, H01, B04 |
| No Application-level tests for the MFA, session and OAuth handlers (only Domain, Infrastructure and a few API tests) | `backend/tests/Notrelix.Application.Tests/Features/Identity/` | H, I, J, C, E to G |
| No expected version on the security writes (disable MFA, revoke token, unlink). They are idempotent single-owner writes; ID-015 asks for protection where a stale write could overwrite newer state | `Mfa/Commands/DisableMfa/`, `ApiTokens/Commands/RevokeApiToken/` | J03, L03 |

## 1.4 Enterprise baseline

Compared with Auth0 refresh-token rotation ([Auth0](https://auth0.com/docs/secure/tokens/refresh-tokens/refresh-token-rotation)), Okta ([Okta](https://developer.okta.com/docs/guides/refresh-tokens/refresh-token-rotation/)), Notion SAML and SCIM ([Notion](https://www.notion.com/help/saml-sso-configuration)), Slack with Microsoft Entra provisioning ([Microsoft](https://learn.microsoft.com/en-ca/Entra/Identity/saas-apps/slack-provisioning-tutorial)) and GitHub fine-grained tokens ([GitHub](https://docs.github.com/en/enterprise-server@3.22/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens)).

| Table stake | Leaders | Notrelix today | Use case |
|---|---|---|---|
| Reuse detection on a rotated refresh credential, with a grace window; revoke the family | Auth0, Okta | none | D03 |
| Absolute session lifetime and idle limit, set by policy | Auth0, Notion Enterprise | sliding 30 days, no cap | D05 |
| SAML or OIDC single sign-on per verified domain, enforced | Notion, Slack | stub | B08 |
| SCIM deprovisioning ends sessions and tokens | Slack, Entra | Accounts holds a SCIM directory model; Identity has no deprovision effect | D04, L08 |
| Required MFA by policy, per account | Slack, Notion Enterprise | no policy fact | J05 |
| Admin or recovery path when the second factor is lost | all | none | J04 |
| Scoped, expiring, creator-bound tokens; last-used shown; a maximum lifetime set by the owner | GitHub | creator-bound only | L05 to L08 |
| New-device and security-change notice by email | all | none | K10 |
| Password set for a provider-only account | Slack, Notion | not offered | K08 |
| Passkeys (WebAuthn) | Auth0, Okta, GitHub | none | not covered, Part 6 |

# 2. Index

Design column: `Final-Auth` (1440), `Final-M-Auth` (390), `Final-Account` (1440), `Final-M-Account` (390) are the artboards of the design canvas (README section 7). "none" means no artboard draws the screen.

| ID | Use case | Capability | Actors | BE | FE | Design | Real e2e | State | Gap |
|---|---|---|---|---|---|---|---|---|---|
| UC-IDN-A01 | Read the signed-in person | A | Signed-in person | yes | yes | Final-Auth | yes (sign-in) | HARDENING | no 1 px diff, no owner check |
| UC-IDN-A02 | Bootstrap read for a client | A | Signed-in person | yes | no | none | no | IMPLEMENTING | no caller, no consumer decision |
| UC-IDN-A03 | Get and send the CSRF token | A | Browser client | yes | yes | n/a | yes (all writes) | HARDENING | production deploy smoke |
| UC-IDN-A04 | Mobile token transport | A | Mobile client | no | no | Final-M-Auth | no | DEFINED | header and body transport absent |
| UC-IDN-B01 | Identify the sign-in method | B | Visitor | yes | yes | Final-Auth | yes | HARDENING | always answers password |
| UC-IDN-B02 | Sign in with email and password | B | Visitor | yes | yes | Final-Auth | yes | HARDENING | no 1 px diff, no owner check |
| UC-IDN-B03 | Register | B | Visitor | yes | yes | Final-Auth | yes | HARDENING | email disclosure open |
| UC-IDN-B04 | Confirm the email from the link | B | Visitor, signed-in person | yes | yes | Final-Auth | yes | HARDENING | no rate-limit attribute |
| UC-IDN-B05 | Request or resend the confirmation | B | Visitor, signed-in person | yes | yes | Final-Auth | no | INTEGRATING | no real e2e |
| UC-IDN-B06 | Ask for a password reset | B | Visitor | yes | yes | Final-Auth | no | INTEGRATING | error side channel, no real e2e |
| UC-IDN-B07 | Complete a password reset | B | Visitor | yes | no | none | no | IMPLEMENTING | no reset screen, code versus link |
| UC-IDN-B08 | Sign in through the company provider | B | Visitor | no | mock only | Final-Auth | no | BLOCKED | Accounts enforcement fact, protocol |
| UC-IDN-B09 | Lock sign-in after repeated failures | B | Visitor | yes | yes | Final-Auth | no | INTEGRATING | no real e2e |
| UC-IDN-C01 | Sign out this device | C | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | no real e2e |
| UC-IDN-C02 | List my sessions and devices | C | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | unbounded list |
| UC-IDN-C03 | Sign out one other device | C | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | no real e2e |
| UC-IDN-C04 | Sign out all other devices | C | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | no step-up |
| UC-IDN-D01 | Refresh the session silently | D | Signed-in client | yes | yes | n/a | no | INTEGRATING | session id changes each refresh |
| UC-IDN-D02 | Session expired or revoked | D | Signed-in person | yes | yes | Final-States | no | INTEGRATING | no real e2e |
| UC-IDN-D03 | Reject a replayed refresh credential | D | System | no | n/a | n/a | no | DEFINED | no reuse detection |
| UC-IDN-D04 | End access when a person is disabled | D | System | partial | no | none | no | IMPLEMENTING | no caller, no revocation |
| UC-IDN-D05 | Bound the absolute session lifetime | D | System | no | n/a | n/a | no | DEFINED | sliding forever |
| UC-IDN-E01 | Start Google or Microsoft sign-in | E | Visitor | yes | yes | Final-Auth | no | INTEGRATING | provider needs credentials |
| UC-IDN-F01 | Complete provider sign-in | F | Visitor | yes | yes | Final-Auth | no | INTEGRATING | no real e2e |
| UC-IDN-F02 | Refuse a stale, replayed or colliding callback | F | Visitor | yes | partial | none | no | IMPLEMENTING | FE error states |
| UC-IDN-G01 | Link a provider to my account | G | Signed-in person | yes | no | none | no | IMPLEMENTING | no screen |
| UC-IDN-G02 | Unlink a provider | G | Signed-in person | yes | no | none | no | IMPLEMENTING | no screen |
| UC-IDN-H01 | Start second-factor enrollment | H | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | no real e2e |
| UC-IDN-H02 | Verify and activate the second factor | H | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | no real e2e |
| UC-IDN-H03 | Read the second-factor status | H | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | no real e2e |
| UC-IDN-I01 | Answer the second step at sign-in | I | Visitor | yes | yes | Final-Auth | no | INTEGRATING | no real e2e |
| UC-IDN-I02 | Expire a challenge after wrong codes | I | Visitor | yes | yes | Final-Auth | no | INTEGRATING | no real e2e |
| UC-IDN-J01 | Sign in with a recovery code | J | Visitor | yes | yes | Final-Auth | no | INTEGRATING | no real e2e |
| UC-IDN-J02 | Regenerate recovery codes | J | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | no rate-limit attribute |
| UC-IDN-J03 | Turn the second factor off | J | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | blocked-by-policy case |
| UC-IDN-J04 | Recover an account that lost both factors | J | Person, Account admin | no | no | none | no | BLOCKED | who may reset another person |
| UC-IDN-J05 | Enforce an account-required second factor | J | System | no | no | none | no | BLOCKED | Accounts policy fact |
| UC-IDN-K01 | Read my security summary | K | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | no real e2e |
| UC-IDN-K02 | Read my security activity | K | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | no real e2e |
| UC-IDN-K03 | Ask which step-up factor applies | K | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | no real e2e |
| UC-IDN-K04 | Step up with a second-factor code | K | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | no real e2e |
| UC-IDN-K05 | Step up with the password | K | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | no real e2e |
| UC-IDN-K06 | Step up with a linked provider | K | Signed-in person | yes | no | none | no | IMPLEMENTING | no screen |
| UC-IDN-K07 | Change my password | K | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | no real e2e |
| UC-IDN-K08 | Set a first password (enterprise baseline) | K | Signed-in person | no | no | none | no | DEFINED | provider-only accounts |
| UC-IDN-K09 | Change my sign-in email | K | Signed-in person | yes | no | none | no | IMPLEMENTING | no proof, no screen |
| UC-IDN-K10 | Notify me of security changes (enterprise baseline) | K | System | no | no | none | no | DEFINED | no mail |
| UC-IDN-K11 | Operator audit of identity security changes | K | Auditor | partial | n/a | n/a | no | IMPLEMENTING | owner is Governance audit |
| UC-IDN-L01 | List workspace API tokens | L | Workspace Owner or Admin | yes | no | none | no | IMPLEMENTING | no screen, unbounded |
| UC-IDN-L02 | Issue an API token | L | Workspace Owner or Admin | yes | no | none | no | IMPLEMENTING | no screen, optional expiry |
| UC-IDN-L03 | Revoke an API token | L | Workspace Owner or Admin | yes | no | none | no | IMPLEMENTING | no screen |
| UC-IDN-L04 | Call the API with a token | L | Integration client | yes | n/a | n/a | no | IMPLEMENTING | scopes not enforced |
| UC-IDN-L05 | Limit a token to chosen scopes | L | Workspace Owner or Admin | no | no | none | no | BLOCKED | scope vocabulary |
| UC-IDN-L06 | Cap token lifetime (enterprise baseline) | L | System | no | no | none | no | DEFINED | rule not coded |
| UC-IDN-L07 | Show when a token was last used (enterprise baseline) | L | Workspace Owner or Admin | no | no | none | no | DEFINED | no column |
| UC-IDN-L08 | Stop tokens of a person who leaves | L | System | no | n/a | n/a | no | DEFINED | no rule |
| UC-IDN-M01 | Change my display name | M | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | no real e2e |
| UC-IDN-M02 | Read and save my preferences | M | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | no real e2e |
| UC-IDN-M03 | Upload my profile photo | M | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | no real e2e |
| UC-IDN-M04 | Remove my profile photo | M | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | no real e2e |
| UC-IDN-M05 | Read a person's photo | M | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | no real e2e |

# 3. Use cases

Common facts, stated once. Every authenticated endpoint is `MapAuthenticated*`; a missing or expired session answers 401. Every unsafe request carries the CSRF header (UC-IDN-A03). Evidence paths are relative: `BE` under `backend/src/Notrelix.Application/Features/Identity/` unless it starts with `Notrelix.`; `API` under `backend/src/Notrelix.API/Endpoints/Identity/`; `FE` under `frontend/`; tests under `backend/tests/Notrelix.<Layer>.Tests/`. Real e2e: only `email-confirmation.real.e2e.spec.ts` and the sign-in helper `real-session.ts` exist; "none" means no spec. Owner checks and 1 px diffs are missing for every use case, so the State lines say only what else is missing.

## Capability A: Session bootstrap

### UC-IDN-A01 Read the signed-in person
- Capability: A Session bootstrap
- Actors and permission: any signed-in person; authentication only (ID-012), no Governance permission.
- Preconditions: a valid session cookie or token.
- Input: none.
- Output: id, email, name, avatar address, email-confirmed flag of the caller only.
- Main flow: 1. The client calls `GET /api/v1/auth/me` on start and after a sign-in. 2. The server resolves the user from the session. 3. The client holds the answer as the principal and routes on.
- No-op / rejection / concurrency / retry: no session or a revoked session answers 401 `identity.*` problem; a deleted user answers the same 401; a read, so retry is safe.
- UI states: loading (bootstrap), error, session expired. No empty, no conflict.
- Rules: ID-012, section 58 and 59; IAREQ018.
- Acceptance: Given a signed-in person, when the client reads `/auth/me`, then only their own fields return and no workspace role or entitlement is in the answer. Given a revoked session, when the client reads it, then the answer is 401 and the app shows the sign-in page.
- Evidence: BE `Auth/Queries/GetCurrentUser`; API `Auth/Queries/GetCurrentUserEndpoint.cs` (`GET /api/v1/auth/me`) · FE `packages/features/auth/src/web/hooks/use-auth-user.ts` · Design Final-Auth · Tests Application `GetCurrentUserTests`, API `AuthenticatedAuthEndpointTests` · Real e2e `support/real-session.ts` (through every real spec)
- State: HARDENING. Missing: one real spec that asserts the revoked-session answer, 1 px diff, owner check.

### UC-IDN-A02 Bootstrap read for a client
- Capability: A Session bootstrap
- Actors and permission: any signed-in person; authentication only.
- Preconditions: a valid session.
- Input: none.
- Output: the user bootstrap result (identity facts for first paint).
- Main flow: 1. A client calls `GET /api/v1/auth/bootstrap`. 2. The server returns the caller's bootstrap facts.
- No-op / rejection / concurrency / retry: 401 without a session; user not found answers a failure; read, safe to retry.
- UI states: loading, error.
- Rules: IAREQ018; ID-017 (no current workspace in identity truth).
- Acceptance: Given a signed-in person, when the client reads bootstrap, then it gets no workspace role and no entitlement. Given no session, then 401.
- Evidence: BE `Auth/Queries/GetBootstrap/GetBootstrap.cs`; API `Auth/Queries/GetBootstrapEndpoint.cs` (`GET /api/v1/auth/bootstrap`) · FE none (no caller found; the web app reads `/auth/me`) · Design none · Tests Application `GetBootstrapTests`, Integration `GetBootstrapQueryHandlerTests` · Real e2e none
- State: IMPLEMENTING. Missing: decide whether a client needs it; if not, delete the endpoint (dead code); if yes, build the consumer.

### UC-IDN-A03 Get and send the CSRF token
- Capability: A Session bootstrap
- Actors and permission: any browser client; public endpoint.
- Preconditions: none.
- Input: none.
- Output: a `csrf_token` cookie and the token value; the client then sends header `X-CSRF-Token` on every unsafe request (ADR-005 double submit).
- Main flow: 1. The client calls `GET /api/v1/auth/csrf` before its first unsafe request. 2. It stores nothing in script storage. 3. It sends the header on POST, PUT, PATCH, DELETE. 4. On `security.csrf_validation_failed` it fetches a new token once and retries once.
- No-op / rejection / concurrency / retry: a missing or wrong header answers 403 `security.csrf_validation_failed`; recovery is bounded to one retry; the endpoint is rate limited by `CsrfByIp`.
- UI states: none visible; a repeated failure shows the generic error.
- Rules: IAREQ126 to IAREQ130; team spec section 13 (resolved).
- Acceptance: Given a browser with no token, when it posts without the header, then 403 and no change. Given a stale token, when the client re-fetches and retries once, then the write succeeds once.
- Evidence: API `Auth/Commands/IssueCsrfTokenEndpoint.cs` (`GET /api/v1/auth/csrf`) · FE `packages/foundation/contracts/src/client/` (`csrf-transport`) · Design n/a · Tests API `CsrfProtectionTests`, `CsrfCrossStackFlowTests`, `CsrfEnforcementDefaultsTests`; FE `csrf-transport.unit.test.ts` · Real e2e every real spec writes through the transport
- State: HARDENING. Missing: staging and production deployment smoke (certification item P14-MIG-003).

### UC-IDN-A04 Mobile token transport
- Capability: A Session bootstrap
- Actors and permission: mobile client; public sign-in endpoints.
- Preconditions: header `X-Notrelix-Client: mobile` on the request.
- Input: sign-in, refresh with the refresh token in the body.
- Output: access and refresh tokens in the response body; no cookies.
- Main flow: 1. The mobile app signs in with the header. 2. The server returns tokens in the body. 3. The app stores them in Keychain or Keystore only. 4. Refresh sends the body token and gets a rotated one.
- No-op / rejection / concurrency / retry: without the header the web rules apply (cookie and CSRF); a mobile call needs no CSRF header; a replayed refresh fails (UC-IDN-D03); push and SSO via system browser are deferred.
- UI states: not a web use case.
- Rules: IAREQ126; product decision below.
- Acceptance: Given the header, when the app signs in, then the body holds the tokens and no cookie is set. Given no header, then behaviour is unchanged.
- Evidence: BE none; API none (`RefreshTokenEndpoint.cs` reads only the cookie) · FE none · Design Final-M-Auth · Tests none · Real e2e none
- State: DEFINED. Missing: everything. Product decision (owner, 2026-10-27): mobile uses a bearer and a rotating refresh token in the body selected by `X-Notrelix-Client: mobile`; web stays cookie plus CSRF.

## Capability B: Login

### UC-IDN-B01 Identify the sign-in method
- Capability: B Login
- Actors and permission: any visitor; public, rate limit `AuthCredentialByIp`.
- Preconditions: none.
- Input: email (required, at most 256 characters, well formed).
- Output: `{ method: "password" | "sso", sso? }`.
- Main flow: 1. The person types an email and presses Continue. 2. The page asks `POST /api/v1/auth/identify`. 3. On "password" the password field opens in place (DESIGN section 39.4).
- No-op / rejection / concurrency / retry: the answer depends on the text of the email only, never on whether an account exists (no enumeration, no timing difference); a malformed email is a validation error; retry is safe. Today every email answers "password" (see UC-IDN-B08).
- UI states: validation error, pending, rate limited.
- Rules: ID-009; section 67 (generic answers); IAREQ014.
- Acceptance: Given a known and an unknown email, when each is identified, then the two answers are identical. Given a malformed email, then a validation error.
- Evidence: BE `Auth/Queries/IdentifySignIn/IdentifySignIn.cs`; API `Auth/Queries/IdentifySignInEndpoint.cs` (`POST /api/v1/auth/identify`) · FE `packages/features/auth/src/web/components/auth-sign-in-surface.tsx` · Design Final-Auth · Tests Application `IdentifySignInTests`; mock e2e `auth-flow.mock.e2e.spec.ts` · Real e2e `support/real-session.ts`
- State: HARDENING. Missing: SSO branch (B08), 1 px diff, owner check.

### UC-IDN-B02 Sign in with email and password
- Capability: B Login
- Actors and permission: any visitor; public, `AuthCredentialByIp`.
- Preconditions: an Active account with a password credential.
- Input: email, password.
- Output: session cookies (access and refresh); or a second-step challenge cookie when MFA is enabled (UC-IDN-I01); a security-activity line; no Account or Workspace fact.
- Main flow: 1. The person submits the in-place form. 2. The server checks the lockout counters, then the credential. 3. On success it creates a session, records "sign-in succeeded" and sets cookies. 4. The client reads `/auth/me` and routes to the last workspace.
- No-op / rejection / concurrency / retry: wrong credential, unknown account, locked account and inactive account answer the same generic failure (IAREQ014); a locked attempt is not verified even with the right password (UC-IDN-B09); a retry makes a new session, not a second effect on the account; an unconfirmed email still signs in (confirmation gates workspace creation, see UC-IDN-B03).
- UI states: pending, validation error, generic failure, locked with time left, second step.
- Rules: ID-001, ID-012; section 38; IAREQ012 to IAREQ014, IAREQ017.
- Acceptance: Given valid credentials, when the person signs in, then a session exists and one "sign-in succeeded" line is recorded. Given a wrong password and an unknown email, then the responses are identical.
- Evidence: BE `Auth/Commands/Login/Login.cs`; API `Auth/Commands/LoginEndpoint.cs` (`POST /api/v1/auth/login`) · FE `packages/features/auth/src/web/hooks/use-login.ts`, route `apps/web/src/routes/sign-in.tsx` · Design Final-Auth, Final-M-Auth · Tests Application `LoginTests`, API `LoginErrorContractTests`, `PublicAuthEndpointTests` · Real e2e `support/real-session.ts`
- State: HARDENING. Missing: real spec for the rejection (wrong password), 1 px diff, owner check.

### UC-IDN-B03 Register
- Capability: B Login
- Actors and permission: any visitor; public, `AuthCredentialByIp`.
- Preconditions: the email is not registered.
- Input: first name, last name (joined into a name of at most 200), email, password (at least 8 characters, `PasswordPolicy`), terms accepted.
- Output: a new User (Active, email unconfirmed), a session, a "sign-in succeeded" line, a confirmation email, an `identity.registration-completed` fact.
- Main flow: 1. The person fills the form. 2. The server creates the User and the session. 3. The client goes to onboarding; creating a workspace is refused until the email is confirmed (UC-IDN-B04).
- No-op / rejection / concurrency / retry: an existing email answers "Email is already in use" (accepted trade-off, see Part 4); two simultaneous registrations of one email create one User (race test); a weak password is a validation error.
- UI states: pending, validation error, email in use, rate limited.
- Rules: ID-002, ID-004; IAREQ001, IAREQ002, IAREQ015.
- Acceptance: Given a new email, when the person registers, then one User and one session exist and the confirmation mail is logged. Given an existing email, then no second User.
- Evidence: BE `Registration/Commands/Register/RegisterCommandHandler.cs`; API `Auth/Commands/RegisterEndpoint.cs` (`POST /api/v1/auth/register`) · FE `packages/features/auth/src/web/hooks/use-register.ts`, route `sign-up.tsx` · Design Final-Auth · Tests Application `RegisterTests`, Integration `RegisterDuplicateRace` (decision notes), Application `IdentityRegistrationCompletedIntegrationEventTests` · Real e2e `email-confirmation.real.e2e.spec.ts`
- State: HARDENING. Missing: owner decision on the email disclosure, 1 px diff, owner check.

### UC-IDN-B04 Confirm the email from the link
- Capability: B Login
- Actors and permission: any visitor holding the link token; public.
- Preconditions: an unused, unexpired verification token.
- Input: token (in the URL fragment).
- Output: email confirmed; the token consumed.
- Main flow: 1. The person opens `/verify-email#token=...`. 2. The page posts `POST /api/v1/auth/email-verification/confirm`. 3. The page says the email is confirmed and offers to continue.
- No-op / rejection / concurrency / retry: a used, expired or unknown token answers the same "This link no longer works" (single use, ID section 15); a second use of the link changes nothing; confirming an already confirmed email is a no-op.
- UI states: pending, success, link no longer works.
- Rules: ID sections 14 and 15; IAREQ015.
- Acceptance: Given a fresh link, when it is opened twice, then the first confirms and the second shows the dead-link state.
- Evidence: BE `Verification/Commands/ConfirmEmail/ConfirmEmail.cs`; API `Auth/Commands/EmailVerificationEndpoints.cs` (`POST /api/v1/auth/email-verification/confirm`) · FE `packages/features/auth/src/web/components/auth-verify-email-surface.tsx`, route `verify-email.tsx` · Design Final-Auth · Tests Application `ConfirmEmailTests`; Domain `EmailVerificationTokenTests` · Real e2e `email-confirmation.real.e2e.spec.ts`
- State: HARDENING. Missing: rate-limit policy on the endpoint, 1 px diff, owner check.

### UC-IDN-B05 Request or resend the confirmation
- Capability: B Login
- Actors and permission: a signed-in person (request) or a visitor with the email (resend); `AuthCredentialByIp` on resend.
- Preconditions: the email is unconfirmed.
- Input: none (request); email (resend).
- Output: a new confirmation mail; earlier active tokens are replaced.
- Main flow: 1. The onboarding notice offers "Send again". 2. The client posts `/auth/email-verification/request` (signed in) or `/resend`. 3. The page says a mail was sent and waits before the next send.
- No-op / rejection / concurrency / retry: the resend answer is generic for unknown, confirmed and unconfirmed emails; a confirmed email sends nothing; resend is throttled; only the newest token works.
- UI states: pending, sent with countdown, rate limited.
- Rules: ID sections 14 and 15, 67.
- Acceptance: Given an unconfirmed account, when the person resends twice, then only the latest link confirms. Given an unknown email, then the answer equals the known case.
- Evidence: BE `Verification/Commands/RequestEmailVerification`, `ResendEmailVerification`; API `Auth/Commands/EmailVerificationEndpoints.cs` (`POST /api/v1/auth/email-verification/request`, `POST /api/v1/auth/email-verification/resend`) · FE `packages/features/auth/src/core/api/auth-flow.service.ts` · Design Final-Auth · Tests Application `RequestEmailVerificationTests`, `ResendEmailVerificationTests` · Real e2e none
- State: INTEGRATING. Missing: real spec, rate-limit attribute on `request`.

### UC-IDN-B06 Ask for a password reset
- Capability: B Login
- Actors and permission: any visitor; public, `AuthCredentialByIp`.
- Preconditions: none.
- Input: email.
- Output: if the account exists, a one-time code mail; always the same success answer.
- Main flow: 1. The person chooses "Forgot password" from the password step. 2. The page posts `POST /api/v1/auth/forgot-password`. 3. The page says "If the email has an account, a message is on its way" with a resend countdown.
- No-op / rejection / concurrency / retry: unknown email returns success with no mail; at most 3 requests per email per hour, then `identity.auth.rate-limited`; defect: a mail send failure answers an error only for real accounts.
- UI states: pending, sent with countdown, rate limited.
- Rules: ID sections 13 and 66; IAREQ014.
- Acceptance: Given a known and an unknown email, when each is submitted, then the answer is identical, including when the mail provider fails.
- Evidence: BE `Auth/Commands/ForgotPassword/ForgotPassword.cs`; API `Auth/Commands/ForgotPasswordEndpoint.cs` (`POST /api/v1/auth/forgot-password`) · FE `packages/features/auth/src/web/components/auth-forgot-surface.tsx`, route `forgot-password.tsx` · Design Final-Auth · Tests Application `ForgotPasswordTests`; mock e2e `auth-flow.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING. Missing: close the send-failure side channel, real spec.

### UC-IDN-B07 Complete a password reset
- Capability: B Login
- Actors and permission: any visitor who holds the one-time code; public, `AuthCredentialByIp`.
- Preconditions: a code issued by UC-IDN-B06, unexpired and unused.
- Input: email, code, new password (policy as in B03).
- Output: new password set; every active session and access token issued before is revoked; the account's sign-in lockout is lifted, not the address lockout; a "password reset" line is recorded.
- Main flow: 1. The person opens the reset screen from the mail. 2. They enter code and new password. 3. The server consumes the code and sets the password. 4. The page sends them to sign in.
- No-op / rejection / concurrency / retry: a wrong, used or expired code fails the same way; a second use of the code has no effect; five wrong codes kill the code (lead decision below).
- UI states: pending, validation error, code no longer works, success.
- Rules: ID sections 13, 15, 20; ID-006; IAREQ014, IAREQ021.
- Acceptance: Given a valid code, when the person resets, then old sessions end and the code fails on reuse. Given a stale code, then no change and the generic answer.
- Evidence: BE `Auth/Commands/ResetPassword/ResetPassword.cs`; API `Auth/Commands/ResetPasswordEndpoint.cs` (`POST /api/v1/auth/reset-password`) · FE `packages/features/auth/src/web/hooks/use-reset-password.ts` (hook only; no route or surface) · Design none · Tests Application `ResetPasswordTests`; Domain `PasswordResetTokenTests` · Real e2e none
- State: IMPLEMENTING. Missing: the reset screen (code entry), a design artboard, real spec. Lead decision (2026-10-30): the mail carries a code, the screen takes email, code and new password; the "link" wording in the web copy is corrected to "code".

### UC-IDN-B08 Sign in through the company provider
- Capability: B Login
- Actors and permission: visitor whose email domain an Account verified; public.
- Preconditions: an Account fact says the domain is verified and the identity provider enforces sign-in (see [accounts](accounts.md)).
- Input: email; the provider's callback (OIDC authorization code with state, nonce, PKCE).
- Output: a session, exactly as UC-IDN-B02; just-in-time User creation only when the Account allows it.
- Main flow: 1. Identify answers "sso" with the company name. 2. The person continues to the provider. 3. The callback creates or finds the User by provider subject (ID-008) and opens a session. 4. Password sign-in is refused for the domain.
- No-op / rejection / concurrency / retry: stale state or replayed callback fails (as F02); a provider subject already bound to another User is refused; email-only merge is never done.
- UI states: pending, redirecting, provider refused, domain not enforced.
- Rules: ID-009; sections 30 and 31; IAREQ040 to IAREQ047.
- Acceptance: Given an enforced domain, when the person identifies, then the provider path is the only path. Given a replayed callback, then no second session.
- Evidence: BE `Auth/Queries/IdentifySignIn/IdentifySignIn.cs` (stub, always password); Domain `backend/src/Notrelix.Domain/Accounts/IdentityProviders/AccountIdentityProvider.cs` (Draft, Active, Disabled; no enforcement flag) · FE mock path in `packages/dev/mock-backend/src/contexts/account/` · Design Final-Auth (company provider step) · Tests mock e2e `auth-flow.mock.e2e.spec.ts` · Real e2e none
- State: BLOCKED. Waits on the Accounts decision that adds the enforcement fact and the verified-domain rule (Part 5), and on the protocol choice (Part 4).

### UC-IDN-B09 Lock sign-in after repeated failures
- Capability: B Login
- Actors and permission: any visitor; applies to every credential check.
- Preconditions: none.
- Input: repeated wrong credentials.
- Output: a temporary lock per account and per client address.
- Main flow: 1. Ten wrong credentials for one account in 15 minutes lock that account for 15 minutes. 2. Fifty from one address in 15 minutes lock the address for 15 minutes. 3. While locked, even the right password is not verified. 4. The button shows the time left.
- No-op / rejection / concurrency / retry: the answer is identical for known and unknown accounts; no answer tells how many attempts remain; a successful password reset lifts the account lock, never the address lock; no administrator unlock is offered; the same lock applies to re-verification by step-up and password change.
- UI states: locked with time left, generic failure.
- Rules: ID section 67 (D10, accepted 2026-10-23); IAREQ014.
- Acceptance: Given ten wrong passwords, when the right one follows within the lock, then it is refused with the generic lock answer. Given a reset, then the account signs in again.
- Evidence: BE `Auth/Throttling/` (`ILoginThrottle`), `Auth/Commands/Login/Login.cs` · FE `packages/features/auth/src/web/components/auth-sign-in-surface.tsx` · Design Final-Auth · Tests Domain `UserLoginAttemptTests`, Application `SecurityStepUpServiceThrottleTests`, `LoginTests`; mock e2e `auth-flow.mock.e2e.spec.ts` ("five wrong passwords lock") · Real e2e none
- State: INTEGRATING. Missing: real spec; the mock e2e locks after five attempts while the product value is ten, so the mock differs from the rule.

## Capability C: Logout

### UC-IDN-C01 Sign out this device
- Capability: C Logout
- Actors and permission: signed-in person; own session only.
- Preconditions: a session.
- Input: none.
- Output: the session is revoked, cookies cleared, access credentials of the session refused at once.
- Main flow: 1. The person chooses Sign out. 2. The client posts `POST /api/v1/auth/logout`. 3. The app returns to the sign-in page and drops all cached server state.
- No-op / rejection / concurrency / retry: logging out an already revoked session is a no-op success; revoked state is never revived (ID-005).
- UI states: pending, signed out.
- Rules: ID-005; sections 16 to 19; IAREQ020.
- Acceptance: Given a session, when the person signs out, then the next call with the old cookies is 401. Given a second logout, then success and no second activity line.
- Evidence: BE `Auth/Commands/Logout/Logout.cs`; API `Auth/Commands/LogoutEndpoint.cs` (`POST /api/v1/auth/logout`) · FE `packages/features/auth/src/web/hooks/use-logout.ts` · Design Final-Account · Tests Application `LogoutTests` · Real e2e none
- State: INTEGRATING. Missing: real spec, rate-limit attribute not needed (own session), 1 px diff.

### UC-IDN-C02 List my sessions and devices
- Capability: C Logout
- Actors and permission: signed-in person; own rows only.
- Preconditions: none.
- Input: none.
- Output: per session: id, status, created and last-seen time, client address, user agent, "this device" flag.
- Main flow: 1. The security page reads `GET /api/v1/auth/sessions`. 2. It shows active sessions, newest first, with a description of the device.
- No-op / rejection / concurrency / retry: another person's session never appears and a foreign id is indistinguishable from an unknown id; read, safe to retry.
- UI states: loading, empty (only this device), success, error.
- Rules: section 67 (account security facts); owner decision 48 (a device is a session; address and user agent only); IAREQ023.
- Acceptance: Given two signed-in devices, when the list is read, then both show and one is marked current. Given another person, then none of my rows appear.
- Evidence: BE `Sessions/Queries/GetUserSessions/GetUserSessionsQuery.cs`; API `Auth/Commands/SessionEndpoints.cs` (`GET /api/v1/auth/sessions`) · FE `apps/web/src/routes/workspaces/$workspaceId/account/security.tsx` · Design Final-Account · Tests Domain `UserSessionTests`; Integration `UserScopeIsolationTests`; mock e2e `account-pages.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING. Missing: the list is unbounded (return active sessions and the last 20 ended ones), real spec.

### UC-IDN-C03 Sign out one other device
- Capability: C Logout
- Actors and permission: signed-in person; own session; no step-up (lead decision).
- Preconditions: the target session is the caller's and Active.
- Input: session id.
- Output: that session is revoked and its access credentials refused at once; a "session revoked" line.
- Main flow: 1. The person chooses Sign out on a device row. 2. The client posts `POST /api/v1/auth/sessions/{sessionId}/revoke`. 3. The row disappears.
- No-op / rejection / concurrency / retry: revoking an already revoked session is a no-op success; an unknown or foreign id answers the same 404 as a missing one; revoking the current session signs this device out.
- UI states: pending, success, not found, destructive confirmation for the current device.
- Rules: ID-005; section 19; IAREQ021.
- Acceptance: Given a second device, when its row is revoked, then its next call is 401. Given another person's session id, then 404 and no change.
- Evidence: BE `Sessions/Commands/RevokeSession/RevokeSession.cs`; API `Auth/Commands/SessionEndpoints.cs` (`POST /api/v1/auth/sessions/{sessionId}/revoke`) · FE `.../account/security.tsx` · Design Final-Account · Tests Domain `UserSessionTests`; mock e2e `account-pages.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING. Missing: Application test for foreign id, real spec.

### UC-IDN-C04 Sign out all other devices
- Capability: C Logout
- Actors and permission: signed-in person; step-up required (owner decision 48).
- Preconditions: the current session is bound to the request; a step-up proof of purpose `RevokeOtherSessions`.
- Input: the step-up proof.
- Output: every other Active session of the person revoked; one "other sessions revoked" line when at least one ended.
- Main flow: 1. The person chooses "Sign out other devices". 2. The page runs the step-up (UC-IDN-K03 to K06). 3. The client posts `POST /api/v1/auth/sessions/revoke-others`. 4. The page shows how many ended.
- No-op / rejection / concurrency / retry: with no other session it is a no-op and records nothing; an unbound session answers 409 `identity.session.not-bound`; a used or missing proof answers the step-up error and changes nothing.
- UI states: step-up prompt, pending, success, error.
- Rules: ID-006; owner decision 48; IAREQ021, IAREQ056.
- Acceptance: Given three sessions, when the person steps up and revokes others, then one remains. Given no proof, then 4xx and all three remain.
- Evidence: BE `Sessions/Commands/RevokeOtherSessions/RevokeOtherSessions.cs` (no step-up call); API `Auth/Commands/SessionEndpoints.cs` (`POST /api/v1/auth/sessions/revoke-others`) · FE `.../account/security.tsx` · Design Final-Account · Tests mock e2e `account-pages.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING. Missing: the step-up purpose and its check (defect), Application and API tests, real spec.

## Capability D: Session expiration

### UC-IDN-D01 Refresh the session silently
- Capability: D Session expiration
- Actors and permission: holder of a refresh credential; public endpoint, `AuthRefreshByIp`.
- Preconditions: an Active, unexpired session for the refresh credential.
- Input: refresh cookie (web) or body (mobile, UC-IDN-A04).
- Output: a new access credential (1 hour) and a rotated refresh credential; the old session is revoked and a new one is created.
- Main flow: 1. The client sees a 401 or an expiring access credential. 2. It posts `POST /api/v1/auth/refresh`. 3. It retries the failed call once.
- No-op / rejection / concurrency / retry: an invalid, revoked or expired credential answers 401 `identity.auth.invalid-refresh-token`; an Inactive or suspended user has the session revoked and gets the same 401; the answer never names the reason; two parallel refreshes with one credential let one win.
- UI states: invisible on success; session expired on failure (UC-IDN-D02).
- Rules: ID-005; section 18; IAREQ017, IAREQ019.
- Acceptance: Given a valid credential, when the client refreshes, then the old credential fails afterwards. Given a suspended user, then 401 and the session ends.
- Evidence: BE `Auth/Commands/RefreshToken/RefreshToken.cs`; API `Auth/Commands/RefreshTokenEndpoint.cs` (`POST /api/v1/auth/refresh`) · FE `packages/foundation/contracts/src/client/api-client.ts` · Design n/a · Tests Application `RefreshTokenTests` (4), Domain `RefreshTokenHashTests` · Real e2e none
- State: INTEGRATING. Missing: the session id changes at each refresh (step-up and the "this device" flag must survive it), real spec.

### UC-IDN-D02 Session expired or revoked
- Capability: D Session expiration
- Actors and permission: signed-in person.
- Preconditions: a 401 that refresh cannot repair.
- Input: none.
- Output: protected actions stop; the person is asked to sign in again and returns to the page they were on.
- Main flow: 1. A call answers 401 and refresh fails. 2. The client clears account-scoped server state. 3. It opens sign-in with `returnTo` (internal paths only, `safeReturnPath`). 4. After sign-in the person lands back.
- No-op / rejection / concurrency / retry: a late response of the old session must not write into the new session's state; an external `returnTo` is ignored.
- UI states: session expired, return after sign-in.
- Rules: ID sections 58 and 59; IAREQ019.
- Acceptance: Given a revoked session, when the next call fails, then no protected UI stays interactive. Given `returnTo=https://x`, then the person lands on the home page.
- Evidence: FE `packages/features/auth/src/core/utils/safe-return-path.ts`, `apps/web/src/routes/auth-callback.tsx` · Design Final-States · Tests FE `safe-return-path.unit.test.ts`, Application `SafeReturnPathTests`; mock e2e `auth-flow.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING. Missing: real spec for a revoked session, account-scoped state reset proof.

### UC-IDN-D03 Reject a replayed refresh credential
- Capability: D Session expiration
- Actors and permission: system.
- Preconditions: a refresh credential already rotated.
- Input: that credential presented again.
- Output: 401 and, after a grace window, the whole session family revoked.
- Main flow: 1. The server keeps the previous credential of each session for 10 seconds. 2. A second use inside the window returns the same new pair (a retry of a lost response). 3. A use after the window revokes every session of that chain and records "other sessions revoked".
- No-op / rejection / concurrency / retry: the window covers two tabs refreshing together; outside it the replay is treated as theft.
- UI states: session expired.
- Rules: ID section 18; ID-005; enterprise baseline (Auth0, Okta).
- Acceptance: Given a rotated credential, when it is replayed after the window, then the chain ends and the next refresh fails.
- Evidence: BE none · FE none · Design n/a · Tests none · Real e2e none
- State: DEFINED. Missing: all. Lead decision (2026-10-30): window 10 seconds, then revoke the chain.

### UC-IDN-D04 End access when a person is disabled
- Capability: D Session expiration
- Actors and permission: Account administrator or SCIM deprovisioning (see [accounts](accounts.md)); the system applies the effect.
- Preconditions: a User moves to Inactive, Suspended or deprovisioned.
- Input: the status change.
- Output: all sessions revoked, all access tokens of the person refused, future sign-in refused with the generic failure; history stays under other contexts (ID-003, ID-016).
- Main flow: 1. A caller outside Identity requests the status change. 2. Identity changes the status and revokes sessions and tokens in the same unit of work. 3. The person is signed out at the next call.
- No-op / rejection / concurrency / retry: disabling a disabled person is a no-op; deleting or anonymizing is not offered yet (owner decision 48); the effect is atomic with the status.
- UI states: signed out with the generic sign-in failure; the admin view belongs to Accounts.
- Rules: ID sections 8, 55; ID-003, ID-006, ID-016; IAREQ003, IAREQ004.
- Acceptance: Given an active person with two sessions and one token, when they are disabled, then all three stop working at once.
- Evidence: Domain `backend/src/Notrelix.Domain/Identity/Users/User.cs` (`Activate`, `Deactivate`, `Suspend`); BE refresh blocks a non-Active user (`RefreshToken.cs`) and login refuses it; no Application command calls them · FE none · Design none · Tests Domain `UserTests`, Application `RefreshTokenTests` · Real e2e none
- State: IMPLEMENTING. Missing: the command, the session and token revocation, the caller in Accounts, tests. Lead decision (2026-10-30): disabling revokes every session and token at once.

### UC-IDN-D05 Bound the absolute session lifetime
- Capability: D Session expiration
- Actors and permission: system.
- Preconditions: a session chain.
- Input: the age of the chain.
- Output: the chain ends after an absolute limit even when used daily.
- Main flow: 1. Each rotated session keeps the start time of its chain. 2. Refresh fails when the chain is older than 90 days or idle for 30 days. 3. The person signs in again.
- No-op / rejection / concurrency / retry: limits are platform values; an Account policy may shorten them later (Accounts).
- UI states: session expired.
- Rules: ID-005; IAREQ019; enterprise baseline (Auth0, Notion).
- Acceptance: Given a chain of 91 days, when it refreshes, then 401.
- Evidence: BE none (`RefreshToken.cs` adds 30 days at each rotation) · FE none · Design n/a · Tests none · Real e2e none
- State: DEFINED. Missing: all. Lead decision (2026-10-30): absolute 90 days, idle 30 days.

## Capability E: OAuth start

### UC-IDN-E01 Start Google or Microsoft sign-in
- Capability: E OAuth start
- Actors and permission: any visitor; public, `AuthCredentialByIp`.
- Preconditions: the provider is enabled by configuration.
- Input: provider in the path, optional `returnUrl` (internal paths only).
- Output: a redirect to the provider with state, nonce and PKCE held server side.
- Main flow: 1. The person presses the provider button. 2. The browser opens `GET /api/v1/auth/oauth/{provider}/start`. 3. The server stores the state and redirects.
- No-op / rejection / concurrency / retry: a disabled or unknown provider answers a failure and no redirect; every start makes a new state; an external `returnUrl` is dropped.
- UI states: redirecting, provider unavailable.
- Rules: ID-008, section 29 (this is Identity OAuth, not an Integrations connection); IAREQ040, IAREQ041.
- Acceptance: Given an enabled provider, when sign-in starts, then the redirect carries state and a code challenge. Given a disabled provider, then no redirect.
- Evidence: BE `OAuth/Commands/StartOAuthLogin/StartOAuthLoginCommand.cs`; API `Auth/Commands/StartOAuthLoginEndpoint.cs` (`GET /api/v1/auth/oauth/{provider}/start`) · FE `packages/features/auth/src/web/components/auth-sign-in-surface.tsx`, `google-mark.tsx`, `microsoft-mark.tsx` · Design Final-Auth · Tests Domain `OAuthSecretLeakageTests`; mock e2e `auth-flow.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING. Missing: Application test, real spec (needs a provider test double).

## Capability F: OAuth callback

### UC-IDN-F01 Complete provider sign-in
- Capability: F OAuth callback
- Actors and permission: visitor returning from the provider; public, `AuthCredentialByIp`.
- Preconditions: a valid, unused state.
- Input: provider, code, state.
- Output: a session (or the second-step challenge when MFA is on), then a redirect to `/auth/callback?returnTo=`; a "sign-in succeeded" line.
- Main flow: 1. The provider redirects to `GET /api/v1/auth/oauth/{provider}/callback`. 2. The server validates the state, exchanges the code and reads the provider subject. 3. It finds the User by provider and subject, or creates one when no account holds that email. 4. It sets cookies and redirects.
- No-op / rejection / concurrency / retry: the state is single use; every failure redirects to sign-in with a normalized error code; the provider tokens are never stored in a readable form.
- UI states: pending, provider error, second step.
- Rules: ID-008, ID-014; section 28; IAREQ042, IAREQ043, IAREQ046, IAREQ047.
- Acceptance: Given a new provider subject with a free email, when the callback completes, then one User and one session exist. Given a returning subject, then the same User signs in.
- Evidence: BE `OAuth/Commands/CompleteOAuthLogin/CompleteOAuthLoginCommand.cs`; API `Auth/Commands/CompleteOAuthLoginEndpoint.cs` (`GET /api/v1/auth/oauth/{provider}/callback`) · FE `apps/web/src/routes/auth-callback.tsx` · Design Final-Auth · Tests API `OAuthCallbackMfaTests`, Integration `CompletedOAuthLoginCommandHandlerTests`, Domain `OAuthProfileSnapshotTests`, `OAuthEventPayloadTests` · Real e2e none
- State: INTEGRATING. Missing: real spec, 1 px diff, owner check.

### UC-IDN-F02 Refuse a stale, replayed or colliding callback
- Capability: F OAuth callback
- Actors and permission: visitor or attacker; public.
- Preconditions: none.
- Input: a callback with an old, reused or foreign state, or a subject that collides.
- Output: no session; a redirect to sign-in with a normalized error.
- Main flow: 1. The callback arrives. 2. State fails, or the provider subject is bound to another User, or the email belongs to a password account. 3. The server refuses and records nothing about the secret values.
- No-op / rejection / concurrency / retry: an email match alone never links or merges (ID-008, ID-018); the existing account's owner must sign in and link from the security page (UC-IDN-G01); a replay of a good callback does not open a second session.
- UI states: provider error with a plain reason, offer to sign in with the password.
- Rules: ID-008, ID-018; sections 27, 28, 64; IAREQ044, IAREQ047.
- Acceptance: Given a used state, when it is replayed, then no session. Given a matching email on a password account, then no merge.
- Evidence: BE `OAuth/Commands/CompleteOAuthLogin/CompleteOAuthLoginCommand.cs` · FE `packages/features/auth/src/core/utils/parse-auth-error.ts` · Design none · Tests Domain `UserOAuthFailureAtomicityTests`, `UserUnlinkOAuthInvariantTests`, `OAuthAccountTests`; Integration `CompletedOAuthLoginCommandHandlerTests` · Real e2e none
- State: IMPLEMENTING. Missing: the sign-in page copy for each error code, an artboard, API test for state replay.

## Capability G: Provider linking and unlinking

### UC-IDN-G01 Link a provider to my account
- Capability: G Provider linking and unlinking
- Actors and permission: signed-in person; step-up of purpose `LinkOAuth`.
- Preconditions: the provider is enabled; the provider subject is not linked to another User.
- Input: provider, step-up proof.
- Output: a linked provider on the person's account; a security-activity line.
- Main flow: 1. The person chooses "Connect Google" on the security page. 2. They pass step-up. 3. The browser opens `GET /api/v1/auth/oauth/{provider}/link/start`, then returns through `GET /api/v1/auth/oauth/{provider}/link/callback`. 4. The page shows the linked provider.
- No-op / rejection / concurrency / retry: linking an already linked provider is a no-op; a subject owned by another User is refused with no disclosure of who; a stale or replayed callback is refused.
- UI states: step-up prompt, redirecting, success, collision, error.
- Rules: ID-008, ID-006; sections 26 to 28; IAREQ043, IAREQ044.
- Acceptance: Given step-up, when the link completes, then the summary lists the provider. Given a colliding subject, then no link.
- Evidence: BE `OAuth/Commands/StartOAuthLink`, `CompleteOAuthLink`; API `Auth/Commands/StartOAuthLinkEndpoint.cs` (`GET /api/v1/auth/oauth/{provider}/link/start`), `CompleteOAuthLinkEndpoint.cs` (`GET /api/v1/auth/oauth/{provider}/link/callback`) · FE none · Design none · Tests Domain `UserOAuthFailureAtomicityTests` · Real e2e none
- State: IMPLEMENTING. Missing: the screen, an artboard, Application and API tests.

### UC-IDN-G02 Unlink a provider
- Capability: G Provider linking and unlinking
- Actors and permission: signed-in person; step-up of purpose `UnlinkOAuth`; `AuthSensitiveByUser`.
- Preconditions: the provider is linked and the person keeps another viable sign-in method (password or another provider).
- Input: provider, step-up proof.
- Output: the link removed; a security-activity line.
- Main flow: 1. The person chooses Disconnect. 2. They pass step-up and confirm. 3. The client posts `POST /api/v1/auth/oauth/{provider}/unlink`.
- No-op / rejection / concurrency / retry: unlinking the last viable method is refused with the reason; unlinking an unlinked provider is a no-op; a used proof fails.
- UI states: destructive confirmation, step-up prompt, blocked with reason, success.
- Rules: ID section 28 (last viable method); IAREQ045.
- Acceptance: Given a password and a provider, when the provider is unlinked, then sign-in by password remains. Given a provider-only account, then the unlink is refused.
- Evidence: BE `OAuth/Commands/UnlinkOAuth/UnlinkOAuthCommand.cs`; API `Auth/Commands/UnlinkOAuthEndpoint.cs` (`POST /api/v1/auth/oauth/{provider}/unlink`) · FE none · Design none · Tests Domain `UserUnlinkOAuthInvariantTests` · Real e2e none
- State: IMPLEMENTING. Missing: the screen, an artboard, Application and API tests.

## Capability H: MFA enrollment

### UC-IDN-H01 Start second-factor enrollment
- Capability: H MFA enrollment
- Actors and permission: signed-in person; own account.
- Preconditions: no active second factor.
- Input: none.
- Output: a pending authenticator method with a secret and a setup URI for a QR code; the secret is shown now and never again.
- Main flow: 1. The person chooses "Turn on two-factor". 2. The client posts `POST /api/v1/auth/mfa/enrollment/start`. 3. The page shows the QR code and the manual key.
- No-op / rejection / concurrency / retry: a pending, unverified method is not active MFA (ID section 24); starting again replaces the pending method; starting with an active factor is refused.
- UI states: pending, showing the code, error.
- Rules: ID-004, ID-007; sections 22 to 24; IAREQ048, IAREQ049, IAREQ053.
- Acceptance: Given no factor, when enrollment starts, then the method is pending and sign-in still needs no second step.
- Evidence: BE `Mfa/Commands/StartMfaEnrollment/StartMfaEnrollment.cs`; API `Auth/Commands/StartMfaEnrollmentEndpoint.cs` (`POST /api/v1/auth/mfa/enrollment/start`) · FE `packages/features/account/src/web/security/account-qr-code.tsx` · Design Final-Account · Tests Domain `MfaMethodTests`; Infrastructure `MfaTotpServiceTests`; FE `account-qr-code.unit.test.ts` · Real e2e none
- State: INTEGRATING. Missing: Application test, real spec, rate-limit attribute.

### UC-IDN-H02 Verify and activate the second factor
- Capability: H MFA enrollment
- Actors and permission: signed-in person; `AuthSensitiveByUser`.
- Preconditions: a pending method.
- Input: the 6-digit code.
- Output: the method is Active; ten recovery codes are shown once; a "second factor enabled" line.
- Main flow: 1. The person types the code. 2. The client posts `POST /api/v1/auth/mfa/enrollment/verify`. 3. The page shows the recovery codes with a "I saved them" gate.
- No-op / rejection / concurrency / retry: a wrong code leaves the method pending; the code counts against the lockout; verifying an active method is a no-op; the codes are never readable again.
- UI states: pending, wrong code, success with codes.
- Rules: ID-004, ID-006; section 24; IAREQ049, IAREQ051, IAREQ054.
- Acceptance: Given a pending method, when the right code is sent, then MFA is active and the summary shows ten codes left.
- Evidence: BE `Mfa/Commands/VerifyMfaEnrollment/VerifyMfaEnrollment.cs`; API `Auth/Commands/VerifyMfaEnrollmentEndpoint.cs` (`POST /api/v1/auth/mfa/enrollment/verify`) · FE `packages/features/account/src/web/security/account-security-surface.tsx` · Design Final-Account · Tests Domain `MfaRecoveryBatchTests`, `UserSecuritySettingsTests` · Real e2e none
- State: INTEGRATING. Missing: Application and API tests, real spec.

### UC-IDN-H03 Read the second-factor status
- Capability: H MFA enrollment
- Actors and permission: signed-in person; own status.
- Preconditions: none.
- Input: none.
- Output: enabled flag, method status, recovery codes left.
- Main flow: 1. The security page reads `GET /api/v1/auth/mfa/status`.
- No-op / rejection / concurrency / retry: read; never returns the secret.
- UI states: loading, off, on, error.
- Rules: ID-014; IAREQ053.
- Acceptance: Given an active factor, when status is read, then no secret or code is in the body.
- Evidence: BE `Mfa/Queries`; API `Auth/Queries/GetMfaStatusEndpoint.cs` (`GET /api/v1/auth/mfa/status`) · FE `packages/features/account/src/core/api/account.service.ts` · Design Final-Account · Tests API `AuthenticatedAuthEndpointTests` · Real e2e none
- State: INTEGRATING. Missing: real spec; the page may read the summary (UC-IDN-K01) and make this call redundant.

## Capability I: MFA challenge

### UC-IDN-I01 Answer the second step at sign-in
- Capability: I MFA challenge
- Actors and permission: visitor who passed the first step; public, `AuthCredentialByIp`.
- Preconditions: a live challenge held in an httpOnly cookie (also after a provider sign-in).
- Input: the 6-digit code (the challenge is in the cookie).
- Output: a session; the challenge cookie cleared; a "sign-in succeeded" line.
- Main flow: 1. Password or provider step answers "second step needed". 2. The person types the code. 3. The client posts `POST /api/v1/auth/mfa/verify`. 4. The app routes on, keeping `returnTo`.
- No-op / rejection / concurrency / retry: a wrong code counts as a failed sign-in; a missing or expired challenge answers "verification expired" and asks to start again; the challenge never appears in a URL.
- UI states: code entry, wrong code, expired, pending.
- Rules: ID-007; section 22; IAREQ050.
- Acceptance: Given a challenge, when the right code is sent, then a session opens. Given no challenge cookie, then the expired state.
- Evidence: BE `Mfa/Commands/CompleteMfaChallenge/CompleteMfaChallenge.cs`; API `Auth/Commands/CompleteMfaChallengeEndpoint.cs` (`POST /api/v1/auth/mfa/verify`) · FE `packages/features/auth/src/web/components/auth-sign-in-surface.tsx` · Design Final-Auth · Tests API `MfaChallengeCookieTests`; Infrastructure `CookieServiceMfaChallengeTests`; mock e2e `auth-flow.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING. Missing: real spec, Application test.

### UC-IDN-I02 Expire a challenge after wrong codes
- Capability: I MFA challenge
- Actors and permission: visitor.
- Preconditions: a live challenge.
- Input: wrong codes.
- Output: after 5 wrong codes the challenge is dead and the cookie is deleted; the person signs in again from the password step.
- Main flow: 1. Each wrong code counts. 2. The fifth kills the challenge. 3. The account and address counters keep counting (UC-IDN-B09).
- No-op / rejection / concurrency / retry: no answer tells how many attempts remain; a dead challenge cannot be revived by a correct code.
- UI states: wrong code, expired.
- Rules: ID section 67; IAREQ050.
- Acceptance: Given five wrong codes, when the right one follows, then it is refused.
- Evidence: BE `Mfa/Commands/CompleteMfaChallenge/CompleteMfaChallenge.cs`; API `Auth/Commands/CompleteMfaChallengeEndpoint.cs` · FE `auth-sign-in-surface.tsx` · Design Final-Auth · Tests API `MfaChallengeCookieTests`; mock e2e `auth-flow.mock.e2e.spec.ts` ("a wrong second-step code is refused") · Real e2e none
- State: INTEGRATING. Missing: real spec.

## Capability J: MFA recovery

### UC-IDN-J01 Sign in with a recovery code
- Capability: J MFA recovery
- Actors and permission: visitor who passed the first step.
- Preconditions: a live challenge; an unused recovery code.
- Input: the recovery code in the code field.
- Output: a session; that code is spent; the count left drops by one.
- Main flow: 1. The person chooses "Use a backup code". 2. They type it. 3. The client posts `POST /api/v1/auth/mfa/verify`.
- No-op / rejection / concurrency / retry: a spent code fails like a wrong code; codes are hashed and never readable.
- UI states: code entry, wrong code, success.
- Rules: ID-004; IAREQ051.
- Acceptance: Given a recovery code, when it is used twice, then the second use fails.
- Evidence: BE `Mfa/Commands/CompleteMfaChallenge/CompleteMfaChallenge.cs` · FE `auth-sign-in-surface.tsx` · Design Final-Auth · Tests Domain `MfaRecoveryBatchTests`; Infrastructure `MfaRecoveryCodeGeneratorTests`; mock e2e `auth-flow.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING. Missing: real spec.

### UC-IDN-J02 Regenerate recovery codes
- Capability: J MFA recovery
- Actors and permission: signed-in person; step-up of purpose `RegenerateRecoveryCodes`.
- Preconditions: an active second factor.
- Input: step-up proof.
- Output: ten new codes shown once; the old batch is void.
- Main flow: 1. The person chooses "New backup codes". 2. They pass step-up. 3. The client posts `POST /api/v1/auth/mfa/recovery-codes/regenerate`.
- No-op / rejection / concurrency / retry: a used proof fails; a retry after success makes another batch and voids the one just shown, so the page must not retry by itself; no active factor is refused.
- UI states: step-up prompt, success with codes, error.
- Rules: ID-004, ID-006; IAREQ051, IAREQ056.
- Acceptance: Given step-up, when codes regenerate, then old codes fail and new codes work.
- Evidence: BE `Mfa/Commands/RegenerateRecoveryCodes/RegenerateRecoveryCodes.cs`; API `Auth/Commands/RegenerateRecoveryCodesEndpoint.cs` (`POST /api/v1/auth/mfa/recovery-codes/regenerate`) · FE `packages/features/account/src/web/security/account-security-surface.tsx` · Design Final-Account · Tests Domain `MfaRecoveryBatchTests` · Real e2e none
- State: INTEGRATING. Missing: Application and API tests, real spec, rate-limit attribute.

### UC-IDN-J03 Turn the second factor off
- Capability: J MFA recovery
- Actors and permission: signed-in person; step-up of purpose `DisableMfa`.
- Preconditions: an active factor; no Account policy requires it (UC-IDN-J05).
- Input: step-up proof.
- Output: the method is removed; recovery codes void; a "second factor disabled" line.
- Main flow: 1. The person chooses "Turn off". 2. They confirm and pass step-up. 3. The client posts `POST /api/v1/auth/mfa/disable`.
- No-op / rejection / concurrency / retry: disabling an off factor is a no-op; a required factor is refused with the reason; a stale second tab sees the new state on reload.
- UI states: destructive confirmation, step-up prompt, blocked with reason, success.
- Rules: ID-007; sections 22, 25; ID-015; IAREQ052, IAREQ054.
- Acceptance: Given step-up, when MFA is disabled, then sign-in needs no second step and one activity line exists.
- Evidence: BE `Mfa/Commands/DisableMfa/DisableMfa.cs`; API `Auth/Commands/DisableMfaEndpoint.cs` (`POST /api/v1/auth/mfa/disable`) · FE `account-security-surface.tsx` · Design Final-Account · Tests Domain `UserSecuritySettingsTests`; mock e2e `account-pages.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING. Missing: the policy-blocked case, Application test, real spec.

### UC-IDN-J04 Recover an account that lost both factors
- Capability: J MFA recovery
- Actors and permission: the person, and an Account administrator who may reset another person's second factor (see [accounts](accounts.md)).
- Preconditions: no authenticator and no recovery code.
- Input: proof of control (to be decided).
- Output: the second factor is reset and all sessions end.
- Main flow: not defined.
- No-op / rejection / concurrency / retry: not defined.
- UI states: not defined.
- Rules: ID section 66 (recovery must not bypass proof); ID-006; ID-013.
- Acceptance: not defined until the decision.
- Evidence: none.
- State: BLOCKED. Needs a decision on who may reset another person's second factor, what proof the person gives, and the delay and notice. A wrong choice weakens account takeover defence.

### UC-IDN-J05 Enforce an account-required second factor
- Capability: J MFA recovery
- Actors and permission: system; the requirement belongs to Accounts or Governance, the method belongs to Identity (ID-007).
- Preconditions: an Account policy "require MFA" (not in the product docs yet).
- Input: policy fact for the person's Account.
- Output: a person without a factor is sent to enrollment after sign-in and before product access; removal of the last factor is refused.
- Main flow: not defined until the policy fact exists.
- No-op / rejection / concurrency / retry: not defined.
- UI states: forced enrollment, blocked removal.
- Rules: ID-007, ID-011; sections 23, 25, 37.
- Acceptance: not defined until the decision.
- Evidence: none (a `LoginFailureReason` mentions MFA only for challenges).
- State: BLOCKED. Waits on the Accounts or Governance policy fact and on which Account's policy wins when a person belongs to several.

## Capability K: Security settings

### UC-IDN-K01 Read my security summary
- Capability: K Security settings
- Actors and permission: signed-in person; own facts only.
- Preconditions: none.
- Input: none.
- Output: password present and last changed, second factor enabled and recovery codes left, active session count, linked providers.
- Main flow: 1. The Security page reads `GET /api/v1/auth/security/summary`.
- No-op / rejection / concurrency / retry: read; "last changed" is known only for changes made since it was recorded.
- UI states: loading, success, error.
- Rules: ID section 67 (account security facts), ID-011; IAREQ055.
- Acceptance: Given a person with a password and a factor, when the summary is read, then the four facts match their state and no other person's.
- Evidence: BE `Security/Queries/GetAccountSecuritySummary`; API `Auth/Commands/SecurityStepUpEndpoints.cs` (`GET /api/v1/auth/security/summary`) · FE `.../account/security.tsx` · Design Final-Account · Tests Application `GetAccountSecuritySummaryTests`, API `SecuritySummaryEndpointTests` · Real e2e none
- State: INTEGRATING. Missing: real spec.

### UC-IDN-K02 Read my security activity
- Capability: K Security settings
- Actors and permission: signed-in person; own lines only.
- Preconditions: none.
- Input: paging cursor.
- Output: newest first lines: type, time, client address and user agent when known, session when relevant; kept 90 days.
- Main flow: 1. The page reads `GET /api/v1/auth/security/activity`. 2. It shows the lines.
- No-op / rejection / concurrency / retry: a failed sign-in is recorded only for an existing account; an unknown email leaves no line; no attempted password is stored; no geolocation.
- UI states: loading, empty, success, error.
- Rules: ID section 67A.
- Acceptance: Given a failed sign-in on my account, when I read activity, then one line shows. Given a guess on an unknown email, then no line exists anywhere.
- Evidence: BE `Security/Queries/GetSecurityActivity`; Infrastructure `BackgroundJobs/SecurityActivityRetention.cs`; API `Auth/Commands/SecurityStepUpEndpoints.cs` (`GET /api/v1/auth/security/activity`) · FE `.../account/security.tsx` · Design Final-Account · Tests Application `GetSecurityActivityTests`, Domain `SecurityActivityEntryTests`, Integration `SecurityActivityHttpFlowTests`, `SecurityEventWriteTests` · Real e2e none
- State: INTEGRATING. Missing: real spec.

### UC-IDN-K03 Ask which step-up factor applies
- Capability: K Security settings
- Actors and permission: signed-in person; `AuthSensitiveByUser`.
- Preconditions: a sensitive action is about to run.
- Input: the purpose (disable MFA, regenerate codes, link or unlink a provider, issue an API token, change password).
- Output: the factor to use: MFA first, then password, then provider re-authentication.
- Main flow: 1. The page posts `POST /api/v1/auth/security/step-up/requirement`. 2. It shows the matching prompt.
- No-op / rejection / concurrency / retry: read-like; no state change; a purpose with no mapping is refused.
- UI states: pending, prompt.
- Rules: ID-006; IAREQ056.
- Acceptance: Given MFA on, when the requirement is asked, then it says MFA; given a provider-only person, then provider.
- Evidence: BE `Security/Queries/GetStepUpRequirement`, `Security/StepUpPurposeMapping.cs`; API `Auth/Commands/SecurityStepUpEndpoints.cs` (`POST /api/v1/auth/security/step-up/requirement`) · FE `account.service.ts` · Design Final-Account · Tests Application `SecurityStepUpServiceThrottleTests` · Real e2e none
- State: INTEGRATING. Missing: real spec.

### UC-IDN-K04 Step up with a second-factor code
- Capability: K Security settings
- Actors and permission: signed-in person; `AuthSensitiveByUser`.
- Preconditions: an active factor; a challenge bound to this session.
- Input: purpose, code (TOTP or recovery code).
- Output: a single-use step-up proof bound to the user, the session and the purpose, short lived.
- Main flow: 1. The person types the code. 2. The client posts `POST /api/v1/auth/security/step-up/complete-mfa`. 3. The page sends the proof with the sensitive request.
- No-op / rejection / concurrency / retry: the proof works once, for that purpose and that session only; wrong codes count against the lockout; a locked attempt is not verified.
- UI states: code entry, wrong code, locked, success.
- Rules: ID section 67, ID-006; IAREQ056.
- Acceptance: Given a proof, when used twice, then the second use fails. Given a proof for another purpose, then it fails.
- Evidence: BE `Security/Commands/CompleteStepUpMfa`; API `SecurityStepUpEndpoints.cs` (`POST /api/v1/auth/security/step-up/complete-mfa`) · FE `account-security-surface.tsx` · Design Final-Account · Tests Application `SecurityStepUpServiceThrottleTests`; mock e2e `account-pages.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING. Missing: real spec; the proof is bound to a session id that refresh replaces (UC-IDN-D01).

### UC-IDN-K05 Step up with the password
- Capability: K Security settings
- Actors and permission: signed-in person with a password and no second factor.
- Preconditions: a password credential.
- Input: purpose, current password.
- Output: a single-use proof as in K04.
- Main flow: 1. The person types the password. 2. The client posts `POST /api/v1/auth/security/step-up/complete-password`.
- No-op / rejection / concurrency / retry: a wrong password counts against the lockout; a locked attempt is not verified.
- UI states: password entry, wrong password, locked, success.
- Rules: ID section 67, ID-006.
- Acceptance: Given the right password, when step-up completes, then one proof exists; given a wrong one, then none.
- Evidence: BE `Security/Commands/CompleteStepUpPassword`; API `SecurityStepUpEndpoints.cs` (`POST /api/v1/auth/security/step-up/complete-password`) · FE `account-password-panel.tsx` · Design Final-Account · Tests Application `SecurityStepUpServiceThrottleTests` · Real e2e none
- State: INTEGRATING. Missing: real spec.

### UC-IDN-K06 Step up with a linked provider
- Capability: K Security settings
- Actors and permission: signed-in person with only provider sign-in.
- Preconditions: a linked provider.
- Input: purpose.
- Output: a single-use proof after a fresh provider login.
- Main flow: 1. The page opens `GET /api/v1/auth/oauth/{provider}/step-up/start`. 2. The provider returns to `GET /api/v1/auth/oauth/{provider}/step-up/callback`. 3. The page continues the sensitive action.
- No-op / rejection / concurrency / retry: state is single use; a different provider subject fails; stale callbacks fail.
- UI states: redirecting, success, error.
- Rules: ID-006, ID-008.
- Acceptance: Given a provider-only person, when step-up completes, then one proof exists.
- Evidence: BE `OAuth/Commands/StartOAuthStepUp`, `CompleteOAuthStepUp`; API `Auth/Commands/StartOAuthStepUpEndpoint.cs`, `CompleteOAuthStepUpEndpoint.cs` · FE none · Design none · Tests none · Real e2e none
- State: IMPLEMENTING. Missing: screen, artboard, tests.

### UC-IDN-K07 Change my password
- Capability: K Security settings
- Actors and permission: signed-in person with a password; step-up of purpose `ChangePassword`; `AuthSensitiveByUser`.
- Preconditions: current password correct.
- Input: current password, new password (policy as B03), step-up proof.
- Output: new password; other sessions revoked; a "password changed" line.
- Main flow: 1. The person fills the Security panel. 2. The client posts `POST /api/v1/auth/change-password`. 3. The page confirms; other devices are signed out.
- No-op / rejection / concurrency / retry: a wrong current password counts against the lockout; same password as before is refused; a retry with a used proof fails.
- UI states: validation error, pending, success, locked.
- Rules: ID sections 12, 20; ID-006; IAREQ015, IAREQ056.
- Acceptance: Given a second device, when the password changes, then that device is signed out. Given a wrong current password, then no change.
- Evidence: BE `Auth/Commands/ChangePassword/ChangePassword.cs`; API `Auth/Commands/ChangePasswordEndpoint.cs` (`POST /api/v1/auth/change-password`) · FE `account-password-panel.tsx`, `packages/features/account/src/core/password.ts` · Design Final-Account · Tests Application `ChangePasswordTests`, API `ChangePasswordEndpointTests`, FE `password.unit.test.ts` · Real e2e none
- State: INTEGRATING. Missing: real spec.

### UC-IDN-K08 Set a first password (enterprise baseline)
- Capability: K Security settings
- Actors and permission: signed-in person with no password credential (provider sign-in).
- Preconditions: step-up by provider.
- Input: new password.
- Output: a password credential; the summary shows it.
- Main flow: 1. The person chooses "Set a password". 2. They pass step-up. 3. The server sets the password and records "password changed".
- No-op / rejection / concurrency / retry: setting a password when one exists is the change flow (K07).
- UI states: step-up prompt, validation error, success.
- Rules: ID section 12; Slack and Notion offer it.
- Acceptance: Given a provider-only person, when a password is set, then password sign-in works.
- Evidence: none (`HasPasswordCredential` exists on User; no command).
- State: DEFINED. Missing: all. Lead decision (2026-10-30): offered through the provider step-up.

### UC-IDN-K09 Change my sign-in email
- Capability: K Security settings
- Actors and permission: signed-in person; step-up of purpose `ChangeSecurityIdentity` (lead decision).
- Preconditions: the new email is free.
- Input: new email, step-up proof.
- Output: a confirmation mail to the new address; the old address keeps working until the new one is confirmed; a notice to the old address; the User id stays.
- Main flow: 1. The person enters the new email. 2. They pass step-up. 3. They open the link sent to the new address. 4. The sign-in email changes.
- No-op / rejection / concurrency / retry: the same email is a no-op; a taken email gets the same neutral answer as a free one ("check your inbox"); other sessions are revoked on confirmation.
- UI states: step-up prompt, sent, validation error, confirmed.
- Rules: ID-002, section 63; ID-006.
- Acceptance: Given an unconfirmed new address, when the person signs in with the old one, then it works; after confirmation only the new one works and the User id is unchanged.
- Evidence: BE `Profiles/Commands/UpdateEmail/UpdateEmail.cs` (changes the email at once, no step-up, answers "Email is already in use."); API `Profile/Commands/UpdateEmailEndpoint.cs` (`POST /api/v1/profile/email`) · FE none · Design none · Tests Application `UpdateEmailTests`, API `ProfileEndpointTests` · Real e2e none
- State: IMPLEMENTING. Missing: step-up, pending-email model, old-address notice, neutral answer, screen, artboard.

### UC-IDN-K10 Notify me of security changes (enterprise baseline)
- Capability: K Security settings
- Actors and permission: system; the person receives.
- Preconditions: a security event.
- Input: password change, reset, second factor on or off, provider link or unlink, new device sign-in, sign-out of other devices, email change.
- Output: a plain mail with time, device and a link to the security page; no secret.
- Main flow: 1. The event commits. 2. A post-commit mail goes to the verified address.
- No-op / rejection / concurrency / retry: a mail failure never fails the event; a retry sends at most one mail per event id.
- UI states: none (mail).
- Rules: ID-014; section 49.
- Acceptance: Given a password change, when it commits, then one mail names the device and holds no secret.
- Evidence: none.
- State: DEFINED. Missing: all. Lead decision (2026-10-30): the events above are mailed, new-device mail only for a client address and agent not seen in the last 90 days.

### UC-IDN-K11 Operator audit of identity security changes
- Capability: K Security settings
- Actors and permission: Account or platform auditor under Governance (see the audit use cases of the governance catalog).
- Preconditions: audit read permission.
- Input: none from Identity.
- Output: audit evidence for credential reset, MFA change, session revocation, provider link and unlink, token create and revoke, without secrets.
- Main flow: 1. A sensitive Identity request completes. 2. The audit middleware records it. 3. The auditor reads it in Governance.
- No-op / rejection / concurrency / retry: the person's own activity feed (UC-IDN-K02) is separate and never exposes audit details.
- UI states: owned by Governance.
- Rules: ID section 49, ID-014; IAREQ057, IAREQ058.
- Acceptance: Given a token revoke, when the auditor reads the audit, then an entry shows with no secret.
- Evidence: `backend/src/Notrelix.API/Middleware/SecurityAuditMiddleware.cs` (path list includes `reset-password`) · Tests API `SecurityAuditMiddlewareTests` · Real e2e none
- State: IMPLEMENTING. Missing: the Governance reader, a per-action list proof for Identity paths.

## Capability L: API token lifecycle

Tokens are workspace-scoped credentials bound to the person who issued them (`ApiToken.Create` takes account, workspace and user). They are issued and revoked under the workspace route; the permission is `ManageApiTokens` on the workspace resource (Governance, see [workspace-governance](../teams/workspace-governance.md)).

### UC-IDN-L01 List workspace API tokens
- Capability: L API token lifecycle
- Actors and permission: Workspace Owner or Admin (`ManageApiTokens`); a person without it gets the same NotFound as a missing workspace.
- Preconditions: none.
- Input: workspace id; paging (to be added).
- Output: metadata only: id, name, safe suffix, status, created, expiry, creator; never the secret.
- Main flow: 1. The workspace security page reads `GET /api/v1/workspaces/{workspaceId}/api-tokens`.
- No-op / rejection / concurrency / retry: a read; another workspace's tokens never appear (RLS and authorization).
- UI states: loading, empty, success, permission denied, error.
- Rules: ID-004, ID-010, ID section 34; IAREQ059, IAREQ063, IAREQ067.
- Acceptance: Given two workspaces, when the first lists, then only its tokens show and no secret. Given a Member, then NotFound.
- Evidence: BE `ApiTokens/Queries/ListApiTokens/ListApiTokensQuery.cs` (no limit); API `ApiTokens/Queries/ListApiTokensEndpoint.cs` (`GET /api/v1/workspaces/{workspaceId}/api-tokens`) · FE none (generated schema only) · Design none · Tests Application `ApiTokenHandlerTests`, Integration `ApiTokenHttpFlowTests`, `ApiTokenScopeTests` · Real e2e none
- State: IMPLEMENTING. Missing: screen, artboard, paging, real spec.

### UC-IDN-L02 Issue an API token
- Capability: L API token lifecycle
- Actors and permission: Workspace Owner or Admin (`ManageApiTokens`); step-up of purpose `IssueApiToken`.
- Preconditions: a bound session; the email is confirmed (to be decided with workspace rules).
- Input: name (1 to the maximum length), expiry (required, see L06), step-up proof.
- Output: a token record; the raw secret returned once in the 201 body; only a hash is stored; a "token created" audit entry.
- Main flow: 1. The person chooses "New token". 2. They name it, pick an expiry and pass step-up. 3. The client posts `POST /api/v1/workspaces/{workspaceId}/api-tokens`. 4. The page shows the secret once with a copy button and a warning.
- No-op / rejection / concurrency / retry: an expiry in the past is refused; a used proof fails; a retry after a lost response makes a second token, so the page must show the secret before any other action and offer revoke; the secret is never logged.
- UI states: step-up prompt, validation error, success with one-time secret, permission denied.
- Rules: ID-010; sections 32 to 35; IAREQ059, IAREQ060, IAREQ062, IAREQ065.
- Acceptance: Given step-up, when a token is issued, then the secret is in the response once and a later list shows only the suffix.
- Evidence: BE `ApiTokens/Commands/CreateApiToken/CreateApiTokenCommand.cs` (`scopes: null`, expiry optional); API `ApiTokens/Commands/CreateApiTokenEndpoint.cs` (`POST /api/v1/workspaces/{workspaceId}/api-tokens`) · FE none · Design none · Tests Application `ApiTokenHandlerTests`, Domain `ApiTokenTests`, `TokenHashTests`, Integration `ApiTokenHttpFlowTests` · Real e2e none
- State: IMPLEMENTING. Missing: screen, artboard, required expiry (L06), scopes (L05), idempotency note.

### UC-IDN-L03 Revoke an API token
- Capability: L API token lifecycle
- Actors and permission: Workspace Owner or Admin (`ManageApiTokens`).
- Preconditions: the token belongs to the workspace.
- Input: workspace id, token id.
- Output: the token is Revoked and refused at once; an audit entry.
- Main flow: 1. The person chooses Revoke and confirms. 2. The client sends `DELETE /api/v1/workspaces/{workspaceId}/api-tokens/{tokenId}`.
- No-op / rejection / concurrency / retry: revoking a revoked token is a no-op success; a token of another workspace answers NotFound; the write needs no expected version because the only transition is one way and idempotent.
- UI states: destructive confirmation, success, not found.
- Rules: ID-010, ID-015; section 54; IAREQ066.
- Acceptance: Given a token, when it is revoked, then the next call with it is 401. Given a second revoke, then success and no second audit entry.
- Evidence: BE `ApiTokens/Commands/RevokeApiToken/RevokeApiTokenCommand.cs`; API `ApiTokens/Commands/RevokeApiTokenEndpoint.cs` (`DELETE /api/v1/workspaces/{workspaceId}/api-tokens/{tokenId}`) · FE none · Design none · Tests Application `ApiTokenHandlerTests`, Integration `ApiTokenFlowTests` · Real e2e none
- State: IMPLEMENTING. Missing: screen, artboard, real spec.

### UC-IDN-L04 Call the API with a token
- Capability: L API token lifecycle
- Actors and permission: an integration client holding a token; the acting principal is an API token (ID-013).
- Preconditions: an Active, unexpired token.
- Input: the token in the request credential.
- Output: a principal of kind API token with user, account and workspace claims; authorization still runs on every operation.
- Main flow: 1. The handler hashes the secret and finds the token in a token-scoped database context. 2. It records use of that one token. 3. It builds the principal. 4. Authorization decides as for the issuing user, within that workspace.
- No-op / rejection / concurrency / retry: an unknown, revoked or expired token answers 401 with no reason; a token never reaches another workspace.
- UI states: none.
- Rules: ID-010, ID-013; sections 33, 35; IAREQ061, IAREQ064.
- Acceptance: Given a valid token, when it calls a workspace route of its workspace, then the call is authorized as the issuer would be. Given another workspace, then NotFound.
- Evidence: Infrastructure `backend/src/Notrelix.Infrastructure/Auth/ApiTokens/ApiTokenAuthenticationHandler.cs` · Tests Integration `ApiTokenScopeTests`, `ApiTokenHttpFlowTests` · Real e2e none
- State: IMPLEMENTING. Missing: scope enforcement (L05), a decision that tokens may not reach person-level Identity routes (sessions, security, profile).

### UC-IDN-L05 Limit a token to chosen scopes
- Capability: L API token lifecycle
- Actors and permission: Workspace Owner or Admin issuing the token.
- Preconditions: a published scope vocabulary.
- Input: a list of scopes at issue.
- Output: a token that passes only calls inside its scopes, still subject to Governance.
- Main flow: not defined until the vocabulary exists.
- No-op / rejection / concurrency / retry: an empty scope list means no access, never full access; scopes cannot be widened after issue.
- UI states: scope picker, read-only list of scopes.
- Rules: ID-010, section 35; IAREQ065.
- Acceptance: Given a read-only scope, when the token writes, then 403.
- Evidence: Domain `backend/src/Notrelix.Domain/Identity/Tokens/ApiTokenScopes.cs` (value object; no consumer calls `Allows`) · Tests Domain `ApiTokenScopesTests`.
- State: BLOCKED. Needs a product decision on the scope vocabulary (per context, read and write) and on whether a token may call person-level routes; a token acting as a full user is a security-principal choice.

### UC-IDN-L06 Cap token lifetime (enterprise baseline)
- Capability: L API token lifecycle
- Actors and permission: system, at issue.
- Preconditions: none.
- Input: requested expiry.
- Output: an expiry is always set; the maximum is 365 days.
- Main flow: 1. Issue without expiry or beyond 365 days is refused with the limit.
- No-op / rejection / concurrency / retry: existing tokens with no expiry are listed with a "no expiry" warning until revoked.
- UI states: validation error.
- Rules: ID-010; GitHub offers a maximum-lifetime policy.
- Acceptance: Given no expiry, when issuing, then a validation error names the limit.
- Evidence: BE `CreateApiTokenCommand.cs` (expiry optional).
- State: DEFINED. Missing: rule and tests. Lead decision (2026-10-30): expiry required, at most 365 days.

### UC-IDN-L07 Show when a token was last used (enterprise baseline)
- Capability: L API token lifecycle
- Actors and permission: Workspace Owner or Admin.
- Preconditions: none.
- Input: none.
- Output: last-used time per token in the list.
- Main flow: 1. The handler records the use time (at most once per hour). 2. The list shows it.
- No-op / rejection / concurrency / retry: the write must not slow or fail the call.
- UI states: "never used".
- Rules: ID-010; section 49.
- Acceptance: Given a call with a token, when the list is read, then the last-used time shows.
- Evidence: Infrastructure `ApiTokenAuthenticationHandler.cs` records use of the token row; the list DTO does not expose it.
- State: DEFINED. Missing: DTO column, screen.

### UC-IDN-L08 Stop tokens of a person who leaves
- Capability: L API token lifecycle
- Actors and permission: system.
- Preconditions: the issuer is disabled (UC-IDN-D04) or removed from the workspace (Workspaces).
- Input: the membership or status change.
- Output: the person's tokens in that workspace (or all, when disabled) are revoked.
- Main flow: 1. The change commits. 2. Tokens of the person for the scope are revoked in the same unit of work or by a consumer of the completed fact.
- No-op / rejection / concurrency / retry: a retry revokes nothing more; business data the token created stays.
- UI states: tokens show as Revoked with the reason.
- Rules: ID-010, ID-016; sections 55, 69.
- Acceptance: Given a token of a removed member, when it calls, then 401.
- Evidence: none.
- State: DEFINED. Missing: rule, consumer. Lead decision (2026-10-30): tokens die with the issuer's workspace membership.

## Capability M: Identity profile

### UC-IDN-M01 Change my display name
- Capability: M Identity profile read and update
- Actors and permission: signed-in person; own profile only (IAREQ011).
- Preconditions: none.
- Input: name (required, at most 200); optional external avatar address.
- Output: the updated user; the sign-in email and credentials are not touched.
- Main flow: 1. The person edits the name on the Profile page. 2. The save bar appears after a change. 3. The client sends `PATCH /api/v1/profile`.
- No-op / rejection / concurrency / retry: an unchanged name is a no-op; an empty name is a validation error; the write carries no expected version (single owner, last write wins; decision below).
- UI states: pristine, dirty with save bar, pending, success, validation error.
- Rules: ID section 7 (display change never changes security identity); IAREQ010, IAREQ011.
- Acceptance: Given a new name, when saved, then it shows in `/auth/me`. Given a blank name, then a validation error.
- Evidence: BE `Profiles/Commands/UpdateProfile/UpdateProfile.cs`; API `Profile/Commands/UpdateProfileEndpoint.cs` (`PATCH /api/v1/profile`) · FE `.../account/profile.tsx`, `use-update-profile.ts` · Design Final-Account, Final-M-Account · Tests Application `UpdateProfileTests`, Domain `UserNameTests`, API `ProfileEndpointTests`; mock e2e `account-pages.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING. Missing: real spec; the endpoint also accepts an external avatar string, which duplicates M03.

### UC-IDN-M02 Read and save my preferences
- Capability: M Identity profile read and update
- Actors and permission: signed-in person; own preferences.
- Preconditions: none.
- Input: partial body with required `expectedVersion` (0 when never saved): locale, time zone, theme, phone, `phoneVisibleToWorkspaceMembers`.
- Output: the saved preferences with a new version.
- Main flow: 1. The page reads `GET /api/v1/profile/preferences`. 2. The person edits and saves with `PATCH /api/v1/profile/preferences`. 3. The phone is normalized to E.164 (`+` and 8 to 15 digits); a blank clears it.
- No-op / rejection / concurrency / retry: no change bumps no version; a stale version answers 412 and the page offers reload; a missing version is a validation error; an invalid phone is refused; the phone shows to others only under the rule of ID section 7.
- UI states: loading, pristine, dirty, pending, validation error, conflict, success.
- Rules: ID section 7 (D15); ID-015.
- Acceptance: Given two devices, when both save from version 3, then one succeeds and one gets 412 and changes nothing.
- Evidence: BE `Profiles/Queries/GetAccountPreferences`, `Profiles/Commands/UpdateAccountPreferences/UpdateAccountPreferences.cs`; API `Profile/Commands/AccountPreferencesEndpoints.cs` (`GET`, `PATCH /api/v1/profile/preferences`) · FE `.../account/profile.tsx`, `appearance.tsx`, `packages/features/account/src/core/phone.ts` · Design Final-Account, Final-M-Account · Tests Domain `UserProfileAccountPreferencesTests`, `UserPreferencesTests`, Application `UpdateAccountPreferencesTests`, `GetAccountPreferencesTests`, API `AccountPreferencesEndpointTests`, FE `phone.unit.test.ts` · Real e2e none
- State: INTEGRATING. Missing: real spec.

### UC-IDN-M03 Upload my profile photo
- Capability: M Identity profile read and update
- Actors and permission: signed-in person; own photo.
- Preconditions: none.
- Input: an image of at most 2,000,000 bytes; PNG, JPEG or WebP recognized from the bytes.
- Output: the photo stored; the previous object is left for the background sweep.
- Main flow: 1. The person picks a file. 2. The client sends `PUT /api/v1/profile/avatar`. 3. The page shows the new photo.
- No-op / rejection / concurrency / retry: SVG, GIF or any other type answers 415 `identity.avatar.unsupported-format`; too large answers 413 `identity.avatar.too-large`; changing the name never touches the photo.
- UI states: pending, success, unsupported, too large.
- Rules: ID section 7 (photo).
- Acceptance: Given a PNG renamed `.jpg`, when uploaded, then it is accepted by its bytes; given an SVG, then 415.
- Evidence: BE `Profiles/Commands/UploadAvatar/UploadAvatar.cs`; API `Profile/Commands/AvatarEndpoints.cs` (`PUT /api/v1/profile/avatar`) · FE `.../account/profile.tsx` · Design Final-Account · Tests Application `AvatarCommandsTests`, API `AvatarEndpointTests`, Domain `UserAvatarTests`; mock e2e `account-pages.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING. Missing: real spec.

### UC-IDN-M04 Remove my profile photo
- Capability: M Identity profile read and update
- Actors and permission: signed-in person; own photo.
- Preconditions: none.
- Input: none.
- Output: no photo; the object is swept later.
- Main flow: 1. The person chooses Remove. 2. The client sends `DELETE /api/v1/profile/avatar`.
- No-op / rejection / concurrency / retry: removing when none exists is a no-op.
- UI states: destructive confirmation, success.
- Rules: ID section 7.
- Acceptance: Given a photo, when removed, then a read of it answers 404.
- Evidence: BE `Profiles/Commands/RemoveAvatar/RemoveAvatar.cs`; API `Profile/Commands/AvatarEndpoints.cs` (`DELETE /api/v1/profile/avatar`) · FE `.../account/profile.tsx` · Design Final-Account · Tests Application `AvatarCommandsTests`, API `AvatarEndpointTests` · Real e2e none
- State: INTEGRATING. Missing: real spec; proof that the sweep removes only unreferenced objects.

### UC-IDN-M05 Read a person's photo
- Capability: M Identity profile read and update
- Actors and permission: the person, or an active non-guest member of a Workspace they share.
- Preconditions: an uploaded photo.
- Input: user id; version in `v`.
- Output: the image streamed with `nosniff`, a locked-down CSP and private immutable cache.
- Main flow: 1. A face renders `GET /api/v1/users/{userId}/avatar?v=...`.
- No-op / rejection / concurrency / retry: any other reader, and any person without an uploaded photo, gets the same 404; a share link or public view never reads it.
- UI states: image, initials fallback.
- Rules: ID section 7; ID-012.
- Acceptance: Given a reader who shares no workspace, when they read it, then 404 identical to "no photo".
- Evidence: BE `Profiles/Queries/GetUserAvatar`; API `Users/MapUserAvatarEndpoints.cs` (`GET /api/v1/users/{userId}/avatar`) · FE `packages/features/account/src/web/profile/account-profile-surface.tsx` · Design Final-Account · Tests Application `GetUserAvatarTests`, API `AvatarEndpointTests` · Real e2e none
- State: INTEGRATING. Missing: real spec, cross-workspace negative at the HTTP level.

# 4. Product decisions applied

- Product decision (owner, 2026-10-27, item 47): mobile uses a bearer and a rotating refresh token in the body selected by `X-Notrelix-Client: mobile`; web stays cookie plus CSRF; tokens live only in Keychain or Keystore; SSO through the system browser and push are deferred (UC-IDN-A04).
- Product decision (owner, 2026-10-27, item 48): a device is a session; a session shows client address and user agent only; revoke-others needs step-up; accent and sidebar are client-only; no account deletion and no data export yet (UC-IDN-C02, C04; Part 6).
- Product decision (owner, 2026-10-27, item 53): sign-in keeps email and password in one form; the password field opens with a height and opacity reveal; focus moves on; reduced motion is respected (UC-IDN-B01, B02).
- Product decision (owner, 2026-10-23, accepted D10, D15 in the product doc): lockout values, security activity and the phone rules are product values already in `docs/product/identity.md`; used by UC-IDN-B09, K02, M02.
- Lead decision (2026-10-30): password reset mail carries a one-time code; the screen takes email, code and new password; the web copy says "code" not "link" (UC-IDN-B07). A code dies after five wrong tries.
- Lead decision (2026-10-30): a rotated refresh credential stays usable for 10 seconds; a use after that revokes the whole chain (UC-IDN-D03).
- Lead decision (2026-10-30): a session chain lives at most 90 days and at most 30 days idle (UC-IDN-D05).
- Lead decision (2026-10-30): disabling a person revokes every session and every API token of theirs in the same unit of work (UC-IDN-D04, L08).
- Lead decision (2026-10-30): signing out one other device needs no step-up; signing out all others does (UC-IDN-C03, C04).
- Lead decision (2026-10-30): a sign-in email change needs step-up, is confirmed at the new address before it takes effect, and notifies the old address (UC-IDN-K09).
- Lead decision (2026-10-30): a provider-only person may set a first password through provider step-up (UC-IDN-K08).
- Lead decision (2026-10-30): the person is mailed for password change or reset, second-factor change, provider link or unlink, other-sessions revoked, email change, and a sign-in from a client not seen in 90 days (UC-IDN-K10).
- Lead decision (2026-10-30): an API token always has an expiry of at most 365 days; tokens die with the issuer's workspace membership (UC-IDN-L06, L08).
- Lead decision (2026-10-30): the display-name write carries no expected version (own data, one writer at a time in practice); preferences keep their version (UC-IDN-M01, M02).
- Lead decision (2026-10-30): job title stays a Workspaces fact (product doc section 7 and the workspaces doc, D15). Owner decision 48 of 2026-10-27 says "job title in the Identity profile"; the shipped code and the product docs put it in Workspaces, so this catalog follows them and Part 5 asks the owner to confirm.
- Lead decision (2026-10-30): enterprise single sign-on uses OIDC first, SAML later, one provider per Account, with sign-in enforced per verified domain; the enforcement fact lives in Accounts (UC-IDN-B08, still BLOCKED until Accounts adds it).

# 5. Canonical updates required

For `docs/product/identity.md`:

- Section 13 (password reset): "The reset message carries a one-time code. The person enters email, code and a new password. Five wrong codes kill the code."
- Section 18 (refresh rotation): "A rotated refresh credential stays usable for 10 seconds so two tabs may refresh together. A use after that revokes every session of that chain. A session chain lives at most 90 days and at most 30 days idle; an Account policy may shorten both."
- Section 19 (revocation): "Signing out another single device needs no step-up. Signing out all other devices needs step-up."
- Section 55 (user disable): "Disabling a person revokes every session and every API token of theirs in the same unit of work, and sign-in is refused with the generic failure."
- Section 63 (login identifier): "Changing the sign-in email needs step-up. The new address becomes the sign-in email only after the person confirms it there. The old address is told. Other sessions end on confirmation. A taken address gets the same neutral answer as a free one."
- Section 12 (password): "A person who signs in only through a provider may set a first password after provider step-up."
- Section 49 (audit) or a new section 49A (security mail): "The person receives mail for password change or reset, second-factor change, provider link or unlink, other sessions revoked, email change and a sign-in from a client not seen in 90 days. A mail never fails the event and holds no secret."
- Section 32 to 35 (API token): "A token has an expiry of at most 365 days. Last use is shown to workspace administrators. A token ends when its issuer leaves the workspace or is disabled. Scopes are chosen at issue and enforced; an empty list grants nothing." The scope vocabulary waits on a decision (UC-IDN-L05).
- Section 30 (account-level SSO): "Enterprise sign-in uses OIDC, then SAML, one provider per Account, enforced per verified domain. Accounts owns the enforcement fact; Identity authenticates."
- Section 7: reconcile owner decision 48 (job title) with the Workspaces ownership of the job title.
- Section 23 or 25: the Account policy "require MFA" and who may reset another person's second factor (UC-IDN-J04, J05).

For `docs/product/accounts.md` (via [accounts](accounts.md)): the identity-provider enforcement flag, the verified-domain rule and the "require MFA" policy fact.

For `docs/workstreams/teams/identity-accounts.md`: add "registration and email confirmation", "password recovery" and "device list" to the Identity capability list (section 4), so letters B and C need no note.

# 6. Not covered

| Product-doc section | Reason |
|---|---|
| Sections 1 to 3, 10, 11, 21, 36, 37, 71 to 77 (mission, vocabulary, checklists, source alignment, stop conditions) | Principles and checklists, not user actions; they are cited by the rules lines above. |
| Sections 4, 5, 6, 63 (stable principal, email is not identity) | Held by every use case that changes the email or links a provider (UC-IDN-K09, F02); no separate action exists. |
| Section 8 and 56 (deleted or anonymized User), 9, 57 | Owner decision 48: no account deletion or data export yet. The Domain `User` has soft delete; no use case is offered until the owner decides retention. |
| Section 29, 45 (OAuth for external business providers) | Integrations owns it; see [automation-integrations](automation-integrations.md). |
| Sections 30, 31 (account IdP configuration) and SCIM directory | Accounts owns configuration; Identity only authenticates (UC-IDN-B08) and ends access (UC-IDN-D04); see [accounts](accounts.md). |
| Sections 40 to 44, 60, 61, 68 to 70 (relations to Accounts, Workspaces, Governance, Billing, workspace switch, suspension, entitlement) | Boundaries, not actions. The default workspace is an Identity fact on the profile but its choice is validated and cleared by Workspaces; it is covered in [workspaces](workspaces.md). The sign-in email port `IIdentityUserFacts` is a consumer port covered by the Workspaces members use cases. |
| Sections 46, 47, 48 (service and system principals, acting principal) | Automation and platform own execution principals; see [automation-integrations](automation-integrations.md) and [platform-foundation](platform-foundation.md). API token principals are in UC-IDN-L04. |
| Section 50, 51 (events and facts) | The only cross-boundary Identity fact with a consumer is registration completed (UC-IDN-B03); other facts wait for a consumer. |
| Section 64, 65 (identity merge) | Not supported on purpose. A colliding provider or email is refused (UC-IDN-F02); merge needs a product and security policy first. |
| Invited registration of a guest | The invitation belongs to Workspaces; Identity only registers the person; see [workspaces](workspaces.md). |
| Passkeys (WebAuthn), SCIM-driven Identity changes | Enterprise baseline items without product rule or owner demand; listed in the baseline table of Part 1. |

## Endpoints with no use case

None. Every route under `backend/src/Notrelix.API/Endpoints/Identity/` appears in the Evidence line of a use case above. The two token transport headers and the default workspace choice are outside this endpoint set.
