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

| Letter | Capability (team spec) | Use cases | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | DONE | BLOCKED |
|---|---|---|---|---|---|---|---|---|
| A | Session bootstrap | 4 | 1 | 1 | 0 | 2 | 0 | 0 |
| B | Login (with registration, email confirmation, password recovery, company SSO) | 9 | 0 | 0 | 0 | 6 | 2 | 1 |
| C | Logout (with the device list) | 4 | 0 | 0 | 0 | 0 | 4 | 0 |
| D | Session expiration | 5 | 0 | 1 | 1 | 1 | 2 | 0 |
| E | OAuth start | 1 | 0 | 0 | 1 | 0 | 0 | 0 |
| F | OAuth callback | 2 | 0 | 0 | 2 | 0 | 0 | 0 |
| G | Provider linking and unlinking | 2 | 0 | 2 | 0 | 0 | 0 | 0 |
| H | MFA enrollment | 3 | 0 | 0 | 0 | 0 | 3 | 0 |
| I | MFA challenge | 2 | 0 | 0 | 0 | 1 | 1 | 0 |
| J | MFA recovery (with disable) | 5 | 0 | 0 | 0 | 0 | 3 | 2 |
| K | Security settings (with step-up, password, sign-in email) | 11 | 2 | 2 | 3 | 1 | 3 | 0 |
| L | API token lifecycle | 8 | 3 | 4 | 0 | 0 | 0 | 1 |
| M | Identity profile read and update | 5 | 0 | 0 | 5 | 0 | 0 | 0 |
| | Total | 61 | 6 | 10 | 12 | 11 | 18 | 4 |

Eighteen use cases are DONE (B04, B05, C01, C02, C03, C04, D02, D03, H01, H02, H03, I02, J01, J02, J03, K03, K04, K05). C01, C02, C04, D03, H03 and J02 were accepted 2026-11-02 by an independent acceptance agent on all five lines (their Evidence lines carry the run). A second independent run on develop `876ebfb57` accepted B04, B05, I02 and J01 and held B06, B07, B09, D01 and I01, each with its gap in the State line: the brand panel of the drawn Final-Auth screens is not within 1 px (logo box, brand name size, tagline 8 px high and one weight light; no check measures it), a reset followed by a sign-in in the same clock second gets an access token the server refuses (it makes `password-reset.real.e2e.spec.ts` fail about one run in six), and the offline retry screen's button is 36 px at 390 px. Of the earlier eight, six were accepted 2026-11-01 by an acceptance agent against §7 of the catalog README, and H02 and K03, rejected that day on one line each, accepted again by the fix lane that closed those lines (the lead reviews their Evidence lines at merge); the rest have no recorded owner check or 1 px design diff yet. HARDENING means a `frontend/e2e/real` spec runs the flow against the real backend: the password sign-in (every real spec signs in through `frontend/e2e/real/support/real-session.ts`) the sign-up with email confirmation (`email-confirmation.real.e2e.spec.ts`), the session read and a session revoked from another device (`session-bootstrap.real.e2e.spec.ts`: A01, A03, C03, D02) and turning two-factor on and off with step-up (`mfa-enrollment-step-up.real.e2e.spec.ts`: H01, H02, J03, K03, K04, K05) and a person with only a provider (`provider-step-up.real.e2e.spec.ts`: K03, K06; no provider runs on the development stack, so only the offer and the refusal run). The provider sign-in has a real spec only for the refusal (`microsoft-sign-in.real.e2e.spec.ts`), so E01 stays INTEGRATING. Since 2026-11-02 the real stack also runs password reset with the ten-wrong-password lock (`password-reset.real.e2e.spec.ts`: B06, B07, B09), the second step at sign-in, recovery codes and their renewal (`second-step-sign-in.real.e2e.spec.ts`: H03, I01, I02, J01, J02), the confirmation link sent again (`email-confirmation-resend.real.e2e.spec.ts`: B05; `email-confirmation.real.e2e.spec.ts` was fixed to read the masked address of the development log: B04), and sign-out, the device list, sign-out of all other devices and refresh rotation with replay (`sign-out-and-sessions.real.e2e.spec.ts`: C01, C02, C04, D01, D03). Nothing else in Identity has a real end-to-end spec.

## 1.2 The ten most important gaps

1. **Password reset could not be finished in the web app.** Built: the reset screen takes email, code and new password, the sent step says code, and a real spec runs the whole reset (UC-IDN-B06, B07). The unused `createUseResetPassword` hook is still in the package.
2. **API token scopes are never enforced.** Tokens are issued with `scopes: null` and the authentication handler builds a principal for the issuing user with the workspace claim; no code reads `ApiTokenScopes.Allows`. ID-010 says a token is never "full user access" (UC-IDN-L05).
3. **No API token UI.** Three endpoints exist, no screen (UC-IDN-L01 to L03).
4. **Company single sign-on is a stub.** `IdentifySignIn` always answers "password"; the mock backend invents an SSO path that the real backend cannot serve (UC-IDN-B08).
5. **Refresh had no replay detection and no absolute lifetime.** Built: a replayed credential outside the 10-second window ends the session, and a session ends 90 days after sign-in however often it is used (UC-IDN-D03, D05). Tests and owner check pending.
6. **Disabling a person does not end their access.** Domain `Deactivate` and `Suspend` exist; no use case calls them; sessions and access tokens are not revoked, only a refresh is blocked (UC-IDN-D04, L08).
7. **Revoke-other-sessions has no step-up** although owner decision 48 requires one (UC-IDN-C04). Built; tests pending.
8. **Changing the sign-in email needs no proof.** No step-up, no notice to the old address, no session revocation, and no web screen (UC-IDN-K09).
9. **Provider link and unlink have no screen**, and the security page cannot show or change linked providers (UC-IDN-G01, G02).
10. **No mobile token transport** (owner decision 47, `X-Notrelix-Client: mobile`): the backend does not read the header, and refresh reads only the cookie (UC-IDN-A04).

## 1.3 Defects in shipped code

| Defect | Where | Use case |
|---|---|---|
| Reset flow mismatch (fixed in this build): email carries a code, the page promised a link, no reset page | `backend/src/Notrelix.Application/Features/Identity/Auth/Commands/ForgotPassword/ForgotPassword.cs`; `frontend/packages/features/auth/src/web/components/auth-forgot-surface.tsx`; `frontend/apps/web/src/routes/` | B07 |
| Forgot-password answers an error only when the account exists and the mail fails to send: a side channel that names real accounts | `ForgotPassword.cs` (`email-send-failed` branch) | B06 |
| Register answers "Email is already in use" (accepted trade-off, owner sign-off pending) | `backend/src/Notrelix.Application/Features/Identity/Registration/Commands/Register/RegisterCommandHandler.cs` | B03 |
| Same disclosure on email change, to a signed-in person | `Profiles/Commands/UpdateEmail/UpdateEmail.cs` | K09 |
| Email change: no step-up, no notice to the old address, login email changes before the new one is proven | `UpdateEmail.cs`; purpose `ChangeSecurityIdentity` exists in `Security/DTOs/StepUpTypes.cs` and nothing uses it | K09 |
| Revoke-others needs no step-up (owner decision 48): built, tests pending | `Sessions/Commands/RevokeOtherSessions/RevokeOtherSessions.cs` | C04 |
| API token scopes are not enforced; `scopes: null` at issue; expiry optional (a token may never expire) | `ApiTokens/Commands/CreateApiToken/CreateApiTokenCommand.cs`; `backend/src/Notrelix.Infrastructure/Auth/ApiTokens/ApiTokenAuthenticationHandler.cs` | L02, L04, L05 |
| Unbounded reads: the session list returned every session ever created (bounded now, tests pending); the token list has no page | `Sessions/Queries/GetUserSessions/GetUserSessionsQuery.cs`; `ApiTokens/Queries/ListApiTokens/ListApiTokensQuery.cs` | C02, L01 |
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
| UC-IDN-A01 | Read the signed-in person | A | Signed-in person | yes | yes | Final-Auth | yes | HARDENING | tests and owner check |
| UC-IDN-A02 | Bootstrap read for a client | A | Signed-in person | yes | no | none | no | IMPLEMENTING | consumers exist (onboarding, account page); the payload carries workspaces with roles, which the acceptance line forbids: decision needed |
| UC-IDN-A03 | Get and send the CSRF token | A | Browser client | yes | yes | n/a | yes | HARDENING | tests and owner check |
| UC-IDN-A04 | Mobile token transport | A | Mobile client | no | no | Final-M-Auth | no | DEFINED | header and body transport absent |
| UC-IDN-B01 | Identify the sign-in method | B | Visitor | yes | yes | Final-Auth | yes | HARDENING | tests and owner check; the SSO branch waits on B08 |
| UC-IDN-B02 | Sign in with email and password | B | Visitor | yes | yes | Final-Auth | yes | HARDENING | tests and owner check |
| UC-IDN-B03 | Register | B | Visitor | yes | yes | Final-Auth | yes | HARDENING | tests, owner check, email disclosure decision |
| UC-IDN-B04 | Confirm the email from the link | B | Visitor, signed-in person | yes | yes | Final-Auth | yes | DONE | accepted 2026-11-02, see the Evidence line |
| UC-IDN-B05 | Request or resend the confirmation | B | Visitor, signed-in person | yes | yes | Final-Auth | yes | DONE | accepted 2026-11-02, see the Evidence line |
| UC-IDN-B06 | Ask for a password reset | B | Visitor | yes | yes | Final-Auth | yes | HARDENING | lines 1-4 PASS, awaiting acceptance |
| UC-IDN-B07 | Complete a password reset | B | Visitor | yes | no | none | yes | HARDENING | lines 1-4 PASS, awaiting acceptance |
| UC-IDN-B08 | Sign in through the company provider | B | Visitor | no | mock only | Final-Auth | no | BLOCKED | Accounts enforcement fact, protocol |
| UC-IDN-B09 | Lock sign-in after repeated failures | B | Visitor | yes | yes | Final-Auth | yes | HARDENING | lines 1-4 PASS, awaiting acceptance |
| UC-IDN-C01 | Sign out this device | C | Signed-in person | yes | yes | Final-Account | yes | DONE | accepted 2026-11-02, see the Evidence line |
| UC-IDN-C02 | List my sessions and devices | C | Signed-in person | yes | yes | Final-Account | yes | DONE | accepted 2026-11-02, see the Evidence line |
| UC-IDN-C03 | Sign out one other device | C | Signed-in person | yes | yes | Final-Account | yes | DONE | accepted 2026-11-01, see the Evidence line |
| UC-IDN-C04 | Sign out all other devices | C | Signed-in person | yes | yes | Final-Account | yes | DONE | accepted 2026-11-02, see the Evidence line |
| UC-IDN-D01 | Refresh the session silently | D | Signed-in client | yes | yes | n/a | yes | HARDENING | lines 1-4 PASS, awaiting acceptance |
| UC-IDN-D02 | Session expired or revoked | D | Signed-in person | yes | yes | Final-Auth | yes | DONE | accepted 2026-11-01, see the Evidence line |
| UC-IDN-D03 | Reject a replayed refresh credential | D | System | yes | n/a | n/a | yes | DONE | accepted 2026-11-02, see the Evidence line |
| UC-IDN-D04 | End access when a person is disabled | D | System | partial | no | none | no | IMPLEMENTING | no caller, no revocation |
| UC-IDN-D05 | Bound the absolute session lifetime | D | System | yes | n/a | n/a | no | INTEGRATING | Domain tests; no real proof of a 91-day chain |
| UC-IDN-E01 | Start Google or Microsoft sign-in | E | Visitor | yes | yes | Final-Auth | no | INTEGRATING | tests and owner check; real run needs provider credentials |
| UC-IDN-F01 | Complete provider sign-in | F | Visitor | yes | yes | Final-Auth | no | INTEGRATING | tests and owner check; real run needs provider credentials |
| UC-IDN-F02 | Refuse a stale, replayed or colliding callback | F | Visitor | yes | yes | none | no | INTEGRATING | tests, owner check and an artboard |
| UC-IDN-G01 | Link a provider to my account | G | Signed-in person | yes | no | none | no | IMPLEMENTING | no screen |
| UC-IDN-G02 | Unlink a provider | G | Signed-in person | yes | no | none | no | IMPLEMENTING | no screen |
| UC-IDN-H01 | Start second-factor enrollment | H | Signed-in person | yes | yes | Final-Account | yes | DONE | accepted 2026-11-01, see the Evidence line |
| UC-IDN-H02 | Verify and activate the second factor | H | Signed-in person | yes | yes | Final-Account | yes | DONE | accepted 2026-11-01 by the fix lane for the lead to review; wrong-code and backup-codes states not drawn (reviewed) |
| UC-IDN-H03 | Read the second-factor status | H | Signed-in person | yes | yes | Final-Account | yes | DONE | accepted 2026-11-02, see the Evidence line |
| UC-IDN-I01 | Answer the second step at sign-in | I | Visitor | yes | yes | Final-Auth | yes | HARDENING | lines 1-4 PASS, awaiting acceptance |
| UC-IDN-I02 | Expire a challenge after wrong codes | I | Visitor | yes | yes | Final-Auth | yes | DONE | accepted 2026-11-02, see the Evidence line |
| UC-IDN-J01 | Sign in with a recovery code | J | Visitor | yes | yes | Final-Auth | yes | DONE | accepted 2026-11-02, see the Evidence line |
| UC-IDN-J02 | Regenerate recovery codes | J | Signed-in person | yes | yes | Final-Account | yes | DONE | accepted 2026-11-02, see the Evidence line |
| UC-IDN-J03 | Turn the second factor off | J | Signed-in person | yes | yes | Final-Account | yes | DONE | accepted 2026-11-01; the policy-blocked refusal waits on J05 |
| UC-IDN-J04 | Recover an account that lost both factors | J | Person, Account admin | no | no | none | no | BLOCKED | who may reset another person |
| UC-IDN-J05 | Enforce an account-required second factor | J | System | no | no | none | no | BLOCKED | Accounts policy fact |
| UC-IDN-K01 | Read my security summary | K | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | tests and owner check |
| UC-IDN-K02 | Read my security activity | K | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | tests and owner check |
| UC-IDN-K03 | Ask which step-up factor applies | K | Signed-in person | yes | yes | Final-Account | yes | DONE | accepted 2026-11-01 by the fix lane for the lead to review; both provider prompts are undrawn (reviewed) |
| UC-IDN-K04 | Step up with a second-factor code | K | Signed-in person | yes | yes | Final-Account | yes | DONE | accepted 2026-11-01, see the Evidence line |
| UC-IDN-K05 | Step up with the password | K | Signed-in person | yes | yes | Final-Account | yes | DONE | accepted 2026-11-01, see the Evidence line |
| UC-IDN-K06 | Step up with a linked provider | K | Signed-in person | yes | yes | none | yes | HARDENING | no provider runs on the development stack, so the success round trip is proven with a fake provider and the mock; the real spec covers the refusal |
| UC-IDN-K07 | Change my password | K | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | tests and owner check |
| UC-IDN-K08 | Set a first password (enterprise baseline) | K | Signed-in person | no | no | none | no | DEFINED | provider-only accounts |
| UC-IDN-K09 | Change my sign-in email | K | Signed-in person | yes | no | none | no | IMPLEMENTING | step-up built; pending-email model, old-address notice, neutral answer, screen, artboard |
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
| UC-IDN-M01 | Change my display name | M | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | tests and owner check |
| UC-IDN-M02 | Read and save my preferences | M | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | tests and owner check |
| UC-IDN-M03 | Upload my profile photo | M | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | tests and owner check |
| UC-IDN-M04 | Remove my profile photo | M | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | tests and owner check |
| UC-IDN-M05 | Read a person's photo | M | Signed-in person | yes | yes | Final-Account | no | INTEGRATING | tests and owner check |

# 3. Use cases

Common facts, stated once. Every authenticated endpoint is `MapAuthenticated*`; a missing or expired session answers 401. Every unsafe request carries the CSRF header (UC-IDN-A03). Evidence paths are relative: `BE` under `backend/src/Notrelix.Application/Features/Identity/` unless it starts with `Notrelix.`; `API` under `backend/src/Notrelix.API/Endpoints/Identity/`; `FE` under `frontend/`; tests under `backend/tests/Notrelix.<Layer>.Tests/`. Real e2e: only `email-confirmation.real.e2e.spec.ts`, `session-bootstrap.real.e2e.spec.ts` and the sign-in helper `real-session.ts` exist; "none" means no spec. Owner checks and 1 px diffs are missing for every use case, so the State lines say only what else is missing.

## Capability A: Session bootstrap

### UC-IDN-A01 Read the signed-in person
- Capability: A Session bootstrap
- Actors and permission: any signed-in person; authentication only (ID-012), no Governance permission.
- Preconditions: a valid session cookie or token.
- Input: none.
- Output: id, email, name, avatar address, email-confirmed flag of the caller only.
- Main flow: 1. The client calls `GET /api/v1/auth/me` on start and after a sign-in. 2. The server resolves the user from the session. 3. The client holds the answer as the principal and routes on.
- No-op / rejection / concurrency / retry: no session or a revoked session answers 401; a session of a person who no longer exists answers 401 `identity.auth.session-invalid`; a read, so retry is safe.
- UI states: loading (bootstrap, announced), error (the session could not be read: the person stays on the page and may retry), session expired (sign-in with the page remembered). No empty, no conflict.
- Rules: ID-012, section 58 and 59; IAREQ018.
- Acceptance: Given a signed-in person, when the client reads `/auth/me`, then only their own fields return and no workspace role or entitlement is in the answer. Given a revoked session, when the client reads it, then the answer is 401 and the app shows the sign-in page.
- Evidence: BE `Auth/Queries/GetCurrentUser`; API `Auth/Queries/GetCurrentUserEndpoint.cs` (`GET /api/v1/auth/me`) · FE `packages/features/auth/src/web/hooks/use-auth-user.ts`, `apps/web/src/shell/guards/auth-guard.tsx` · Design Final-Auth (sign-in page; no artboard draws the session check or the retry screen) · Tests Application `GetCurrentUserTests` (4), Integration `GetCurrentUserQueryHandlerTests` (2), API `AuthenticatedAuthEndpointTests`, `GetCurrentUserErrorContractTests`; FE `auth-guard.component.test.tsx` (6) · Real e2e `session-bootstrap.real.e2e.spec.ts` (own facts only, no session 401, revoked session 401 then sign-in with the page remembered)
- State: HARDENING. Missing: tests and owner check (the tests above are written and have not been run together); design: an artboard for the session check and retry screens (the build uses the shared loading and error states). A disabled or suspended person still reads this endpoint until UC-IDN-D04.

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
- Evidence: BE `Auth/Queries/GetBootstrap/GetBootstrap.cs`; API `Auth/Queries/GetBootstrapEndpoint.cs` (`GET /api/v1/auth/bootstrap`) · FE `packages/features/workspace/src/web/onboarding/onboarding-api.ts`, `packages/features/account/src/core/api/account.service.ts` (they read `accountId`) · Design none · Tests Application `GetBootstrapTests`, Integration `GetBootstrapQueryHandlerTests` · Real e2e none
- State: IMPLEMENTING. Missing: the acceptance line says the answer holds no workspace role and no entitlement, but `BootstrapResult` returns `Workspaces` with `Role` and `AccountId` and the web app uses `AccountId`. Either the acceptance line or the payload changes (ID-017, product decision); not touched in this pass.

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
- Evidence: API `Auth/Commands/IssueCsrfTokenEndpoint.cs` (`GET /api/v1/auth/csrf`) · FE `packages/foundation/contracts/src/client/` (`csrf-transport`) · Design n/a · Tests API `CsrfProtectionTests`, `CsrfCrossStackFlowTests`, `CsrfEnforcementDefaultsTests`; FE `csrf-transport.unit.test.ts` · Real e2e `session-bootstrap.real.e2e.spec.ts` (no header and wrong header answer 403 and change nothing; a stale token is refused once, recovered once, and the write lands once), and every real spec writes through the transport
- State: HARDENING. Missing: tests and owner check (the real spec is written and has not been run; the real backend run now keeps the CSRF gate on, which development settings turn off). The staging and production deployment smoke (certification item P14-MIG-003) needs a deployed stack; none exists yet.

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
- Main flow: 1. The person fills the form. 2. The server creates the User and the session. 3. The client opens the app: the personal workspace once its provisioning has run (UC-WSP-A02), the onboarding page until then; creating a further workspace is refused until the email is confirmed (UC-IDN-B04).
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
- Evidence: BE `Verification/Commands/ConfirmEmail/ConfirmEmail.cs`; API `Auth/Commands/EmailVerificationEndpoints.cs` (`POST /api/v1/auth/email-verification/confirm`) · FE `packages/features/auth/src/web/components/auth-verify-email-surface.tsx`, route `verify-email.tsx` · Design Final-Auth · Tests Application `ConfirmEmailTests`; Domain `EmailVerificationTokenTests` · Real e2e `email-confirmation.real.e2e.spec.ts` Independent acceptance 2026-11-02 by an agent that did not build it, on develop `876ebfb57` plus the tests this run added (branch accept/identity-b4). Backend: Application `ConfirmEmailTests` 5 passed (the run added an active token past its expiry, which fails, confirms nothing and expires the token, and a used token, which fails as an unknown one does); Domain `UserActorSemanticsTests` 16 passed (+1: confirming an already confirmed email bumps no version and raises no event); Integration `CredentialScopeTests.ConfirmEmail_UnderTheAppRole_ConfirmsOnlyTheUserTheTokenBelongsTo` (unknown token, replay, another person's token). Integration `LoginThrottleIntegrationTests`, `MfaFlowTests`, `RefreshTokenCommandHandlerTests` and `CredentialScopeTests` 64 passed in one run (Testcontainers: Postgres and Redis). Frontend: vitest web on `packages/features/auth` 52 passed. Design: no artboard draws this page (Final-Auth `het-han` is the invitation, not this link), so it is built on the pattern of the forgot page; confirming, confirmed, dead link, new-link form and sent were captured at 1440, 1024 and 390 in light and dark on the real stack and every contact sheet was opened: no overflow, clipping or squeezed column, brand mark visible at 390, no target under 44 px at 390, axe no serious or critical violation. Real e2e: `email-confirmation.real.e2e.spec.ts` passed 6 of 6 across runs of the real stack (compose project notrelix-fe-real-e2e): sign-up, the unconfirmed refusal, the link, the single-use dead link. Defect listed, not held against this use case: an active token past its expiry answers 'Verification token has expired.' (`ConfirmEmail.cs:73`) where an unknown or used token answers 'Invalid or expired verification token.'; the page shows the same dead-link state for both. Independent acceptance: every line in §7 held.
- State: DONE — accepted by independent acceptance 2026-11-02. Built: the endpoint carries the `AuthCredentialByIp` limit (429 documented). Real spec `email-confirmation.real.e2e.spec.ts` passes again: it expected the onboarding page right after sign-up, but registering also provisions the personal workspace (UC-WSP-A02, an event consumer), so the app opens the personal workspace once that has run and the onboarding page until then; the spec accepts either and does its work from `/onboarding`. Known race for the Workspaces owner: a new member can see onboarding for a moment before the workspace exists. Copy (2026-11-02): a live link past its expiry answers `identity.email-verification.token-expired` (still 400) and the page says 'This link has expired' with the new-link form; an unknown or used link keeps the generic answer and the dead-link page (`ConfirmEmailTests` 4, web flow tests +2).

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
- Evidence: BE `Verification/Commands/RequestEmailVerification`, `ResendEmailVerification`; API `Auth/Commands/EmailVerificationEndpoints.cs` (`POST /api/v1/auth/email-verification/request`, `POST /api/v1/auth/email-verification/resend`) · FE `packages/features/auth/src/core/api/auth-flow.service.ts` · Design Final-Auth · Tests Application `RequestEmailVerificationTests`, `ResendEmailVerificationTests` · Real e2e `email-confirmation-resend.real.e2e.spec.ts` (1 passed: the onboarding notice sends the link again, says so and waits with the time left; a third ask from the resend route replaces the second; the first and second links are refused with the same 400 and only the newest confirms; an unknown and a known address get the same answer; a confirmed address sends nothing). Independent acceptance 2026-11-02 by an agent that did not build it, on develop `876ebfb57` plus the tests this run added (branch accept/identity-b4). Backend: Application `ResendEmailVerificationTests` 4, `RequestEmailVerificationTests` 3 and `EmailVerificationTokenIssuerTests` 1 passed; Integration `CredentialScopeTests.ResendVerification_UnderTheAppRole_IssuesATokenForThatUserOnly` (a known and an unknown address answer alike). Integration `LoginThrottleIntegrationTests`, `MfaFlowTests`, `RefreshTokenCommandHandlerTests` and `CredentialScopeTests` 64 passed in one run (Testcontainers: Postgres and Redis). Frontend: vitest web on `packages/features/auth` 52 passed. Design: no artboard draws these states; the onboarding notice before and after sending (with the countdown) and the verify page's form and sent state were captured at 1440, 1024 and 390 in light and dark on the real stack and every contact sheet was opened: no overflow or clipping, the header and step bar do not squeeze at 390, 'Send the link again' and 'Send a new link' are at least 44 px at 390, axe no serious or critical violation. Real e2e: `email-confirmation-resend.real.e2e.spec.ts` passed 5 of 6 across runs of the real stack; the sixth, the first test after a cold stack start, hit the 30 s test timeout while polling the development log for the second mail (the page already showed the countdown), so the run added `test.setTimeout(90_000)` to the spec. Defect listed, not held against this use case: the onboarding wizard's own 'Continue' and 'Save and exit' are 40 px high at 390 (Workspaces onboarding). Independent acceptance: every line in §7 held.
- State: DONE — accepted by independent acceptance 2026-11-02. Built: the sign-up page and the onboarding notice wait 60 seconds after a send and say when asking is refused (429); the onboarding notice used to disappear when the new link went out, so the person was told nothing, and now stays and says it went out; the token of a link is spent once however many times the page mounts (the page called confirm twice and showed a dead link over a confirmed address; found by the real spec). Backend: Application `ResendEmailVerificationTests` 4 (known, rate limited, unknown and confirmed answer success and issue nothing), `EmailVerificationTokenIssuerTests` 1 (an earlier link is revoked by the next). Frontend: web component tests for both countdowns. At 390 px the onboarding notice's 'Send the link again' is a 44 px target and the onboarding header and step bar no longer squeeze (only the current step names itself on a phone); the real resend spec measures it. The mock backend answers the resend route (`auth.resendEmailVerification`, on the consumer surface), so the verify page's sent state works on the mock. A failed confirmation call is forgotten, so a retry with the same token asks the server again (component test). Moved Storybook baselines to regenerate: onboarding stories at the mobile width.

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
- Evidence: BE `Auth/Commands/ForgotPassword/ForgotPassword.cs`; API `Auth/Commands/ForgotPasswordEndpoint.cs` (`POST /api/v1/auth/forgot-password`) · FE `packages/features/auth/src/web/components/auth-forgot-surface.tsx`, route `forgot-password.tsx` · Design Final-Auth · Tests Application `ForgotPasswordTests`; mock e2e `auth-flow.mock.e2e.spec.ts` · Real e2e `password-reset.real.e2e.spec.ts` (1 passed: ten wrong passwords lock the account, the page shows the time left; Forgot password asks for the email and says a code went out with a resend countdown; a known and an unknown address get the identical answer; the fourth request in an hour is 429; a second request replaces the first code). Independent acceptance 2026-11-02 by an agent that did not build it, on develop `876ebfb57` plus the tests this run added (branch accept/identity-b4). Backend: Application `ForgotPasswordTests` 5 passed (identical answer for a known and an unknown address, also when the send fails; the limit; the address never logged). Integration `LoginThrottleIntegrationTests`, `MfaFlowTests`, `RefreshTokenCommandHandlerTests` and `CredentialScopeTests` 64 passed in one run (Testcontainers: Postgres and Redis). Frontend: vitest web on `packages/features/auth` 52 passed (+1 this run: the forgot page says the request limit on the ask step and on the sent step; that state had no test). Mock e2e of the run, 64 passed: `design-fidelity-auth` 14, `auth-states-narrow` 16, `auth-reset-states` 6, `sign-in-narrow` 7, `auth-flow` 18, `onboarding` 3. Contact sheets of every sign-in state at 1440, 1024, 390 and 320 in light and dark were opened. Design box diff against the canvas (repo copy equal to the live canvas file): the form column of the drawn states is within 1 px with the lead's decided offsets, which the spec states and asserts; the brand panel is not (logo box 36 x 28 vs 36 x 36, brand name 20 px vs 21 px, tagline y 180 vs 188 and weight 600 vs 650), and no check measures it. Real e2e: `password-reset.real.e2e.spec.ts` passed 5 of 6 across runs of the real stack (the failure is in the State line).
- State: HARDENING. Built: the sent step says code, not link, and offers 'Enter the code'; Backend tests already cover the identical answer when the mail fails and the limit (`ForgotPasswordTests` 5 pass). The sent step's visual baselines (`forgot-sent`, `forgot-sent-ready`) change with the copy and the new button and must be regenerated by the owner's baseline job. The drawn `quen-mat-khau` and `da-gui` boxes are measured by `design-fidelity-auth.mock.e2e.spec.ts` (within 1 px, light and dark); the sent step follows the product rule (code, ten minutes, 'Enter the code', resend countdown) and so holds one button the artboard does not draw: the boxes around it are expected 31 px off, and the copy-dependent boxes are not compared by width. Link rows are 44 px at 390 px (`auth-states-narrow.mock.e2e.spec.ts`, 390 and 320 px, light and dark, axe clean). Baselines `forgot-sent`, `forgot-sent-ready` and the sign-in stories move and are regenerated by the baseline job. Line 5: reviewed 2026-11-02 on develop `876ebfb57` (branch accept/identity-b4), not accepted. Lines 1 and 2 held. Line 3 gap: The brand panel of the drawn screens is not within 1 px of the artboard (measured on `/sign-in` at 1440 x 900, light and dark, against the canvas file whose sha256 equals the repo copy): the logo box is 36 x 28 where the artboard draws 36 x 36, the brand name is 20 px where it draws 21 px, and the tagline sits at y 180 where it draws 188 (8 px higher) and is weight 600 where it draws 650 (`auth-shell.tsx`; the shape, footer and language switch match). `design-fidelity-auth.mock.e2e.spec.ts` measures only the form column (its header says so), so no check shows these offsets. Line 4 gap: `password-reset.real.e2e.spec.ts` (shared with B06, B07 and B09) failed in 1 of 6 runs here (`expect(me).toBe(200)` right after the sign-in that follows the reset, spec line 159). Cause, reproduced: the user watermark that a reset sets rejects an access token issued in the same clock second as the reset, because `AccessTokenRevocationEvaluator.cs:7` compares the token's whole-second `iat` with `<=` against the millisecond watermark; 6 of 16 sign-ins made about 300 ms after a reset got 401 on `/auth/me`. The reset's own rule (sessions and tokens issued before are revoked) over-reaches to a token issued after it. lines 1-4 PASS, awaiting acceptance (2026-11-02, branch idn/acceptance-b4-fixes). The brand panel is now within 1 px of Final-Auth at 1440 x 900, light and dark (before -> after: logo box 36 x 28 -> 36 x 36, brand name 20 -> 21 px, tagline y 180 -> 188 and weight 600 -> 650; two typography roles `brand-name` and `brand-promise`); `design-fidelity-auth.mock.e2e.spec.ts` measures it (16 of 16 passed, with the 'Enter the code' button of `da-gui`). The access token now carries a signed `iat_ms` claim (set in `JwtService`); `AccessTokenRevocationEvaluator.ResolveIssuedAt` compares it with the millisecond watermark when it agrees with `iat`, a token without it keeps `iat <= watermark` rejecting, in the bearer check and in realtime. Tests: `AccessTokenRevocationEvaluatorTests` 11 (before, same second after, same second before, missing and disagreeing claim); Integration `SecurityActivityHttpFlowTests` 11 of 11, including 20 sign-ins 300 ms after a reset with 0 refused on `/auth/me` and the pre-reset token still refused. Real `password-reset.real.e2e.spec.ts` 6 of 6 passed (3 + 3 on two fresh stacks; repeating more in one stack meets the per-IP 429 limit).

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
- Evidence: BE `Auth/Commands/ResetPassword/ResetPassword.cs`; API `Auth/Commands/ResetPasswordEndpoint.cs` (`POST /api/v1/auth/reset-password`) · FE `packages/features/auth/src/web/hooks/use-reset-password.ts` (hook only; no route or surface) · Design none · Tests Application `ResetPasswordTests`; Domain `PasswordResetTokenTests` · Real e2e `password-reset.real.e2e.spec.ts` (shared with B06: a wrong code is refused in words; the right code changes the password and sign-in shows the notice; device B and the old password stop working; the code works once; five wrong codes kill the code and a right one is refused with `identity.auth.too-many-attempts`); mock walk `auth-reset-states.mock.e2e.spec.ts` (6 passed: form, validation, refused and the sign-in notice at 1440, 1024 and 390 px in light and dark: no sideways scroll, 44 px targets at 390 except the shared 36 px show-password eye, axe no serious or critical violation). Independent acceptance 2026-11-02 by an agent that did not build it, on develop `876ebfb57` plus the tests this run added (branch accept/identity-b4). Backend: Application `ResetPasswordTests` 5 and Domain `PasswordResetTokenTests` passed; Infrastructure `OtpServiceTests` 21 (a code works once, a wrong code does not delete it, the attempt limit deletes it). Integration `LoginThrottleIntegrationTests`, `MfaFlowTests`, `RefreshTokenCommandHandlerTests` and `CredentialScopeTests` 64 passed in one run (Testcontainers: Postgres and Redis). Frontend: vitest web on `packages/features/auth` 52 passed. Mock e2e of the run, 64 passed: `design-fidelity-auth` 14, `auth-states-narrow` 16, `auth-reset-states` 6, `sign-in-narrow` 7, `auth-flow` 18, `onboarding` 3. The reset screen has no artboard: form, validation, refused and the sign-in notice were captured at 1440, 1024 and 390 in light and dark and every contact sheet was opened: no overflow or clipping, controls 44 px at 390 (the show-password eye included), axe clean. Real e2e: `password-reset.real.e2e.spec.ts` passed 5 of 6 across runs of the real stack (the failure is in the State line).
- State: HARDENING. Built: the reset screen `/reset-password` (email, code, new password; `ResetFlow`, `AuthResetSurface`), reached from the sent step with the email carried over; sign-in says the password changed and every device signed out; the copy says code and ten minutes. Lead decision applied: no artboard exists, so the screen is built from the Auth components on the pattern of the forgot screen and reviewed. Tests: Application `ResetPasswordTests` 5, Domain `PasswordResetTokenTests`; web component tests for the surface (3) and the flow (7: success, the four refusals, new code). The show-password eye and the 'Back to sign-in' and 'Send a new code' rows are 44 px at 390 px (`auth-reset-states.mock.e2e.spec.ts` no longer exempts the eye); the drawn desktop eye stays 36 px. Line 5: reviewed 2026-11-02 on develop `876ebfb57` (branch accept/identity-b4), not accepted. Lines 1, 2 and 3 held. Line 4 gap: `password-reset.real.e2e.spec.ts` (shared with B06, B07 and B09) failed in 1 of 6 runs here (`expect(me).toBe(200)` right after the sign-in that follows the reset, spec line 159). Cause, reproduced: the user watermark that a reset sets rejects an access token issued in the same clock second as the reset, because `AccessTokenRevocationEvaluator.cs:7` compares the token's whole-second `iat` with `<=` against the millisecond watermark; 6 of 16 sign-ins made about 300 ms after a reset got 401 on `/auth/me`. The reset's own rule (sessions and tokens issued before are revoked) over-reaches to a token issued after it. lines 1-4 PASS, awaiting acceptance (2026-11-02, branch idn/acceptance-b4-fixes). The access token now carries a signed `iat_ms` claim (set in `JwtService`); `AccessTokenRevocationEvaluator.ResolveIssuedAt` compares it with the millisecond watermark when it agrees with `iat`, a token without it keeps `iat <= watermark` rejecting, in the bearer check and in realtime. Tests: `AccessTokenRevocationEvaluatorTests` 11 (before, same second after, same second before, missing and disagreeing claim); Integration `SecurityActivityHttpFlowTests` 11 of 11, including 20 sign-ins 300 ms after a reset with 0 refused on `/auth/me` and the pre-reset token still refused. Real `password-reset.real.e2e.spec.ts` 6 of 6 passed (3 + 3 on two fresh stacks; repeating more in one stack meets the per-IP 429 limit).

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
- Evidence: BE `Auth/Throttling/` (`ILoginThrottle`), `Auth/Commands/Login/Login.cs` · FE `packages/features/auth/src/web/components/auth-sign-in-surface.tsx` · Design Final-Auth · Tests Domain `UserLoginAttemptTests`, Application `SecurityStepUpServiceThrottleTests`, `LoginTests`; mock e2e `auth-flow.mock.e2e.spec.ts` ("five wrong passwords lock") · Real e2e `password-reset.real.e2e.spec.ts` (shared with B06, B07: ten wrong passwords, then the right password is refused with 429 and the page shows 'Try again in N seconds' without signing in; a reset lifts the account lock and the new password signs in). Independent acceptance 2026-11-02 by an agent that did not build it, on develop `876ebfb57` plus the tests this run added (branch accept/identity-b4). Backend: Domain `UserLoginAttemptTests`, Application `SecurityStepUpServiceThrottleTests`, `LoginTests` and `ResetPasswordTests` passed; Integration `LoginThrottleIntegrationTests`, `MfaFlowTests`, `RefreshTokenCommandHandlerTests` and `CredentialScopeTests` 64 passed in one run (Testcontainers: Postgres and Redis). It holds the lock cases (ten wrong passwords lock even the right password, known and unknown accounts get the same failure and lock, a reset lifts the account lock and never the address lock, parallel guesses cannot pass the limit, Redis holds no email or address in clear). Frontend: vitest web on `packages/features/auth` 52 passed. Mock e2e of the run, 64 passed: `design-fidelity-auth` 14, `auth-states-narrow` 16, `auth-reset-states` 6, `sign-in-narrow` 7, `auth-flow` 18, `onboarding` 3. Contact sheets of the locked state at 1440, 1024, 390 and 320 in light and dark were opened. Design box diff against the canvas (repo copy equal to the live canvas file): the form column of the drawn states is within 1 px with the lead's decided offsets, which the spec states and asserts; the brand panel is not (logo box 36 x 28 vs 36 x 36, brand name 20 px vs 21 px, tagline y 180 vs 188 and weight 600 vs 650), and no check measures it. Real e2e: `password-reset.real.e2e.spec.ts` passed 5 of 6 across runs of the real stack (the failure is in the State line).
- State: HARDENING. Built: the mock backend and its mock e2e now lock after ten wrong passwords for 15 minutes, like the product rule (it locked after five for a minute). The address lock (fifty from one address) has no real proof here. The drawn `loi` state is measured within 1 px (`design-fidelity-auth.mock.e2e.spec.ts`): the chosen email is the drawn chip, not a second field, and the locked attempt answers the generic refusal under the field with the lock notice below it (the same refusal as a wrong password, IAREQ014); the field is emptied. The eye and the link rows are 44 px at 390 px. Line 5: reviewed 2026-11-02 on develop `876ebfb57` (branch accept/identity-b4), not accepted. Lines 1 and 2 held. Line 3 gap: the drawn `loi` form column is within 1 px, but the brand panel of the drawn screens is not within 1 px of the artboard (measured on `/sign-in` at 1440 x 900, light and dark, against the canvas file whose sha256 equals the repo copy): the logo box is 36 x 28 where the artboard draws 36 x 36, the brand name is 20 px where it draws 21 px, and the tagline sits at y 180 where it draws 188 (8 px higher) and is weight 600 where it draws 650 (`auth-shell.tsx`; the shape, footer and language switch match). `design-fidelity-auth.mock.e2e.spec.ts` measures only the form column (its header says so), so no check shows these offsets. Line 4 gap: `password-reset.real.e2e.spec.ts` (shared with B06, B07 and B09) failed in 1 of 6 runs here (`expect(me).toBe(200)` right after the sign-in that follows the reset, spec line 159). Cause, reproduced: the user watermark that a reset sets rejects an access token issued in the same clock second as the reset, because `AccessTokenRevocationEvaluator.cs:7` compares the token's whole-second `iat` with `<=` against the millisecond watermark; 6 of 16 sign-ins made about 300 ms after a reset got 401 on `/auth/me`. The reset's own rule (sessions and tokens issued before are revoked) over-reaches to a token issued after it. The acceptance clause 'Given a reset, then the account signs in again' is the step that fails. lines 1-4 PASS, awaiting acceptance (2026-11-02, branch idn/acceptance-b4-fixes). The brand panel is now within 1 px of Final-Auth at 1440 x 900, light and dark (before -> after: logo box 36 x 28 -> 36 x 36, brand name 20 -> 21 px, tagline y 180 -> 188 and weight 600 -> 650; two typography roles `brand-name` and `brand-promise`); `design-fidelity-auth.mock.e2e.spec.ts` measures it (16 of 16 passed, with the 'Enter the code' button of `da-gui`). The access token now carries a signed `iat_ms` claim (set in `JwtService`); `AccessTokenRevocationEvaluator.ResolveIssuedAt` compares it with the millisecond watermark when it agrees with `iat`, a token without it keeps `iat <= watermark` rejecting, in the bearer check and in realtime. Tests: `AccessTokenRevocationEvaluatorTests` 11 (before, same second after, same second before, missing and disagreeing claim); Integration `SecurityActivityHttpFlowTests` 11 of 11, including 20 sign-ins 300 ms after a reset with 0 refused on `/auth/me` and the pre-reset token still refused. Real `password-reset.real.e2e.spec.ts` 6 of 6 passed (3 + 3 on two fresh stacks; repeating more in one stack meets the per-IP 429 limit).

## Capability C: Logout

### UC-IDN-C01 Sign out this device
- Capability: C Logout
- Actors and permission: signed-in person; own session only.
- Preconditions: a session.
- Input: none.
- Output: the session is revoked, cookies cleared, access credentials of the session refused at once.
- Main flow: 1. The person chooses Sign out. 2. The client posts `POST /api/v1/auth/logout`. 3. The app returns to the sign-in page and drops all cached server state.
- No-op / rejection / concurrency / retry: only the caller's own sessions end (the one the access credential names and the one the refresh cookie holds); another person's cookie never ends their session; a missing refresh cookie still signs this device out; a second sign-out with a live credential finds nothing active and changes nothing; a call whose session is already gone answers 401 and the client counts that as signed out; revoked state is never revived (ID-005). Sign-out records no activity line (the recorded facts list has none).
- UI states: pending, signed out, failed (the server session may still be alive: the person is told, stays signed in and may retry).
- Rules: ID-005; sections 16 to 19; IAREQ020.
- Acceptance: Given a session, when the person signs out, then the next call with the old cookies is 401. Given a second logout, then success and no second activity line.
- Evidence: BE `Auth/Commands/Logout/Logout.cs`; API `Auth/Commands/LogoutEndpoint.cs` (`POST /api/v1/auth/logout`) · FE `packages/features/auth/src/web/hooks/use-logout.ts`, `apps/web/src/shell/workspace/workspace-shell.tsx` (failure toast) · Design Final-Account · Tests Application `LogoutTests` (written before the change, needs updating) · Real e2e `sign-out-and-sessions.real.e2e.spec.ts` test 2 (1 passed: the row of this device asks first, then returns to sign-in; the next call is 401, the old refresh credential no longer refreshes, a second sign-out answers 401 and revives nothing). Real stack, run at the head of `idn/scope-0-1` on develop `6ac912d0c`: `password-reset`, `second-step-sign-in`, `email-confirmation`, `email-confirmation-resend`, `sign-out-and-sessions`, `session-bootstrap` and `mfa-enrollment-step-up` real specs, 11 of 11 passed in one run. Accepted 2026-11-02 by an independent acceptance agent that did not build it, on develop `83ab02283` plus the tests this run added (branch accept/identity-b2). Backend: Application `LogoutTests` 7 (four added by this run: another person's refresh cookie never ends their session; this device only, another own session stays active and gets no marker; with no refresh cookie the session the access credential names still ends and is marked; a second sign-out finds nothing active, keeps the revoke time and records no activity), Domain `UserSessionTests`. Frontend vitest web on the auth and account packages and the account and auth-guard pages: 7 files, 122 passed; node 88 passed. Ending this device asks first, keeps it on Cancel, locks while busy and shows a failure alert (`account-pages.component.test.tsx` 38). Keyboard and axe: `account-security-flows.mock.e2e.spec.ts` (confirmation opened with Enter, Cancel focused; axe no serious or critical at 1440 and 390 light and dark). Mock e2e of the acceptance run: auth-reset-states 6, account-security-flows 11, sign-in-narrow 7, design-fidelity-account 4, account-pages 8, auth-flow 18 (54 passed). Design: no artboard draws the confirmation (reviewed, screenshots at 1440, 1024 and 390 px in light and dark were taken and every one opened (contact sheets of the six per state): no overflow, clipping or squeezed column, buttons and fields at least 44 px at 390, axe no serious or critical violation.) The security page itself is within 1 px of Final-Account `bao-mat` at 1440 light and dark (`design-fidelity-account.mock.e2e.spec.ts`, 4 passed). The account page's own tab strip is 34 px high at 390 (shared account frame, listed as a defect, not held against this use case). Real e2e: `sign-out-and-sessions.real.e2e.spec.ts` test 2 passed (3 of 3 in the file): the row of this device asks first, the old refresh credential no longer refreshes, a second sign-out is 401. Real stack, run on develop `83ab02283` plus this branch (compose project notrelix-fe-real-e2e): `sign-out-and-sessions` 3 of 3, `session-bootstrap` 3 of 3, `second-step-sign-in` 1 of 1, `password-reset` 1 of 1, `email-confirmation-resend` 1 of 1 passed; `email-confirmation` 0 of 1 failed (after sign-up the page opens `/workspaces/<id>`, the spec expects `/onboarding`, spec line 25). Lead acceptance: every line in §7 held.
- State: DONE — accepted by independent acceptance 2026-11-02. Built: the page asks before ending this device and the sign-out of an already ended session is a success.

### UC-IDN-C02 List my sessions and devices
- Capability: C Logout
- Actors and permission: signed-in person; own rows only.
- Preconditions: none.
- Input: none.
- Output: per session: id, status, created and last-seen time, client address, user agent, "this device" flag.
- Main flow: 1. The security page reads `GET /api/v1/auth/sessions`. 2. It shows active sessions, newest first, with a description of the device.
- No-op / rejection / concurrency / retry: another person's session never appears and a foreign id is indistinguishable from an unknown id; the list holds every live session and at most 20 ended ones; a session past its end time that nothing closed reads as expired; read, safe to retry.
- UI states: loading, empty (only this device), success, error.
- Rules: section 67 (account security facts); owner decision 48 (a device is a session; address and user agent only); IAREQ023.
- Acceptance: Given two signed-in devices, when the list is read, then both show and one is marked current. Given another person, then none of my rows appear.
- Evidence: BE `Sessions/Queries/GetUserSessions/GetUserSessionsQuery.cs`; API `Auth/Commands/SessionEndpoints.cs` (`GET /api/v1/auth/sessions`) · FE `apps/web/src/routes/workspaces/$workspaceId/account/security.tsx` · Design Final-Account · Tests Domain `UserSessionTests`; Integration `UserScopeIsolationTests`; mock e2e `account-pages.mock.e2e.spec.ts` · Real e2e `sign-out-and-sessions.real.e2e.spec.ts` test 1 (1 passed: three live sessions with one marked this device, the page says '3 sessions are active', another person's sessions never appear). Real stack, run at the head of `idn/scope-0-1` on develop `6ac912d0c`: `password-reset`, `second-step-sign-in`, `email-confirmation`, `email-confirmation-resend`, `sign-out-and-sessions`, `session-bootstrap` and `mfa-enrollment-step-up` real specs, 11 of 11 passed in one run. Accepted 2026-11-02 by an independent acceptance agent that did not build it, on develop `83ab02283` plus the tests this run added (branch accept/identity-b2). Backend: Application `GetUserSessionsQueryTests` 5, added by this run (two live devices both show and only the caller's is current; another person's live and ended sessions never appear; ended sessions are capped at 20 while every live one stays; a session past its end time that nothing closed reads as expired; with no bound session none is current), Domain `UserSessionTests`; the cross-person negative is the handler's own filter (test above) and the real spec (another person's sessions never appear). A foreign session id is indistinguishable from an unknown id on the revoke side (C03, `RevokeSessionTests`). Frontend vitest web on the auth and account packages and the account and auth-guard pages: 7 files, 122 passed; node 88 passed. Keyboard and axe: `account-security-flows.mock.e2e.spec.ts` 11 passed. Design: the sessions block is within 1 px of Final-Account `bao-mat` at 1440 light and dark (rows, sign-out buttons 86 x 28, block widths; `design-fidelity-account.mock.e2e.spec.ts` 4 passed); at 1024 and 390 reviewed from screenshots, screenshots at 1440, 1024 and 390 px in light and dark were taken and every one opened (contact sheets of the six per state): no overflow, clipping or squeezed column, buttons and fields at least 44 px at 390, axe no serious or critical violation. The list cannot be empty (the current session is always in it); the page-level load error with retry is the shared account page state, covered by a component test and not screenshotted here. The account page's own tab strip is 34 px high at 390 (shared account frame, listed as a defect, not held against this use case). Real e2e: `sign-out-and-sessions.real.e2e.spec.ts` test 1 passed: three live sessions with one marked this device, '3 sessions are active', another person's sessions never appear. Real stack, run on develop `83ab02283` plus this branch (compose project notrelix-fe-real-e2e): `sign-out-and-sessions` 3 of 3, `session-bootstrap` 3 of 3, `second-step-sign-in` 1 of 1, `password-reset` 1 of 1, `email-confirmation-resend` 1 of 1 passed; `email-confirmation` 0 of 1 failed (after sign-up the page opens `/workspaces/<id>`, the spec expects `/onboarding`, spec line 25). Lead acceptance: every line in §7 held.
- State: DONE — accepted by independent acceptance 2026-11-02. Built: the list holds every live session and at most 20 ended ones.

### UC-IDN-C03 Sign out one other device
- Capability: C Logout
- Actors and permission: signed-in person; own session; no step-up (lead decision).
- Preconditions: the target session is the caller's and Active.
- Input: session id.
- Output: that session is revoked and its access credentials refused at once; a "session revoked" line.
- Main flow: 1. The person chooses Sign out on a device row. 2. The client posts `POST /api/v1/auth/sessions/{sessionId}/revoke`. 3. The row disappears.
- No-op / rejection / concurrency / retry: revoking an already revoked session is a no-op success; an unknown or foreign id answers the same 404 as a missing one; revoking the current session signs this device out.
- UI states: pending, success, not found (the row is gone: the list refreshes and says the device was already signed out), destructive confirmation for the current device (the safe answer is focused).
- Rules: ID-005; section 19; IAREQ021.
- Acceptance: Given a second device, when its row is revoked, then its next call is 401. Given another person's session id, then 404 and no change.
- Evidence: BE `Sessions/Commands/RevokeSession/RevokeSession.cs`; API `Auth/Commands/SessionEndpoints.cs` (`POST /api/v1/auth/sessions/{sessionId}/revoke`) · FE `.../account/security.tsx`, `packages/features/account/src/web/security/account-security-surface.tsx` · Design Final-Account · Tests Domain `UserSessionTests`; mock e2e `account-pages.mock.e2e.spec.ts` · Run: Integration `AccountSecurityIntegrationTests` (10: another person's or an unknown session id is NotFound and the session stays; an own other session is revoked and the current one kept), Domain `UserSessionTests` (13) · Real e2e `frontend/e2e/real/session-bootstrap.real.e2e.spec.ts` (a session revoked from a second device answers 401 on its next call and the open page returns to sign-in) · Accepted 2026-11-01 by an acceptance agent that did not build it, at `a6898a75e` (branch accept/first-done-batch-2 on develop `f830571c4`). Backend: Application `RevokeSessionTests` 5 (own active session revoked, cut off and recorded once; an already revoked or expired session answers success with no second line and no blacklist write; another person's id and an unknown id answer the same 404 and change nothing; the current session can be ended), Integration `AccountSecurityIntegrationTests` 10 on Postgres (foreign and unknown id NotFound, own other session revoked and the current kept), Domain `UserSessionTests` 13, API `StepUpPurposeEndpointTests` 14 (the revoke route is 401 without a session). No expected version exists on these writes: each is an absolute, idempotent end state or a single-use proof consumed atomically (catalog §1.3, ID-015). Sessions and factors belong to a person, not to a workspace, so the cross-tenant negative is the other person's id. Frontend: `account-security.interaction.component.test.tsx` 16 and `account-security-flows.interaction.component.test.tsx` 9 (vitest web), `apps/web` `account-pages.component.test.tsx` 33 (sign out one device; 404 says the device was already signed out and the list is read again; 500 shows the failure alert; ending this device asks first, keeps it on Cancel, focuses the safe answer and locks while busy). Keyboard: the account-security-flows spec opens the confirmation with Enter, finds Cancel focused and closes it with Enter. Accessibility: axe (all rules, colour contrast included) on the security page, the sign-out-this-device confirmation at 1440 and 390 px in light and dark: no serious or critical violation (`frontend/e2e/mock/account-security-flows.mock.e2e.spec.ts`, 7 passed; `design-fidelity-account.mock.e2e.spec.ts` 4 passed; `account-pages.mock.e2e.spec.ts` 8 passed on the same run, 46 of 46 for the mock specs of this batch: flows 7, sign-in 7, card popup and copy link 20, design fidelity 4, account pages 8). Design, measured: the security page at 1440 in light and dark within 1 px of Final-Account `bao-mat` (`design-fidelity-account.mock.e2e.spec.ts`); at 390 the account-security-flows spec asserts no sideways scroll, every button and field at least 44 px tall and axe clean in each state. Reviewed, no artboard draws them: the confirmation for ending this device and the 'already signed out' notice. Built from the account page components and tokens on the pattern of the security page; acceptance screenshots at 1440, 1024 and 390 in light and dark were taken by the build lane and the acceptance agent opened the confirmation at 390 in dark. No 1024 artboard exists and no spec asserts 1024, so 1024 is reviewed from screenshots only. Real e2e: `session-bootstrap.real.e2e.spec.ts` test 2 now clicks the other device's row, sees 'Signed out.', the ended device's next call is 401 and this device stays signed in; another person's session id and an unknown id answer the same 404 and the other person's session stays active. Real stack, run at `a6898a75e`: `mfa-enrollment-step-up.real.e2e.spec.ts` 1 passed, `session-bootstrap.real.e2e.spec.ts` 3 passed (12 of 12 in the run with `work-item-states.real.e2e.spec.ts`). Lead acceptance: every line in §7 held on this run.
- State: DONE. Accepted 2026-11-01 by an acceptance agent that did not build it, at `a6898a75e` (branch accept/first-done-batch-2 on develop `f830571c4`). Built: an expired session answers success like a revoked one (it used to raise a rule error); the page asks before ending this device, drops the cached state and leaves for sign-in; a 404 refreshes the list with a notice.

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
- Evidence: BE `Sessions/Commands/RevokeOtherSessions/RevokeOtherSessions.cs`, `Security/DTOs/StepUpTypes.cs` (`RevokeOtherSessions`); API `Auth/Commands/SessionEndpoints.cs` (`POST /api/v1/auth/sessions/revoke-others`, body `{ stepUpToken }`, answer `{ revokedCount }`) · FE `.../account/security.tsx`, `packages/features/account/src/core/api/account.service.ts`, mock `account-security.handlers.ts` · Design Final-Account · Tests mock e2e `account-pages.mock.e2e.spec.ts` and web `account-pages.component.test.tsx` (both call revoke-others without a proof and need updating) · Real e2e `sign-out-and-sessions.real.e2e.spec.ts` test 1 (shared with C02: a call with no proof is refused and every device stays; a wrong password changes nothing; the right password signs out both other devices, this one stays, the page says 'Signed out of 2 other devices.'; with none left the page says no other device is signed in). Real stack, run at the head of `idn/scope-0-1` on develop `6ac912d0c`: `password-reset`, `second-step-sign-in`, `email-confirmation`, `email-confirmation-resend`, `sign-out-and-sessions`, `session-bootstrap` and `mfa-enrollment-step-up` real specs, 11 of 11 passed in one run. Accepted 2026-11-02 by an independent acceptance agent that did not build it, on develop `83ab02283` plus the tests this run added (branch accept/identity-b2). Backend: Application `RevokeOtherSessionsTests` 7, added by this run (a proof ends every other session, keeps the current one, marks each cut-off once and records one line; no proof or a blank one is 412 `identity.security.step-up-required` and every device stays; a wrong or spent proof is refused and every device stays; with no other session it is a success no-op that spends no proof and records nothing; an unbound current session is 409 `identity.session.not-bound`; another person's sessions and already ended ones are never touched or counted), Integration `SecurityActivityHttpFlowTests` (no proof is refused, the password proof ends others) and `MfaFlowTests` (proofs are bound to user, session and purpose and spend once, also with many parallel callers). The write carries no expected version: it is an end state plus a single-use proof (catalog §1.3). Cross-tenant: sessions belong to a person, so the negative is another person's sessions. Frontend vitest web on the auth and account packages and the account and auth-guard pages: 7 files, 122 passed; node 88 passed. The provider path spends its proof on this action (`account-pages.component.test.tsx`, body `{ stepUpToken }`); the code path is walked by `account-pages.mock.e2e.spec.ts` ('ending every other device spends its own proof'). Mock e2e of the acceptance run: auth-reset-states 6, account-security-flows 11, sign-in-narrow 7, design-fidelity-account 4, account-pages 8, auth-flow 18 (54 passed). Design: the danger button is within 1 px of Final-Account `bao-mat` at 1440 (204 x 32, `design-fidelity-account.mock.e2e.spec.ts`); the proof prompt, the wrong-code message and the result notice with one session left are undrawn: screenshots at 1440, 1024 and 390 px in light and dark were taken and every one opened (contact sheets of the six per state): no overflow, clipping or squeezed column, buttons and fields at least 44 px at 390, axe no serious or critical violation. The account page's own tab strip is 34 px high at 390 (shared account frame, listed as a defect, not held against this use case). Real e2e: `sign-out-and-sessions.real.e2e.spec.ts` test 1 passed: no proof is refused and every device stays, a wrong password changes nothing, the right password ends both other devices and the page says 'Signed out of 2 other devices.'. Real stack, run on develop `83ab02283` plus this branch (compose project notrelix-fe-real-e2e): `sign-out-and-sessions` 3 of 3, `session-bootstrap` 3 of 3, `second-step-sign-in` 1 of 1, `password-reset` 1 of 1, `email-confirmation-resend` 1 of 1 passed; `email-confirmation` 0 of 1 failed (after sign-up the page opens `/workspaces/<id>`, the spec expects `/onboarding`, spec line 25). Lead acceptance: every line in §7 held.
- State: DONE — accepted by independent acceptance 2026-11-02. Built: the step-up proof is required and single-use.

## Capability D: Session expiration

### UC-IDN-D01 Refresh the session silently
- Capability: D Session expiration
- Actors and permission: holder of a refresh credential; public endpoint, `AuthRefreshByIp`.
- Preconditions: an Active, unexpired session for the refresh credential.
- Input: refresh cookie (web) or body (mobile, UC-IDN-A04).
- Output: a new access credential (1 hour) and a rotated refresh credential; the session keeps its id, so a step-up proof and the "this device" mark survive a refresh.
- Main flow: 1. The client sees a 401 or an expiring access credential. 2. It posts `POST /api/v1/auth/refresh`. 3. It retries the failed call once.
- No-op / rejection / concurrency / retry: an invalid, revoked or expired credential answers 401 `identity.auth.invalid-refresh-token`; an Inactive or suspended user has the session revoked and gets the same 401; the answer never names the reason; two parallel refreshes with one credential rotate once and both receive the same new credential (UC-IDN-D03).
- UI states: invisible on success; session expired on failure (UC-IDN-D02).
- Rules: ID-005; section 18; IAREQ017, IAREQ019.
- Acceptance: Given a valid credential, when the client refreshes, then the old credential fails afterwards. Given a suspended user, then 401 and the session ends.
- Evidence: BE `Auth/Commands/RefreshToken/RefreshToken.cs`, `Auth/Services/IRefreshRotationLedger.cs`, Infrastructure `Identity/Auth/RefreshRotationLedger.cs`, Domain `Identity/Sessions/UserSession.cs` (`Rotate`), `SessionLifetimePolicy.cs`, migration `AddSessionRefreshRotation`, RLS `014_anonymous_scopes.sql` (refresh-token scope matches the current and the just-replaced digest); API `Auth/Commands/RefreshTokenEndpoint.cs` (`POST /api/v1/auth/refresh`) · FE `packages/foundation/contracts/src/client/api-client.ts` · Design n/a · Tests Application `RefreshTokenTests` (4: they build the handler without the ledger, the blacklist and the activity recorder and expect a second session row), Domain `RefreshTokenHashTests`, Domain tests calling `UpdateRefreshToken` (replaced by `Rotate`) · Real e2e `sign-out-and-sessions.real.e2e.spec.ts` test 3 (1 passed: a refresh answers a new credential, the session keeps its id). Independent acceptance 2026-11-02 by an agent that did not build it, on develop `876ebfb57` plus the tests this run added (branch accept/identity-b4). Backend: Application `RefreshTokenTests` 12 passed, Domain `UserSessionTests` and `RefreshTokenHashTests`, Integration `RefreshTokenCommandHandlerTests` and `CredentialScopeTests` (replaced credential, grace window, replay ends the chain, a suspended or inactive user is revoked and gets 401). Frontend: `api-client.unit.test.ts` 9 passed (a network drop, a 429 and a 5xx keep the session and raise no session-expired event; a retry asks again; a 401 still signs out) and `auth-guard.component.test.tsx` (the retry screen, in Vietnamese too). Real e2e: `session-refresh-offline.real.e2e.spec.ts` passed 6 of 6 and `sign-out-and-sessions.real.e2e.spec.ts` test 3 passed 3 of 3 across runs of the real stack. The offline retry screen ('Could not check your session', one 'Try again' button) was captured at 1440, 1024 and 390 in light and dark on the real stack and opened: no overflow, axe clean, 'Try again' is the first Tab stop.
- State: HARDENING. Domain `UserSessionTests` +4 (rotation keeps the session and the idle expiry, the cap at 90 days, refused after the limit and for a revoked session). A network failure during the silent refresh no longer signs the person out: the refresh publishes session-expired only when the server answers 401; a network failure, a limit (429) or a server fault keep the session, the caller sees the offline failure, and the session read retries on reconnect while no person has been read. Tests: `api-client.unit.test.ts` +3 (network drop, 429 and 5xx keep the session, a retry asks again; the 401 test still signs out) and real `session-refresh-offline.real.e2e.spec.ts` (refresh cut off: retry screen, no sign-in, refresh credential kept, then the same session opens). Line 5: reviewed 2026-11-02 on develop `876ebfb57` (branch accept/identity-b4), not accepted. Lines 1, 2 and 4 held. Line 3 gap: the offline state is a screen and its single 'Try again' button is 36 px high at 390 px (`apps/web/src/shell/guards`, default `Button` size; the rule is 44 px); 1440 and 1024 are clean. lines 1-4 PASS, awaiting acceptance (2026-11-02, branch idn/acceptance-b4-fixes). 'Try again' is 44 px at 390 px (`max-md:h-touch`, 36 px at 1440 and 1024) and the card keeps a 16 px margin; the screen was opened at 1440, 1024 and 390 px in light and dark (no overflow, axe clean) and the real `session-refresh-offline.real.e2e.spec.ts` measures it at 390 px (2 of 2 passed, 3 runs).

### UC-IDN-D02 Session expired or revoked
- Capability: D Session expiration
- Actors and permission: signed-in person.
- Preconditions: a 401 that refresh cannot repair.
- Input: none.
- Output: protected actions stop; the person is asked to sign in again and returns to the page they were on.
- Main flow: 1. A call answers 401 and refresh fails. 2. The client clears account-scoped server state. 3. It opens sign-in with `returnTo` (internal paths only, `safeReturnPath`). 4. After sign-in the person lands back.
- No-op / rejection / concurrency / retry: a late response of the old session must not write into the new session's state; an external `returnTo` is ignored.
- UI states: session expired (the sign-in page opens with a calm line saying the session ended), return after sign-in.
- Rules: ID sections 58 and 59; IAREQ019.
- Acceptance: Given a revoked session, when the next call fails, then no protected UI stays interactive. Given `returnTo=https://x`, then the person lands on the home page.
- Evidence: FE `packages/features/auth/src/core/utils/safe-return-path.ts`, `apps/web/src/routes/auth-callback.tsx`, `apps/web/src/main.tsx` (`navigateToSignedOut` adds `reason=session-ended`), `apps/web/src/routes/sign-in.tsx`, `packages/features/auth/src/web/components/auth-sign-in-surface.tsx` (`session-ended` line) · Design Final-Auth (`email`; the session-ended notice is not drawn) · Tests FE `safe-return-path.unit.test.ts`, Application `SafeReturnPathTests`; mock e2e `auth-flow.mock.e2e.spec.ts` · Run: FE `safe-return-path.unit.test.ts` (29), Application `SafeReturnPathTests` (30), FE `account-pages.component.test.tsx` (28) · Real e2e `frontend/e2e/real/session-bootstrap.real.e2e.spec.ts` (a revoked session ends at sign-in with the page remembered; signing in again returns to the page) · Accepted 2026-11-01 by an acceptance agent that did not build it, at `a6898a75e` (branch accept/first-done-batch-2 on develop `f830571c4`). Backend: Application `RefreshTokenTests` 10 (a revoked, expired or unknown session cannot refresh), `SafeReturnPathTests` 30, `LogoutTests` 3; a session revoked from another device answers 401 at once (real spec). The notice is client-only, so no new handler exists. Cross-tenant: not applicable, the use case reads no tenant data. Frontend: `safe-return-path.unit.test.ts` 29 (an external `returnTo` is ignored), `apps/web` `session-lifecycle.integration.test.ts` 6 and `account-isolation.integration.test.ts` 5 (the ended session clears account-scoped server state, closes realtime and signs out once; a late response of the old principal does not reach the new one), `auth-flow.mock.e2e.spec.ts` 18 (mock run). Sign-in is a form and is operable by keyboard; the notice is a `status` region. Accessibility: axe (all rules) on the sign-in page after a session ended (the notice shown) at 1440 and 390 px in light and dark: no serious or critical violation (`frontend/e2e/mock/sign-in-narrow.mock.e2e.spec.ts` 7 passed). Design: Final-Auth draws sign-in `email` at desktop only and no artboard draws the 'Your session ended' notice, so nothing is measured against an artboard here. Measured by assertion in `sign-in-narrow.mock.e2e.spec.ts`: at 390 the footer stays inside the viewport, the language switch reads on one line and each of its buttons is 44 px high, with no sideways scroll; at 1440 the switch is the same box as before the 390 fix (x 1210.34, y 842, 181.66 by 34, within 1 px). Fixed on the way: at 390 px 'Tiếng Việt' wrapped onto two lines (`auth-shell.tsx`: no-wrap labels, a footer that wraps, 44 px buttons on phones). Reviewed: the notice built from the sign-in page's own status text; the acceptance agent opened the 390 light re-shoot of the real sign-in page after a revoke (one-line switch, notice readable); 1024 is reviewed from the build lane's screenshots only. Real e2e: `session-bootstrap.real.e2e.spec.ts` test 1 now asserts the 'Your session ended' notice on the sign-in page after the reload, the page remembered in `redirect`, and the return after signing in again. Real stack, run at `a6898a75e`: `mfa-enrollment-step-up.real.e2e.spec.ts` 1 passed, `session-bootstrap.real.e2e.spec.ts` 3 passed (12 of 12 in the run with `work-item-states.real.e2e.spec.ts`). Lead acceptance: every line in §7 held on this run.
- State: DONE. Accepted 2026-11-01 by an acceptance agent that did not build it, at `a6898a75e` (branch accept/first-done-batch-2 on develop `f830571c4`). Built: the notice on the sign-in page (en and vi); the mock e2e that follows the sign-out redirect expects the address without `reason`.

### UC-IDN-D03 Reject a replayed refresh credential
- Capability: D Session expiration
- Actors and permission: system.
- Preconditions: a refresh credential already rotated.
- Input: that credential presented again.
- Output: 401 and, after a grace window, the whole session family revoked.
- Main flow: 1. The server keeps the digest of the previous credential of each session and the new credential itself for the grace window (Redis, 15 seconds, encrypted with AES-GCM under a key derived by HKDF-SHA256 from the replaced credential). 2. A second use inside 10 seconds returns the same new refresh credential (a retry of a lost response) with a fresh access credential. 3. A use after the window revokes the session, cuts off its access credentials and records "session revoked".
- No-op / rejection / concurrency / retry: the window covers two tabs refreshing together (the first request rotates, the other waits up to two seconds for that rotation to commit, then receives the same credential); outside it the replay is treated as theft. A session is one row, so "the chain" is that session. Only the credential the last rotation replaced is recognised: an older one is just unknown (401, no revocation). When the held credential is gone (cache flush) a retry gets 401 without ending the session.
- UI states: session expired.
- Rules: ID section 18; ID-005; enterprise baseline (Auth0, Okta).
- Acceptance: Given a rotated credential, when it is replayed after the window, then the chain ends and the next refresh fails.
- Evidence: BE `Auth/Commands/RefreshToken/RefreshToken.cs` (`HandleReplacedCredentialAsync`, `ServeGraceAsync`, `ServeRacedRotationAsync`), `Auth/Services/IRefreshRotationLedger.cs`, Infrastructure `Identity/Auth/RefreshRotationLedger.cs`, Domain `UserSession.cs` (`IsWithinRotationGrace`), `SessionRevocationReasons.RefreshTokenReuse` · FE none (the client already ends the session on a 401 from refresh) · Design n/a · Tests none · Real e2e `sign-out-and-sessions.real.e2e.spec.ts` test 3 (shared with D01: inside ten seconds the replaced credential is answered with the new one; after eleven seconds its use is 401 and the session ends, the other device's next call is 401). Real stack, run at the head of `idn/scope-0-1` on develop `6ac912d0c`: `password-reset`, `second-step-sign-in`, `email-confirmation`, `email-confirmation-resend`, `sign-out-and-sessions`, `session-bootstrap` and `mfa-enrollment-step-up` real specs, 11 of 11 passed in one run. Accepted 2026-11-02 by an independent acceptance agent that did not build it, on develop `83ab02283` plus the tests this run added (branch accept/identity-b2). Backend: Application `RefreshTokenTests` 12 (a replaced credential presented inside the grace is answered with its successor; after the grace it ends the session, cuts off its access credentials, records it and answers the generic refusal; a never-issued credential changes nothing; a claim holder that failed before its commit is taken over by the retry), Domain `UserSessionTests` (grace window, 90-day cap), and Integration `RefreshRotationLedgerIntegrationTests` 5, added by this run on a real Redis (25 parallel refreshes with one credential: exactly one claims the rotation; each credential has its own claim; a released claim is taken by the retry and not before; the successor reads back only with the credential it replaced and Redis holds it sealed, not in clear; a never-replaced credential recalls nothing). 103 of 103 passed in the run of the identity integration classes. No screen: lines 2 and 3 do not apply (the client ends the session on a 401 from refresh). Real e2e: `sign-out-and-sessions.real.e2e.spec.ts` test 3 passed: inside ten seconds the replaced credential is answered with the new one, after eleven its use is 401 and the session ends and the other device's next call is 401. Real stack, run on develop `83ab02283` plus this branch (compose project notrelix-fe-real-e2e): `sign-out-and-sessions` 3 of 3, `session-bootstrap` 3 of 3, `second-step-sign-in` 1 of 1, `password-reset` 1 of 1, `email-confirmation-resend` 1 of 1 passed; `email-confirmation` 0 of 1 failed (after sign-up the page opens `/workspaces/<id>`, the spec expects `/onboarding`, spec line 25). Lead acceptance: every line in §7 held.
- State: DONE — accepted by independent acceptance 2026-11-02. Built: the replay window and the theft response.

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
- State: IMPLEMENTING. Not built in this pass: nothing in the product can disable a person yet, so an Identity command would have no caller; it needs the Accounts caller (who may disable whom, SCIM deprovisioning) first. Missing: the command, the session and token revocation, the caller in Accounts, tests. Lead decision (2026-10-30): disabling revokes every session and token at once.

### UC-IDN-D05 Bound the absolute session lifetime
- Capability: D Session expiration
- Actors and permission: system.
- Preconditions: a session chain.
- Input: the age of the chain.
- Output: the chain ends after an absolute limit even when used daily.
- Main flow: 1. A session keeps its creation time through every rotation. 2. Each refresh sets the expiry to 30 days from now, never later than 90 days after the session was created; refresh fails after that. 3. The person signs in again.
- No-op / rejection / concurrency / retry: limits are platform values; an Account policy may shorten them later (Accounts).
- UI states: session expired.
- Rules: ID-005; IAREQ019; enterprise baseline (Auth0, Notion).
- Acceptance: Given a chain of 91 days, when it refreshes, then 401.
- Evidence: BE Domain `Identity/Sessions/SessionLifetimePolicy.cs` (`IdleLifetime`, `AbsoluteLifetime`, `ExpiryAfterRefresh`), `UserSession.Rotate`, `Auth/Sessions/AuthSessionIssuer.cs` · FE none · Design n/a · Tests Domain `UserSessionTests` (the idle expiry, the cap at 90 days, refused after 90 days) · Real e2e none
- State: INTEGRATING. Built. Missing: a real proof (a chain of 91 days cannot be aged on the real stack without a clock fixture), owner check. Lead decision (2026-10-30): absolute 90 days, idle 30 days. The web refresh cookie lives 7 days (`JwtSettings:RefreshTokenExpireDays`), shorter than the idle limit, so a browser left alone for 8 days signs in again; left as is, owner check.

## Capability E: OAuth start

### UC-IDN-E01 Start Google or Microsoft sign-in
- Capability: E OAuth start
- Actors and permission: any visitor; public, `AuthCredentialByIp`.
- Preconditions: the provider is enabled by configuration.
- Input: provider in the path, optional `returnUrl` (internal paths only).
- Output: a redirect to the provider with state, nonce and PKCE held server side; an unknown, disabled or unconfigured provider redirects to the sign-in page with `error=identity.oauth.provider-unavailable` and makes no redirect to a provider.
- Main flow: 1. The person presses the provider button. 2. The browser opens `GET /api/v1/auth/oauth/{provider}/start`. 3. The server stores the state and redirects.
- No-op / rejection / concurrency / retry: a disabled or unknown provider answers a failure and no redirect to it (the browser lands on the sign-in page with the code, never on raw JSON); every start makes a new state held 10 minutes; an external `returnUrl` is dropped; both providers carry an S256 code challenge and the matching verifier is sent at the token endpoint.
- UI states: redirecting (the clicked button says where the browser goes and the buttons wait; a page restored from the back-forward cache starts over), provider unavailable (a message under the email field that points to email and password).
- Rules: ID-008, section 29 (this is Identity OAuth, not an Integrations connection); IAREQ040, IAREQ041.
- Acceptance: Given an enabled provider, when sign-in starts, then the redirect carries state and a code challenge. Given a disabled provider, then no redirect.
- Evidence: BE `OAuth/Commands/StartOAuthLogin/StartOAuthLoginCommand.cs`, `OAuth/OAuthPkce.cs`; Infrastructure `Identity/OAuth/OAuthProviderClient.cs`; API `Auth/Commands/StartOAuthLoginEndpoint.cs` (`GET /api/v1/auth/oauth/{provider}/start`) · FE `packages/features/auth/src/web/components/auth-sign-in-surface.tsx`, `containers/sign-in-flow.tsx`, `google-mark.tsx`, `microsoft-mark.tsx` · Design Final-Auth · Tests Domain `OAuthSecretLeakageTests`; mock e2e `auth-flow.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING. Built: the Microsoft authorization URL now carries `response_type=code` (it had none, so the provider would have refused it) and a PKCE challenge, redeemed with the verifier; the same rule applies to the link and step-up starts; a refused start redirects to the sign-in page with the code instead of answering 400 JSON; the redirecting and provider-unavailable states. Missing: tests and owner check (`PublicAuthEndpointTests` assertions of a 400 for a bad provider need updating), real spec (needs a provider test double), 1 px diff. A rate-limited start still answers 429 JSON in the browser; Google still asks for offline access and forced consent at every sign-in although no provider token is kept (not changed here).

## Capability F: OAuth callback

### UC-IDN-F01 Complete provider sign-in
- Capability: F OAuth callback
- Actors and permission: visitor returning from the provider; public, `AuthCredentialByIp`.
- Preconditions: a valid, unused state.
- Input: provider, code, state.
- Output: a session (or the second-step challenge when MFA is on), then a redirect to `/auth/callback?returnTo=`; a "sign-in succeeded" line.
- Main flow: 1. The provider redirects to `GET /api/v1/auth/oauth/{provider}/callback`. 2. The server validates the state, exchanges the code and reads the provider subject. 3. It finds the User by provider and subject, or creates one when the provider vouches that the email is verified and no account holds it (a provider with no such claim, Microsoft, never creates one). 4. It sets cookies and redirects.
- No-op / rejection / concurrency / retry: the state is single use and is read and removed in one atomic step, so two callbacks racing with it cannot both pass; the provider's ID token is checked (issuer, audience and authorized party, expiry, issue time, and the nonce held with the state) before any claim is believed, and a provider that sends none fails the sign-in; every failure redirects to sign-in with one normalized error code; the provider tokens are never stored in a readable form.
- UI states: pending, provider error (a plain sentence per code), second step.
- Rules: ID-008, ID-014; section 28; IAREQ042, IAREQ043, IAREQ046, IAREQ047.
- Acceptance: Given a new provider subject with a free email, when the callback completes, then one User and one session exist. Given a returning subject, then the same User signs in. Given a Microsoft identity that is not linked, then no account is created and the page says the email is not verified.
- Evidence: BE `OAuth/Commands/CompleteOAuthLogin/CompleteOAuthLoginCommand.cs`, `OAuth/OAuthLoginErrorCodes.cs`; Infrastructure `Identity/OAuth/OidcIdTokenValidator.cs`, `OAuthProviderClient.cs`, `OAuthStateStore.cs`; API `Auth/Commands/CompleteOAuthLoginEndpoint.cs` (`GET /api/v1/auth/oauth/{provider}/callback`) · FE `apps/web/src/routes/auth-callback.tsx` · Design Final-Auth · Tests API `OAuthCallbackMfaTests`, Integration `CompletedOAuthLoginCommandHandlerTests`, Domain `OAuthProfileSnapshotTests`, `OAuthEventPayloadTests` · Real e2e none
- State: INTEGRATING. Built: the ID token check (the Google token was decoded with no issuer, audience, expiry or nonce check, and a payload with URL-safe characters failed to decode); the same check for Microsoft, whose UserInfo answer must name the same subject; an enabled provider must ask for the `openid` scope and have a known issuer; the atomic state; the failure redirect now carries a code. Missing: tests and owner check, real spec (needs a provider test double), 1 px diff. The ID token signature is not verified (the token comes straight from the token endpoint over TLS, OIDC Core 3.1.3.7); verify it against the JWKS if a token ever arrives another way. Decided 2026-10-30 (ID section 27.1) and already how the code behaves: Microsoft sends no verified-email claim, so a Microsoft identity signs in only when already linked and never creates an account (`email-unverified` for anyone else); the rule holds for any provider that sends no such claim.

### UC-IDN-F02 Refuse a stale, replayed or colliding callback
- Capability: F OAuth callback
- Actors and permission: visitor or attacker; public.
- Preconditions: none.
- Input: a callback with an old, reused or foreign state, or a subject that collides.
- Output: no session; a redirect to sign-in with a normalized error.
- Main flow: 1. The callback arrives. 2. State fails, or the provider subject is bound to another User, or the email belongs to a password account. 3. The server refuses and records nothing about the secret values.
- No-op / rejection / concurrency / retry: an email match alone never links or merges (ID-008, ID-018); the existing account's owner must sign in and link from the security page (UC-IDN-G01); a replay of a good callback does not open a second session.
- UI states: provider error with a plain reason (expired or used, stopped at the provider, email unverified, email already has an account, anything else), offer to sign in with the password.
- Rules: ID-008, ID-018; sections 27, 28, 64; IAREQ044, IAREQ047.
- Acceptance: Given a used state, when it is replayed, then no session. Given a matching email on a password account, then no merge.
- Evidence: BE `OAuth/Commands/CompleteOAuthLogin/CompleteOAuthLoginCommand.cs`, `OAuth/OAuthLoginErrorCodes.cs` · FE `packages/features/auth/src/web/containers/sign-in-flow.tsx` (code to sentence), `web/components/auth-sign-in-surface.tsx`, `web/copy/auth.copy.ts` · Design none · Tests Domain `UserOAuthFailureAtomicityTests`, `UserUnlinkOAuthInvariantTests`, `OAuthAccountTests`; Integration `CompletedOAuthLoginCommandHandlerTests` · Real e2e none
- State: INTEGRATING. Built: the codes `identity.oauth.failed`, `state-invalid`, `denied`, `email-unverified`, `email-in-use`, `mfa-required`, `provider-unavailable` and one sentence for each on the sign-in page (vi, en); an inactive account, a failed exchange and an untrusted ID token all read as `failed`; the state is used up before the provider's own error is read. Missing: tests and owner check (API test for state replay), an artboard (none draws the error sentences). The link and step-up callbacks still redirect to the failure URL without a code (UC-IDN-G01, K06).

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
- UI states: pending (the buttons wait, so a second start cannot replace the secret the first one shows), showing the code (focus lands on the code field), error.
- Rules: ID-004, ID-007; sections 22 to 24; IAREQ048, IAREQ049, IAREQ053.
- Acceptance: Given no factor, when enrollment starts, then the method is pending and sign-in still needs no second step.
- Evidence: BE `Mfa/Commands/StartMfaEnrollment/StartMfaEnrollment.cs`, `Security/DTOs/StepUpTypes.cs` (`EnrollMfa`); API `Auth/Commands/StartMfaEnrollmentEndpoint.cs` (`POST /api/v1/auth/mfa/enrollment/start`, body `stepUpToken`) · FE `packages/features/account/src/web/security/account-qr-code.tsx`, `account-security-surface.tsx`, `apps/web/src/routes/workspaces/$workspaceId/account/security.tsx`, mock `packages/dev/mock-backend/src/contexts/account/account-security.handlers.ts` · Design Final-Account · Tests Domain `MfaMethodTests`; Infrastructure `MfaTotpServiceTests`; FE `account-qr-code.unit.test.ts` · Run: Integration `MfaFlowTests` (30), Application `VerifyMfaEnrollmentTests` (3), `SecurityStepUpServiceConsumeTests` (7), Domain `MfaMethodTests` (9), Infrastructure `MfaTotpServiceTests` (15) · Real e2e `frontend/e2e/real/mfa-enrollment-step-up.real.e2e.spec.ts` (turn on asks for the password first, the pending method shows its key) · Accepted 2026-11-01 by an acceptance agent that did not build it, at `a6898a75e` (branch accept/first-done-batch-2 on develop `f830571c4`). Backend: Application `StartMfaEnrollmentTests` 7 (no proof is 412 and creates nothing; a session-less caller is 412; a wrong, spent or other-purpose proof is refused and creates nothing; an active factor answers 409 first and keeps the proof; a proof creates one pending method and shows the secret only in the answer; starting again replaces the pending method; another person's pending method is untouched), `LoginTests` 12 (a pending method adds no second step at sign-in, an active one does: the acceptance line), Integration `MfaFlowTests` 30, Domain `MfaMethodTests` 9, Infrastructure `MfaTotpServiceTests` 15, API `StepUpPurposeEndpointTests` 14 (401 without a session). No expected version exists on these writes: each is an absolute, idempotent end state or a single-use proof consumed atomically (catalog §1.3, ID-015). Sessions and factors belong to a person, not to a workspace, so the cross-tenant negative is the other person's id. Frontend: `account-security.interaction.component.test.tsx` 16 and `account-security-flows.interaction.component.test.tsx` 9 (vitest web), `apps/web` `account-pages.component.test.tsx` 33 (QR code of the producer's address with the key as fallback; empty answer not sent; busy holds Confirm; wrong password, throttled and expired in words). Keyboard: the account-security-flows spec walks turn-on with Enter and typing only; focus lands on the password field and then on the code field. Accessibility: axe (all rules, colour contrast included) on the password prompt, the wrong-password message, the QR setup panel at 1440 and 390 px in light and dark: no serious or critical violation (`frontend/e2e/mock/account-security-flows.mock.e2e.spec.ts`, 7 passed; `design-fidelity-account.mock.e2e.spec.ts` 4 passed; `account-pages.mock.e2e.spec.ts` 8 passed on the same run, 46 of 46 for the mock specs of this batch: flows 7, sign-in 7, card popup and copy link 20, design fidelity 4, account pages 8). Design, measured: the security page at 1440 in light and dark within 1 px of Final-Account `bao-mat` (`design-fidelity-account.mock.e2e.spec.ts`); at 390 the account-security-flows spec asserts no sideways scroll, every button and field at least 44 px tall and axe clean in each state. Reviewed, no artboard draws them: the password prompt and the setup panel (QR code, manual key, code field). Built from the account page components and tokens on the pattern of the security page; acceptance screenshots at 1440, 1024 and 390 in light and dark were taken by the build lane and the acceptance agent opened the QR setup panel at 390 in light and at 1440 in light after a wrong code. No 1024 artboard exists and no spec asserts 1024, so 1024 is reviewed from screenshots only. Real e2e: `mfa-enrollment-step-up.real.e2e.spec.ts` 1 passed: turn on asks for the password first, a wrong password is refused in words and starts nothing, then the pending method shows its key. Real stack, run at `a6898a75e`: `mfa-enrollment-step-up.real.e2e.spec.ts` 1 passed, `session-bootstrap.real.e2e.spec.ts` 3 passed (12 of 12 in the run with `work-item-states.real.e2e.spec.ts`). Lead acceptance: every line in §7 held on this run.
- State: DONE. Accepted 2026-11-01 by an acceptance agent that did not build it, at `a6898a75e` (branch accept/first-done-batch-2 on develop `f830571c4`). Built: the endpoint carries the `AuthSensitiveByUser` limit and answers `Cache-Control: no-store` (the answer holds the secret); the page waits during a start. Decided 2026-10-30 and built: starting an enrollment needs a single-use step-up proof of the new purpose `EnrollMfa` (no proof: 412 `identity.security.step-up-required`; a wrong, spent or other-purpose proof: 400 `identity.security.step-up-invalid`); an account that already has a factor answers 409 first and keeps the proof; the page asks for the password, then starts.

### UC-IDN-H02 Verify and activate the second factor
- Capability: H MFA enrollment
- Actors and permission: signed-in person; `AuthSensitiveByUser`.
- Preconditions: a pending method.
- Input: the 6-digit code.
- Output: the method is Active; eight recovery codes (`MfaPolicy.RecoveryCodeCount`) are shown once; a "second factor enabled" line.
- Main flow: 1. The person types the code. 2. The client posts `POST /api/v1/auth/mfa/enrollment/verify`. 3. The page shows the recovery codes with a "I saved them" gate.
- No-op / rejection / concurrency / retry: a wrong code leaves the method pending and answers 400 `identity.mfa.invalid-verification-code`; only the per-user request limit applies (the person is shown the secret they are verifying, so a guess gains nothing; the lockout of ID section 67 names step-up and password change, not enrollment); another person's method id and an unknown id answer the same 400 `identity.mfa.enrollment-not-found`; verifying a method that is already active answers that same 400 (the codes are never readable again, so a retry cannot replay them); the answer says no-store; two parallel verifies with two different proofs are serialised: one activates, the other fails at commit with 412 (stale version) or 409 (a second active factor) and changes nothing (see State).
- UI states: pending, wrong code, success with codes.
- Rules: ID-004, ID-006; section 24; IAREQ049, IAREQ051, IAREQ054.
- Acceptance: Given a pending method, when the right code is sent, then MFA is active and the summary shows eight codes left.
- Evidence: BE `Mfa/Commands/VerifyMfaEnrollment/VerifyMfaEnrollment.cs`, `Mfa/DTOs/MfaEnrollmentVerifyResult.cs` (`MfaEnrollmentActivationResult`), `Domain/Identity/Sessions/SessionRevocationReasons.cs` (`mfa-enrolled`); API `Auth/Commands/VerifyMfaEnrollmentEndpoint.cs` (`POST /api/v1/auth/mfa/enrollment/verify`, body `stepUpToken`) · FE `packages/features/account/src/web/security/account-security-surface.tsx`, `apps/web/src/routes/workspaces/$workspaceId/account/security.tsx` · Design Final-Account · Tests Domain `MfaRecoveryBatchTests`, `UserSecuritySettingsTests` · Run: Integration `MfaFlowTests` (35), Application `VerifyMfaEnrollmentTests` (3), `SecurityStepUpServiceConsumeTests` (7), Domain `MfaMethodTests` (9), Infrastructure `MfaTotpServiceTests` (15) · Real e2e `frontend/e2e/real/mfa-enrollment-step-up.real.e2e.spec.ts` (the right code activates, the backup codes show with the count of other devices signed out, the summary says on) · Acceptance run 2026-11-01 at `a6898a75e` (acceptance agent): Application `VerifyMfaEnrollmentTests` 8 (a wrong code answers invalid-code, leaves the method pending and keeps the proof; another person's method id and an unknown id answer the same refusal; an already active method answers it too, so the codes cannot be replayed; a wrong or spent proof after a right code activates nothing and issues no codes; activation issues eight codes, ends the other sessions only and records `MfaEnabled` and `OtherSessionsRevoked`), Integration `MfaFlowTests` 30, Domain `MfaMethodTests` 9 and `MfaRecoveryBatchTests` 10; frontend and real e2e as for H01 (`mfa-enrollment-step-up.real.e2e.spec.ts` 1 passed, wrong first code refused on the real stack); axe on the setup, wrong-code and backup-codes states at 1440 and 390 in light and dark clean. · Fix run 2026-11-01 at `4f75dfe77` (fix lane): line 1 Backend: Postgres `MfaFlowTests` 35 (the 30 of before and 5 new, three of them for this fix) with `AccountSecurityIntegrationTests` 10: `ParallelActivations_OfOnePendingMethod_OneWins_AndOnlyOneRecoveryBatchExists` (two sessions, two valid proofs, the same code, both read the method before either commits: exactly one succeeds, the other fails with a database conflict, one active method, one recovery batch), `ParallelActivations_OfTwoPendingMethods_OneWins_AndTheDatabaseKeepsOneActiveFactor` (a settings row already exists, so it is not what serialises) and `TheDatabase_RefusesASecondActiveFactorOfTheSameKind_ForOnePerson` (the index named in the error; another person and another kind are free). With the index removed from the migration the two parallel tests still pass, because the settings row version also stops the second commit; the index test is what proves the invariant itself. Losing path in Application: `VerifyMfaEnrollmentTests` 8 (another method already active answers `identity.mfa.already-enabled` and keeps the proof; an already active method answers the not-found refusal). Domain `MfaMethodTests` 9 unchanged. Architecture 666 passed. Lines 2 and 4 are unchanged by this fix: the frontend of H02 was not touched and the real spec `mfa-enrollment-step-up.real.e2e.spec.ts` passed on the real stack at the merged head (see K03 for the run). Line 3 as in the earlier acceptance: the setup and backup-codes states are reviewed, not drawn. Line 5: this is the fix lane's run; the lead reviews it. · Acceptance 2026-11-02 at `6ac912d0c` (independent acceptance agent, branch accept/batch-1): line 1 Postgres Integration `MfaFlowTests` 35 and `AccountSecurityIntegrationTests` 10 passed (the parallel-activation and unique-index tests among them), Domain 200, Application 150 and API 97 (filtered to the classes this use case names, 0 failed) and Postgres Integration 229 (0 failed). Line 2 vitest web `account-security-surface` and `account-pages` 68 passed (pending, wrong code, codes shown). Line 3 mock `design-fidelity-account` 4 passed (security page within 1 px at 1440, light and dark); the wrong-code and backup-codes states (no artboard) were screenshotted at 1440, 1024 and 390 in light and dark and opened: no overflow, clipping or squeezed column, axe no serious or critical in all 6 views of each, the page's own buttons and fields at least 44 px at 390 (the four shell top-bar icon buttons are 32 and 36 px: a shell defect, not this use case's). Line 4 `mfa-enrollment-step-up.real.e2e.spec.ts` 1 passed on the real stack (wrong password refused, wrong first code refused and the factor stays off, the right code shows the codes, turning off). Verdict: all five lines hold.
- State: DONE — accepted by independent acceptance 2026-11-02 at `6ac912d0c` (branch accept/batch-1); earlier accepted 2026-11-01 by the fix lane that closed the one rejected line, at `4f75dfe77` on branch idn/h02-k03 (PR into develop); the lead reviews this Evidence line at merge. Built: a unique partial index `ux_user_mfa_methods_active_per_type` on `identity.user_mfa_methods (user_id, type) WHERE status = 'Active'` (migration `AddUserMfaActiveFactorGuard`; it first disables, never deletes, duplicate active rows and keeps the earliest verified) next to the version token every aggregate root already carries (`ApplicationDbContext` marks `AggregateRoot.Version` as a concurrency token; the earlier statement that `UserMfaMethod` has none was wrong). The loser of two parallel activations fails at commit and its whole transaction rolls back: one active method, one recovery batch. Known limits: the loser has spent its own proof (the page asks again); the data session answers the lost race 412 and a unique violation 409 (`PreconditionFailedProblemTests`, `ExceptionMappingBehaviorTests`), but no test drives that one endpoint over HTTP; wrong-code message and backup-codes panel are not drawn (reviewed, built from account page components and tokens).

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
- Evidence: BE `Mfa/Queries`, `Mfa/Services/MfaStatusProjection.cs`; API `Auth/Queries/GetMfaStatusEndpoint.cs` (`GET /api/v1/auth/mfa/status`) · FE `packages/features/account/src/core/api/account.service.ts` · Design Final-Account · Tests API `AuthenticatedAuthEndpointTests` · Real e2e `second-step-sign-in.real.e2e.spec.ts` (shared: after turning the factor on the summary says it is on and the page shows 'N backup codes left'). Real stack, run at the head of `idn/scope-0-1` on develop `6ac912d0c`: `password-reset`, `second-step-sign-in`, `email-confirmation`, `email-confirmation-resend`, `sign-out-and-sessions`, `session-bootstrap` and `mfa-enrollment-step-up` real specs, 11 of 11 passed in one run. Accepted 2026-11-02 by an independent acceptance agent that did not build it, on develop `83ab02283` plus the tests this run added (branch accept/identity-b2). Backend: Application `GetMfaStatusQueryTests` 4, added by this run (an active factor reports enabled, its type and the recovery codes left after one is used; a factor still pending verification is not enabled; an invalidated batch counts no codes; another person's factor and codes never show; the body type has four fields and no secret, hash or code), API `IdentityOpenApiShapeTests` (the answer shape; 54 of 54 passed with `LoginErrorContractTests` and `StepUpPurposeEndpointTests`). The route needs a session by the authenticated-request marker. Frontend vitest web on the auth and account packages and the account and auth-guard pages: 7 files, 122 passed; node 88 passed. Design: the two-factor block (status badge, 'N backup codes left' line, buttons) is within 1 px of Final-Account `bao-mat` at 1440 light and dark (block 916 x 168.2, `design-fidelity-account.mock.e2e.spec.ts` 4 passed); the off status is walked with axe at 1440 and 390 in `account-security-flows.mock.e2e.spec.ts`; the security page at 1024 and 390 was reviewed from screenshots, screenshots at 1440, 1024 and 390 px in light and dark were taken and every one opened (contact sheets of the six per state): no overflow, clipping or squeezed column, buttons and fields at least 44 px at 390, axe no serious or critical violation. The account page's own tab strip is 34 px high at 390 (shared account frame, listed as a defect, not held against this use case). Real e2e: `second-step-sign-in.real.e2e.spec.ts` passed (after turning the factor on the summary says it is on and the page shows 'N backup codes left'). Real stack, run on develop `83ab02283` plus this branch (compose project notrelix-fe-real-e2e): `sign-out-and-sessions` 3 of 3, `session-bootstrap` 3 of 3, `second-step-sign-in` 1 of 1, `password-reset` 1 of 1, `email-confirmation-resend` 1 of 1 passed; `email-confirmation` 0 of 1 failed (after sign-up the page opens `/workspaces/<id>`, the spec expects `/onboarding`, spec line 25). Lead acceptance: every line in §7 held.
- State: DONE — accepted by independent acceptance 2026-11-02. Built: the status read and its card.

## Capability I: MFA challenge

### UC-IDN-I01 Answer the second step at sign-in
- Capability: I MFA challenge
- Actors and permission: visitor who passed the first step; public, `AuthCredentialByIp`.
- Preconditions: a live challenge held in an httpOnly cookie (also after a provider sign-in).
- Input: the 6-digit code (the challenge is in the cookie).
- Output: a session; the challenge cookie cleared; a "sign-in succeeded" line.
- Main flow: 1. Password or provider step answers "second step needed". 2. The person types the code. 3. The client posts `POST /api/v1/auth/mfa/verify`. 4. The app routes on, keeping `returnTo`.
- No-op / rejection / concurrency / retry: a wrong code counts as a failed sign-in; a missing or expired challenge answers "verification expired" and asks to start again; the challenge never appears in a URL; a code (or recovery code) that already opened something is refused, so a second request with the same code never opens a second session (RFC 6238 section 5.2): the first claim of a person's time step, or of a recovery code, wins; two parallel requests with one valid code open one session.
- UI states: code entry, wrong code (the boxes empty and focus returns to the first), expired (the verify button is off; "Back to sign-in" is the way on), pending.
- Rules: ID-007; section 22; IAREQ050.
- Acceptance: Given a challenge, when the right code is sent, then a session opens. Given no challenge cookie, then the expired state.
- Evidence: BE `Mfa/Commands/CompleteMfaChallenge/CompleteMfaChallenge.cs`, `Mfa/Services/MfaCodeVerifier.cs`, `Mfa/Abstractions/IMfaCodeUseGuard.cs`; Infrastructure `Identity/Mfa/MfaCodeUseGuard.cs`, `MfaTotpService.cs`; API `Auth/Commands/CompleteMfaChallengeEndpoint.cs` (`POST /api/v1/auth/mfa/verify`) · FE `packages/features/auth/src/web/components/auth-sign-in-surface.tsx` · Design Final-Auth · Tests API `MfaChallengeCookieTests`; Infrastructure `CookieServiceMfaChallengeTests`; mock e2e `auth-flow.mock.e2e.spec.ts` · Real e2e `second-step-sign-in.real.e2e.spec.ts` (1 passed: a wrong code is refused in words, a right code opens the session; a code that already opened a session is not accepted by a second request). Independent acceptance 2026-11-02 by an agent that did not build it, on develop `876ebfb57` plus the tests this run added (branch accept/identity-b4). Backend: Application `CompleteMfaChallengeTests` 7 passed, API `MfaChallengeCookieTests` 7, Infrastructure `CookieServiceMfaChallengeTests` and `MfaCodeUseGuardTests` passed; Integration `LoginThrottleIntegrationTests`, `MfaFlowTests`, `RefreshTokenCommandHandlerTests` and `CredentialScopeTests` 64 passed in one run (Testcontainers: Postgres and Redis). Frontend: vitest web on `packages/features/auth` 52 passed. Mock e2e of the run, 64 passed: `design-fidelity-auth` 14, `auth-states-narrow` 16, `auth-reset-states` 6, `sign-in-narrow` 7, `auth-flow` 18, `onboarding` 3. Contact sheets of the code entry, wrong code, backup and ended states at 1440, 1024, 390 and 320 in light and dark were opened: no overflow, the six boxes shrink inside the column. Design box diff against the canvas (repo copy equal to the live canvas file): the form column of the drawn states is within 1 px with the lead's decided offsets, which the spec states and asserts; the brand panel is not (logo box 36 x 28 vs 36 x 36, brand name 20 px vs 21 px, tagline y 180 vs 188 and weight 600 vs 650), and no check measures it. The drawn `xac-minh` form column is within 1 px apart from the lead's decided missing 'remember this device' row (boxes above the row 19.5 px lower, below it 19.5 px higher, as the spec asserts). Real e2e: `second-step-sign-in.real.e2e.spec.ts` passed 3 of 3 across runs of the real stack (a wrong code is refused in words, a right code opens the session, a code that already opened a session is refused).
- State: HARDENING. Backend: Application `CompleteMfaChallengeTests` 7 (right code, wrong code counted as a failed sign-in, a claimed code opens one session, a step-up challenge is refused like an expired one, a locked account checks no code). The six code boxes shrink instead of overflowing (no sideways scroll at 390 and 320 px), 'Use a backup code' and 'Back to sign-in' are 44 px, and the drawn `xac-minh` state is within 1 px apart from the 'remember this device for 30 days' row, which the lead decided is not built in 0.1.0 (the boxes above it are measured 19.5 px lower, those below 19.5 px higher). Line 5: reviewed 2026-11-02 on develop `876ebfb57` (branch accept/identity-b4), not accepted. Lines 1, 2 and 4 held. Line 3 gap: the drawn `xac-minh` form column is within 1 px, but the brand panel of the drawn screens is not within 1 px of the artboard (measured on `/sign-in` at 1440 x 900, light and dark, against the canvas file whose sha256 equals the repo copy): the logo box is 36 x 28 where the artboard draws 36 x 36, the brand name is 20 px where it draws 21 px, and the tagline sits at y 180 where it draws 188 (8 px higher) and is weight 600 where it draws 650 (`auth-shell.tsx`; the shape, footer and language switch match). `design-fidelity-auth.mock.e2e.spec.ts` measures only the form column (its header says so), so no check shows these offsets. lines 1-4 PASS, awaiting acceptance (2026-11-02, branch idn/acceptance-b4-fixes). The brand panel is now within 1 px of Final-Auth at 1440 x 900, light and dark (before -> after: logo box 36 x 28 -> 36 x 36, brand name 20 -> 21 px, tagline y 180 -> 188 and weight 600 -> 650; two typography roles `brand-name` and `brand-promise`); `design-fidelity-auth.mock.e2e.spec.ts` measures it (16 of 16 passed, with the 'Enter the code' button of `da-gui`).

### UC-IDN-I02 Expire a challenge after wrong codes
- Capability: I MFA challenge
- Actors and permission: visitor.
- Preconditions: a live challenge.
- Input: wrong codes.
- Output: after 5 wrong codes the challenge is dead and the cookie is deleted; the person signs in again from the password step.
- Main flow: 1. Each wrong code counts. 2. The fifth kills the challenge. 3. The account and address counters keep counting (UC-IDN-B09).
- No-op / rejection / concurrency / retry: no answer tells how many attempts remain; a dead challenge cannot be revived by a correct code; the fifth wrong code removes the challenge at once (it used to be removed by the attempt after it, so the fifth wrong answer still looked retryable) and answers expired.
- UI states: wrong code, expired.
- Rules: ID section 67; IAREQ050.
- Acceptance: Given five wrong codes, when the right one follows, then it is refused.
- Evidence: BE `Mfa/Commands/CompleteMfaChallenge/CompleteMfaChallenge.cs`; API `Auth/Commands/CompleteMfaChallengeEndpoint.cs` · FE `auth-sign-in-surface.tsx` · Design Final-Auth · Tests API `MfaChallengeCookieTests`; mock e2e `auth-flow.mock.e2e.spec.ts` ("a wrong second-step code is refused") · Real e2e `second-step-sign-in.real.e2e.spec.ts` (shared: wrong codes until `identity.mfa.challenge-invalid`, the page says the verification expired and the button is off, a right code afterwards is refused with 400 and no session opens). Independent acceptance 2026-11-02 by an agent that did not build it, on develop `876ebfb57` plus the tests this run added (branch accept/identity-b4). Backend: Application `CompleteMfaChallengeTests` 7 passed (the fifth wrong code kills the challenge at once and answers expired; a killed challenge refuses a right code and verifies nothing); API `MfaChallengeCookieTests` 7; Integration `MfaFlowTests` (`AttemptLimitExhausted_InvalidatesChallenge`, the budget is cumulative per challenge and independent between challenges). Integration `LoginThrottleIntegrationTests`, `MfaFlowTests`, `RefreshTokenCommandHandlerTests` and `CredentialScopeTests` 64 passed in one run (Testcontainers: Postgres and Redis). Frontend: vitest web on `packages/features/auth` 52 passed. Design: the wrong-code and ended states are not drawn; they were captured with the code entry and the backup field at 1440, 1024, 390 and 320 in light and dark (`auth-states-narrow.mock.e2e.spec.ts` 16 passed: no sideways scroll, every control 44 px at 390 and 320, axe clean) and every contact sheet was opened: the six code boxes shrink inside the column, the ended state keeps the field, turns the button off and leaves 'Back to sign-in'. Mock e2e of the run, 64 passed: `design-fidelity-auth` 14, `auth-states-narrow` 16, `auth-reset-states` 6, `sign-in-narrow` 7, `auth-flow` 18, `onboarding` 3. Real e2e: `second-step-sign-in.real.e2e.spec.ts` passed 3 of 3 across runs of the real stack (wrong codes until `identity.mfa.challenge-invalid`, the page says the verification expired and the button is off, a right code afterwards is refused with 400 and no session opens). Carried: the step-up challenge (K04) still removes its challenge on the attempt after the fifth. The brand panel of every Final-Auth page is off the artboard (see I01's State); no artboard draws this use case's own states. Independent acceptance: every line in §7 held.
- State: DONE — accepted by independent acceptance 2026-11-02. Backend: Application `CompleteMfaChallengeTests` (fifth wrong code kills the challenge; a killed challenge refuses a right code). The step-up challenge (K04) still removes its challenge on the attempt after the fifth. The digit-code row no longer overflows at 390 or 320 px, and the links of the ended state are 44 px (`auth-states-narrow.mock.e2e.spec.ts`, second-step states).

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
- Evidence: BE `Mfa/Commands/CompleteMfaChallenge/CompleteMfaChallenge.cs`, `Mfa/Services/MfaCodeVerifier.cs` (recovery code claim, then `MfaRecoveryBatch.TryConsume`) · FE `auth-sign-in-surface.tsx` (`OtpStep`, backup mode) · Design Final-Auth · Tests Domain `MfaRecoveryBatchTests`; Infrastructure `MfaRecoveryCodeGeneratorTests`; mock e2e `auth-flow.mock.e2e.spec.ts` · Real e2e `second-step-sign-in.real.e2e.spec.ts` (shared: a recovery code signs in, the same code is refused on the next device). Independent acceptance 2026-11-02 by an agent that did not build it, on develop `876ebfb57` plus the tests this run added (branch accept/identity-b4). Backend: Domain `MfaRecoveryBatchTests` 10 and Infrastructure `MfaRecoveryCodeGeneratorTests` 6 passed; Application `CompleteMfaChallengeTests` 7; Integration `MfaFlowTests.RecoveryCode_CompletesChallenge_AndIsSingleUse`. Integration `LoginThrottleIntegrationTests`, `MfaFlowTests`, `RefreshTokenCommandHandlerTests` and `CredentialScopeTests` 64 passed in one run (Testcontainers: Postgres and Redis). Frontend: vitest web on `packages/features/auth` 52 passed. Design: the backup-code mode, its wrong-code message and the ended state are not drawn; they were captured at 1440, 1024, 390 and 320 in light and dark (`auth-states-narrow.mock.e2e.spec.ts` 16 passed: no sideways scroll, 44 px controls at 390 and 320, 'Use an authenticator app' and 'Back to sign-in' included, axe clean) and every contact sheet was opened. Real e2e: `second-step-sign-in.real.e2e.spec.ts` passed 3 of 3 across runs of the real stack (a recovery code signs in once and is refused on the next device). The brand panel of every Final-Auth page is off the artboard (see I01's State); no artboard draws this use case's own states. Independent acceptance: every line in §7 held.
- State: DONE — accepted by independent acceptance 2026-11-02. Built: the backup-code field of the second step, its wrong-code message and success; 'Use an authenticator app' and 'Back to sign-in' are 44 px at 390 px. The screen sits in the `xac-minh` frame (the brand panel of that frame is a gap of UC-IDN-I01, not of this use case's own undrawn states).

### UC-IDN-J02 Regenerate recovery codes
- Capability: J MFA recovery
- Actors and permission: signed-in person; step-up of purpose `RegenerateRecoveryCodes`.
- Preconditions: an active second factor.
- Input: step-up proof.
- Output: eight new codes (`MfaPolicy.RecoveryCodeCount`) shown once; the old batch is void.
- Main flow: 1. The person chooses "New backup codes". 2. They pass step-up. 3. The client posts `POST /api/v1/auth/mfa/recovery-codes/regenerate`.
- No-op / rejection / concurrency / retry: a used proof fails; a retry after success makes another batch and voids the one just shown, so the page must not retry by itself; no active factor is refused.
- UI states: step-up prompt, success with codes, error.
- Rules: ID-004, ID-006; IAREQ051, IAREQ056.
- Acceptance: Given step-up, when codes regenerate, then old codes fail and new codes work.
- Evidence: BE `Mfa/Commands/RegenerateRecoveryCodes/RegenerateRecoveryCodes.cs`; API `Auth/Commands/RegenerateRecoveryCodesEndpoint.cs` (`POST /api/v1/auth/mfa/recovery-codes/regenerate`) · FE `packages/features/account/src/web/security/account-security-surface.tsx`, `apps/web/src/routes/workspaces/$workspaceId/account/security.tsx` (`finishStepUp`) · Design Final-Account · Tests Domain `MfaRecoveryBatchTests` · Real e2e `second-step-sign-in.real.e2e.spec.ts` (shared: new codes ask for a proof (a recovery code here), the page shows the new codes once, an old code is refused at sign-in). Real stack, run at the head of `idn/scope-0-1` on develop `6ac912d0c`: `password-reset`, `second-step-sign-in`, `email-confirmation`, `email-confirmation-resend`, `sign-out-and-sessions`, `session-bootstrap` and `mfa-enrollment-step-up` real specs, 11 of 11 passed in one run. Accepted 2026-11-02 by an independent acceptance agent that did not build it, on develop `83ab02283` plus the tests this run added (branch accept/identity-b2). Backend: Application `RegenerateRecoveryCodesTests` 7, added by this run (a proof voids the old batch and issues one new batch whose codes are shown once; no proof or a blank one is 412 and the old codes stay valid; a wrong or spent proof is refused and the old codes stay valid; no active factor is refused before any proof is spent; an unbound session is refused as step-up required; another person's factor and batch are never used or voided), Domain `MfaRecoveryBatchTests`, Integration `MfaFlowTests` (`RegenerateRecoveryCodes_InvalidatesOldBatch_AndNewCodesWork`, `RawMfaChallenge_CannotAuthorize_RegenerateRecoveryCodes`). Retrying after success makes another batch and voids the one shown: the page asks once per proof. Frontend vitest web on the auth and account packages and the account and auth-guard pages: 7 files, 122 passed; node 88 passed. ('asks for a proof before creating new backup codes and shows them once', `account-pages.component.test.tsx`). Keyboard and axe: `account-security-flows.mock.e2e.spec.ts` (code prompt, wrong code, codes panel with focus on 'I saved them'). Mock e2e of the acceptance run: auth-reset-states 6, account-security-flows 11, sign-in-narrow 7, design-fidelity-account 4, account-pages 8, auth-flow 18 (54 passed). Design: 'Tạo mã mới' is within 1 px of Final-Account `bao-mat` at 1440; the proof prompt and the codes panel are undrawn: screenshots at 1440, 1024 and 390 px in light and dark were taken and every one opened (contact sheets of the six per state): no overflow, clipping or squeezed column, buttons and fields at least 44 px at 390, axe no serious or critical violation. The account page's own tab strip is 34 px high at 390 (shared account frame, listed as a defect, not held against this use case). Real e2e: `second-step-sign-in.real.e2e.spec.ts` passed (new codes ask for a proof, the page shows the codes once, an old code is refused at sign-in). Real stack, run on develop `83ab02283` plus this branch (compose project notrelix-fe-real-e2e): `sign-out-and-sessions` 3 of 3, `session-bootstrap` 3 of 3, `second-step-sign-in` 1 of 1, `password-reset` 1 of 1, `email-confirmation-resend` 1 of 1 passed; `email-confirmation` 0 of 1 failed (after sign-up the page opens `/workspaces/<id>`, the spec expects `/onboarding`, spec line 25). Lead acceptance: every line in §7 held.
- State: DONE — accepted by independent acceptance 2026-11-02. Built: the proof-gated renewal.

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
- Evidence: BE `Mfa/Commands/DisableMfa/DisableMfa.cs`; API `Auth/Commands/DisableMfaEndpoint.cs` (`POST /api/v1/auth/mfa/disable`) · FE `account-security-surface.tsx` (`FlowPanel` disable step, `TwoFactorOffPanel`), `apps/web/src/routes/workspaces/$workspaceId/account/security.tsx` · Design Final-Account · Tests Domain `UserSecuritySettingsTests`; mock e2e `account-pages.mock.e2e.spec.ts` · Run: Integration `MfaFlowTests` (30: `DisableMfa_RevokesSessionsAndRemovesChallengeRequirement`, a raw challenge or a wrong-purpose proof cannot disable), Domain `UserSecuritySettingsTests` (6) · Real e2e `frontend/e2e/real/mfa-enrollment-step-up.real.e2e.spec.ts` (turning off asks for a fresh code, then the session ends and the page says two-factor is off) · Accepted 2026-11-01 by an acceptance agent that did not build it, at `a6898a75e` (branch accept/first-done-batch-2 on develop `f830571c4`). Backend: Application `DisableMfaTests` 5 (an off factor is a success no-op that consumes nothing and records nothing; no proof is 412 and the factor stays; a wrong, spent or other-purpose proof is refused and the factor and sessions stay; a proof turns it off, voids the codes, ends every session and records one `MfaDisabled` line; another person's factor and sessions are never touched), Integration `MfaFlowTests` 30 (`DisableMfa_RevokesSessionsAndRemovesChallengeRequirement`, a raw challenge or a wrong-purpose proof cannot disable), Domain `UserSecuritySettingsTests` 6. No expected version exists on these writes: each is an absolute, idempotent end state or a single-use proof consumed atomically (catalog §1.3, ID-015). Sessions and factors belong to a person, not to a workspace, so the cross-tenant negative is the other person's id. Frontend: `account-security.interaction.component.test.tsx` 16 and `account-security-flows.interaction.component.test.tsx` 9 (vitest web), `apps/web` `account-pages.component.test.tsx` 33 (turn off asks for a code, then the off panel says every session ended and focuses Sign in). Keyboard: the account-security-flows spec turns it off with Enter and typing only. Accessibility: axe (all rules, colour contrast included) on the disable code prompt, the two-factor-off panel at 1440 and 390 px in light and dark: no serious or critical violation (`frontend/e2e/mock/account-security-flows.mock.e2e.spec.ts`, 7 passed; `design-fidelity-account.mock.e2e.spec.ts` 4 passed; `account-pages.mock.e2e.spec.ts` 8 passed on the same run, 46 of 46 for the mock specs of this batch: flows 7, sign-in 7, card popup and copy link 20, design fidelity 4, account pages 8). Design, measured: the security page at 1440 in light and dark within 1 px of Final-Account `bao-mat` (`design-fidelity-account.mock.e2e.spec.ts`); at 390 the account-security-flows spec asserts no sideways scroll, every button and field at least 44 px tall and axe clean in each state. Reviewed, no artboard draws them: the turn-off prompt, the code prompt and the 'off' panel with its Sign in button. Built from the account page components and tokens on the pattern of the security page; acceptance screenshots at 1440, 1024 and 390 in light and dark were taken by the build lane and the acceptance agent opened the code prompt at 390 in dark. No 1024 artboard exists and no spec asserts 1024, so 1024 is reviewed from screenshots only. Real e2e: `mfa-enrollment-step-up.real.e2e.spec.ts` 1 passed: turning off asks for a fresh code, then the session ends and the page says two-factor is off. Real stack, run at `a6898a75e`: `mfa-enrollment-step-up.real.e2e.spec.ts` 1 passed, `session-bootstrap.real.e2e.spec.ts` 3 passed (12 of 12 in the run with `work-item-states.real.e2e.spec.ts`). Lead acceptance: every line in §7 held on this run.
- State: DONE. Accepted 2026-11-01 by an acceptance agent that did not build it, at `a6898a75e` (branch accept/first-done-batch-2 on develop `f830571c4`). Built: the endpoint carries the `AuthSensitiveByUser` limit; the confirmation says what turning off does and its button is the danger one; the producer ends every session of the person, this one included (reason `mfa-disabled`, access cut off), so the page shows "Two-factor authentication is off" with a Sign in button instead of reading anything further; the mock does the same. Carried out of J03 until they can exist: the refusal and the 'blocked with reason' state for an Account policy that requires two-factor belong to UC-IDN-J05, which is BLOCKED on a decision; no policy fact exists anywhere, so no such refusal can happen today. The lead may overrule this carve-out.

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
- Input: the purpose (disable MFA, regenerate codes, link or unlink a provider, issue an API token, change password, revoke other sessions).
- Output: the factor to use: MFA first, then password, then provider re-authentication.
- Main flow: 1. The page posts `POST /api/v1/auth/security/step-up/requirement`. 2. It shows the matching prompt.
- No-op / rejection / concurrency / retry: read-like; no state change; a purpose with no mapping is refused with 400 (a number or an unknown name used to parse to an undefined purpose and fail as a 500; this holds for the three step-up calls and the provider step-up start).
- UI states: pending (the security page's buttons wait while the requirement is read), prompt (code, password, or the provider: one "Confirm with <provider>" button per linked provider, no field), provider refused (the alert says the provider could not confirm the person and the button stays), no linked provider (a line saying to set a password or turn on two-factor).
- Rules: ID-006; IAREQ056.
- Acceptance: Given MFA on, when the requirement is asked, then it says MFA; given a provider-only person, then provider.
- Evidence: BE `Security/Queries/GetStepUpRequirement`, `Security/StepUpPurposeMapping.cs`; API `Auth/Commands/SecurityStepUpEndpoints.cs`, `StepUpPurposeInput.cs` (`POST /api/v1/auth/security/step-up/requirement`) · FE `account.service.ts` · Design Final-Account · Tests Application `SecurityStepUpServiceThrottleTests` · Run: Integration `AccountSecurityIntegrationTests` (10), `MfaFlowTests` (30), Application `SecurityStepUpServiceThrottleTests` (5), `SecurityStepUpServiceConsumeTests` (7) · Real e2e `frontend/e2e/real/mfa-enrollment-step-up.real.e2e.spec.ts` (the page asks for the requirement and shows the password prompt without a factor and the code prompt with one) · Acceptance run 2026-11-01 by an acceptance agent that did not build it, at `a6898a75e` (branch accept/first-done-batch-2 on develop `f830571c4`). Backend: Application `SecurityStepUpServiceFactorTests` 16 (MFA first with a challenge bound to the session and purpose, then password with no challenge, then provider re-authentication; another person's active factor does not make mine MFA; no session is 412 and asks nothing), `SecurityStepUpServiceThrottleTests` 5, `SecurityStepUpServiceConsumeTests` 7, API `StepUpPurposeEndpointTests` 14 (a number or an unknown name is 400 on all three step-up calls; 401 without a session), Integration `AccountSecurityIntegrationTests` 10 and `MfaFlowTests` 30. No expected version exists on these writes: each is an absolute, idempotent end state or a single-use proof consumed atomically (catalog §1.3, ID-015). Sessions and factors belong to a person, not to a workspace, so the cross-tenant negative is the other person's id. Frontend: `account-security.interaction.component.test.tsx` 16 and `account-security-flows.interaction.component.test.tsx` 9 (vitest web), `apps/web` `account-pages.component.test.tsx` 33 (the matching prompt: code when a factor is on, password otherwise; the page's buttons wait while the requirement is read). Keyboard: the account-security-flows spec reaches both prompts with Enter. Accessibility: axe (all rules, colour contrast included) on the code prompt and the password prompt at 1440 and 390 px in light and dark: no serious or critical violation (`frontend/e2e/mock/account-security-flows.mock.e2e.spec.ts`, 7 passed; `design-fidelity-account.mock.e2e.spec.ts` 4 passed; `account-pages.mock.e2e.spec.ts` 8 passed on the same run, 46 of 46 for the mock specs of this batch: flows 7, sign-in 7, card popup and copy link 20, design fidelity 4, account pages 8). Design, measured: the security page at 1440 in light and dark within 1 px of Final-Account `bao-mat` (`design-fidelity-account.mock.e2e.spec.ts`); at 390 the account-security-flows spec asserts no sideways scroll, every button and field at least 44 px tall and axe clean in each state. Reviewed, no artboard draws them: neither step-up prompt. Built from the account page components and tokens on the pattern of the security page; acceptance screenshots at 1440, 1024 and 390 in light and dark were taken by the build lane and the acceptance agent opened the K05 password prompt at 1024 in light, the same panel. No 1024 artboard exists and no spec asserts 1024, so 1024 is reviewed from screenshots only. Real e2e: `mfa-enrollment-step-up.real.e2e.spec.ts` 1 passed: the page asks for the requirement and shows the password prompt without a factor and the code prompt with one. Real stack, run at `a6898a75e`: `mfa-enrollment-step-up.real.e2e.spec.ts` 1 passed, `session-bootstrap.real.e2e.spec.ts` 3 passed (12 of 12 in the run with `work-item-states.real.e2e.spec.ts`). Lead ruling: not DONE until the K06 provider step-up screen exists. · Fix run 2026-11-01 at `4f75dfe77` (fix lane, which built the fix; the lead reviews): line 1 Backend: Application `SecurityStepUpServiceFactorTests` 16 (provider factor produced, unchanged), `CompleteOAuthStepUpTests` 7 (the linked subject buys one proof bound to the state's user, session and purpose; enrolment gets two different proofs; another subject, a subject linked to another person, an unknown, spent or other-flow state and a provider error grant nothing and never reach the provider), API `OAuthStepUpProofHandoffTests` 10 (the proof leaves in a httpOnly, `SameSite=Lax`, path-scoped cookie of at most 5 minutes and never in the URL; one claim returns it and deletes the cookie; no cookie answers 400 `identity.security.step-up-invalid`; no session answers 401; a failure sets no cookie and returns to the page with `stepUpResult=failed`; an unsafe stored return path is dropped; the web app on another origin is returned to, not the API host), Integration `MfaFlowTests` 35 (`ProviderStepUpProofs_AreBoundToUserSessionAndPurpose_AndSpendOnce`, `ProviderStepUpProof_IsSpentByExactlyOneOfManyParallelCallers` on real Redis), Architecture 666, API suite 761, Application 2155, Infrastructure 399, Domain 3383 (full suites run before the merge of develop). No expected version applies: the proof is a single-use value consumed atomically (catalog §1.3, ID-015); the cross-tenant negative is the other person's subject, session and proof. Line 2 Frontend: `account-security-flows.interaction.component.test.tsx` 11 (the provider prompt has no field and focuses its first button, the refused state alerts in both locales, the no-provider line), `apps/web` `account-pages.component.test.tsx` 38 (the provider is offered and the browser leaves for the start URL with the page to return to; back with `stepUp=enroll` the first proof starts the enrolment and the second activates it; back with `stepUp=others` the proof signs out the other devices; a refused return and a refused claim say so and offer the provider again, and a refusal asks nothing of the backend). Keyboard: `account-security-flows.mock.e2e.spec.ts` presses Enter on "Bật xác thực hai yếu tố", finds the provider button focused, presses Enter and lands on the QR step. Accessibility: axe (all rules, contrast included) on the provider prompt, the setup that follows and the refused state at 1440 and 390 px in light and dark: no serious or critical violation; no sideways scroll and every button at least 44 px tall at 390 (`account-security-flows.mock.e2e.spec.ts` 11 passed with the 4 new). Line 3 Design: no artboard draws either provider state; built from the account page components and tokens on the pattern of the code and password prompts; screenshots of the prompt and the refused state at 1440, 1024 and 390 in light and dark were taken and opened (no overflow, clipping or squeezed columns); 1024 is reviewed from screenshots only, as for K04. The security page itself is still measured within 1 px (`design-fidelity-account.mock.e2e.spec.ts` 4 passed). Line 4 Real e2e: `provider-step-up.real.e2e.spec.ts` on the real stack, a person made provider-only in the stack's database: the summary says no password and one linked provider, the requirement answers `OAuth`, the page shows the provider button and no password field; the main flow of K03 is the choice of factor, which this covers. Rejection: the real backend has no provider, so the start refuses and the browser comes back to the page with the message; the claim with no cookie answers 400; two-factor stays off. Real stack (own compose project, ports 5481 and 5482, run at `4f75dfe77` with develop merged): `provider-step-up.real.e2e.spec.ts` 1 passed with `mfa-enrollment-step-up.real.e2e.spec.ts` 1 passed and `session-bootstrap.real.e2e.spec.ts` 3 passed (5 of 5); an earlier run of the same specs failed once on the per-person step-up limit (429) because the spec reused the two-factor spec's person, and it now uses its own. Mock specs at the same head: `account-security-flows` 11, `auth-flow` 23, `account-pages` 8 and `design-fidelity-account` 4 (41 of 41). Line 5: this is the fix lane's run, not an independent acceptance agent's; the lead reviews it. · Acceptance 2026-11-02 at `6ac912d0c` (independent acceptance agent, branch accept/batch-1): line 1 `SecurityStepUpService*` and `StepUpPurposeEndpointTests` (inside the filtered Application 150 and API 97, 0 failed), Postgres Integration `AccountSecurityIntegrationTests` 10 and `MfaFlowTests` 35 passed. Line 2 vitest web 68 passed (code prompt, password prompt, provider button per linked provider, provider refused, no linked provider). Line 3 `design-fidelity-account` 4 passed; the provider prompt and the refused state (no artboard) were screenshotted at 1440, 1024 and 390 in light and dark and opened: no overflow or clipping, axe no serious or critical, page controls at least 44 px at 390. Keyboard: `account-security-flows.mock.e2e.spec.ts` 11 passed. Line 4 `mfa-enrollment-step-up.real.e2e.spec.ts` 1 passed (password prompt without a factor, code prompt with one) and `provider-step-up.real.e2e.spec.ts` 1 passed (the server answers the provider factor, the page shows 'Confirm with Google' and no field, the refusal returns with the message and nothing is claimed); the provider success round trip needs a real provider and is not run (stated limit). Verdict: all five lines hold.
- State: DONE — accepted by independent acceptance 2026-11-02 at `6ac912d0c` (branch accept/batch-1); earlier accepted 2026-11-01 by the fix lane at `4f75dfe77` on branch idn/h02-k03 (PR into develop), after the lead's ruling that it waits for the provider step-up screen; the lead reviews this Evidence line at merge. Built: with the provider factor the page shows "Confirm with <provider>" for each linked provider (the summary names them) instead of a password field, leaves for `GET /api/v1/auth/oauth/{provider}/step-up/start` with the page to come back to, and on return spends the proof on the action that asked (UC-IDN-K06). Known limits: a person with a provider and no password still sees "Change password" on the page, which asks for a current password they do not have (K07 and the first-password flow K08 own it); the success round trip needs a real provider, which the development stack has none of (see K06).

### UC-IDN-K04 Step up with a second-factor code
- Capability: K Security settings
- Actors and permission: signed-in person; `AuthSensitiveByUser`.
- Preconditions: an active factor; a challenge bound to this session.
- Input: purpose, code (TOTP or recovery code).
- Output: a single-use step-up proof bound to the user, the session and the purpose, short lived.
- Main flow: 1. The person types the code. 2. The client posts `POST /api/v1/auth/security/step-up/complete-mfa`. 3. The page sends the proof with the sensitive request.
- No-op / rejection / concurrency / retry: the proof works once, for that purpose and that session only; wrong codes count against the lockout; a locked attempt is not verified.
- UI states: code entry, wrong code, expired (the challenge is out of tries or the proof is too old: the page opens a fresh challenge for the same step and says so, instead of repeating "wrong code"), locked, success.
- Rules: ID section 67, ID-006; IAREQ056.
- Acceptance: Given a proof, when used twice, then the second use fails. Given a proof for another purpose, then it fails.
- Evidence: BE `Security/Commands/CompleteStepUpMfa`; API `SecurityStepUpEndpoints.cs` (`POST /api/v1/auth/security/step-up/complete-mfa`) · FE `account-security-surface.tsx` · Design Final-Account · Tests Application `SecurityStepUpServiceThrottleTests`; mock e2e `account-pages.mock.e2e.spec.ts` · Run: Integration `AccountSecurityIntegrationTests` (10), `MfaFlowTests` (30), Application `SecurityStepUpServiceThrottleTests` (5), `SecurityStepUpServiceConsumeTests` (7) · Real e2e `frontend/e2e/real/mfa-enrollment-step-up.real.e2e.spec.ts` (turning off is bought with a code; the code works once) · Accepted 2026-11-01 by an acceptance agent that did not build it, at `a6898a75e` (branch accept/first-done-batch-2 on develop `f830571c4`). Backend: Application `SecurityStepUpServiceFactorTests` 16 (right code issues one proof bound to user, session and purpose; a wrong code answers invalid-code, keeps the challenge, counts a failure and issues no proof; the fifth wrong code kills the challenge; a challenge of another user, session or purpose, an expired one or an unknown one is refused before any code is checked), `SecurityStepUpServiceThrottleTests` 5, `SecurityStepUpServiceConsumeTests` 7 (replay, other session, other user, other purpose, expired, mismatch burns the proof), Integration `MfaFlowTests` 30 (`VerifiedProof_SucceedsOnce_AndReplayIsRejected`, `AtomicConsume_UnderConcurrency_OnlyOneCallerWins`, `ProofForWrongPurpose_...`, `ProofForAnotherSession_...`, `ProofForAnotherUser_...`) and `AccountSecurityIntegrationTests` 10 (`StepUpMfa_AfterMaxBadAttempts_IsLocked_...`). No expected version exists on these writes: each is an absolute, idempotent end state or a single-use proof consumed atomically (catalog §1.3, ID-015). Sessions and factors belong to a person, not to a workspace, so the cross-tenant negative is the other person's id. Frontend: `account-security.interaction.component.test.tsx` 16 and `account-security-flows.interaction.component.test.tsx` 9 (vitest web), `apps/web` `account-pages.component.test.tsx` 33 (wrong, throttled with the seconds to wait and Confirm held, expired with a fresh challenge opened for the same step, failed, pending). Keyboard: the account-security-flows spec types a wrong code and Enter, sees the alert and `aria-invalid`, then the right code and Enter. Accessibility: axe (all rules, colour contrast included) on the code prompt, the wrong-code message, the throttled step at 1440 and 390 px in light and dark: no serious or critical violation (`frontend/e2e/mock/account-security-flows.mock.e2e.spec.ts`, 7 passed; `design-fidelity-account.mock.e2e.spec.ts` 4 passed; `account-pages.mock.e2e.spec.ts` 8 passed on the same run, 46 of 46 for the mock specs of this batch: flows 7, sign-in 7, card popup and copy link 20, design fidelity 4, account pages 8). Design, measured: the security page at 1440 in light and dark within 1 px of Final-Account `bao-mat` (`design-fidelity-account.mock.e2e.spec.ts`); at 390 the account-security-flows spec asserts no sideways scroll, every button and field at least 44 px tall and axe clean in each state. Reviewed, no artboard draws them: the code prompt, wrong code, expired, locked and too-many-tries states. Built from the account page components and tokens on the pattern of the security page; acceptance screenshots at 1440, 1024 and 390 in light and dark were taken by the build lane and the acceptance agent opened the throttled step at 1440 in light. No 1024 artboard exists and no spec asserts 1024, so 1024 is reviewed from screenshots only. Real e2e: `mfa-enrollment-step-up.real.e2e.spec.ts` 1 passed: turning off is bought with a code that works once. Rejection: the wrong first code at activation is refused on the real stack. Real stack, run at `a6898a75e`: `mfa-enrollment-step-up.real.e2e.spec.ts` 1 passed, `session-bootstrap.real.e2e.spec.ts` 3 passed (12 of 12 in the run with `work-item-states.real.e2e.spec.ts`). Lead acceptance: every line in §7 held on this run.
- State: DONE. Accepted 2026-11-01 by an acceptance agent that did not build it, at `a6898a75e` (branch accept/first-done-batch-2 on develop `f830571c4`). Built: the step-up challenge dies at the fifth wrong code (it was removed only by the attempt after it, as the sign-in challenge used to be); a code that already opened something is refused (UC-IDN-I01 guard, shared verifier); the dead-challenge state on the page. Known limit (UC-IDN-D01): the proof is bound to a session id that refresh replaces, so a proof held across a refresh is refused and the page shows the expired state. Observed, not gating: the throttled message reads 'Try again in 1 seconds' (no singular form in the copy).

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
- Evidence: BE `Security/Commands/CompleteStepUpPassword`; API `SecurityStepUpEndpoints.cs` (`POST /api/v1/auth/security/step-up/complete-password`) · FE `account-password-panel.tsx` · Design Final-Account · Tests Application `SecurityStepUpServiceThrottleTests` · Run: Integration `AccountSecurityIntegrationTests` (10), `MfaFlowTests` (30), Application `SecurityStepUpServiceThrottleTests` (5), `SecurityStepUpServiceConsumeTests` (7) · Real e2e `frontend/e2e/real/mfa-enrollment-step-up.real.e2e.spec.ts` (turning on is bought with the current password) · Accepted 2026-11-01 by an acceptance agent that did not build it, at `a6898a75e` (branch accept/first-done-batch-2 on develop `f830571c4`). Backend: Application `SecurityStepUpServiceFactorTests` 16 (a right password issues one proof bound to user, session and purpose; a wrong one answers invalid-password; a provider-only person is refused and gets no proof), `SecurityStepUpServiceThrottleTests` 5 (locked attempt is not verified, wrong counts, right clears the budget), Integration `AccountSecurityIntegrationTests` 10 (lock after the maximum wrong guesses even for the right password with Retry-After, another person on the same address is not locked, success resets the budget) and `MfaFlowTests` 30. No expected version exists on these writes: each is an absolute, idempotent end state or a single-use proof consumed atomically (catalog §1.3, ID-015). Sessions and factors belong to a person, not to a workspace, so the cross-tenant negative is the other person's id. Frontend: `account-security.interaction.component.test.tsx` 16 and `account-security-flows.interaction.component.test.tsx` 9 (vitest web), `apps/web` `account-pages.component.test.tsx` 33 (password entry; wrong password in words with `aria-invalid`; throttled; pending). Keyboard: the account-security-flows spec types the password and Enter at the proof step. Accessibility: axe (all rules, colour contrast included) on the password prompt and the wrong-password message at 1440 and 390 px in light and dark: no serious or critical violation (`frontend/e2e/mock/account-security-flows.mock.e2e.spec.ts`, 7 passed; `design-fidelity-account.mock.e2e.spec.ts` 4 passed; `account-pages.mock.e2e.spec.ts` 8 passed on the same run, 46 of 46 for the mock specs of this batch: flows 7, sign-in 7, card popup and copy link 20, design fidelity 4, account pages 8). Design, measured: the security page at 1440 in light and dark within 1 px of Final-Account `bao-mat` (`design-fidelity-account.mock.e2e.spec.ts`); at 390 the account-security-flows spec asserts no sideways scroll, every button and field at least 44 px tall and axe clean in each state. Reviewed, no artboard draws them: the password prompt and the wrong-password message. Built from the account page components and tokens on the pattern of the security page; acceptance screenshots at 1440, 1024 and 390 in light and dark were taken by the build lane and the acceptance agent opened the password prompt at 1024 in light. No 1024 artboard exists and no spec asserts 1024, so 1024 is reviewed from screenshots only. Real e2e: `mfa-enrollment-step-up.real.e2e.spec.ts` 1 passed: turning on is bought with the current password; a wrong password is refused first. Real stack, run at `a6898a75e`: `mfa-enrollment-step-up.real.e2e.spec.ts` 1 passed, `session-bootstrap.real.e2e.spec.ts` 3 passed (12 of 12 in the run with `work-item-states.real.e2e.spec.ts`). Lead acceptance: every line in §7 held on this run.
- State: DONE. Accepted 2026-11-01 by an acceptance agent that did not build it, at `a6898a75e` (branch accept/first-done-batch-2 on develop `f830571c4`). 

### UC-IDN-K06 Step up with a linked provider
- Capability: K Security settings
- Actors and permission: signed-in person with only provider sign-in.
- Preconditions: a linked provider.
- Input: purpose.
- Output: a single-use proof after a fresh provider login.
- Main flow: 1. The page opens `GET /api/v1/auth/oauth/{provider}/step-up/start?purpose=<name>&returnUrl=<internal path>`. 2. The provider returns to `GET /api/v1/auth/oauth/{provider}/step-up/callback`; the backend keeps the proof in a short-lived httpOnly cookie (path `/api/v1/auth/security/step-up`, never in a URL) and sends the browser back to the page. 3. The page reads it once with `POST /api/v1/auth/security/step-up/claim-provider` and continues the sensitive action; turning two-factor on gets a second proof from the same re-authentication for its activation.
- No-op / rejection / concurrency / retry: state is single use; a different provider subject fails; stale callbacks fail.
- UI states: redirecting (the button waits), success (the action goes on), error (the page says the provider could not confirm the person and offers it again).
- Rules: ID-006, ID-008.
- Acceptance: Given a provider-only person, when step-up completes, then one proof exists.
- Evidence: BE `OAuth/Commands/StartOAuthStepUp`, `CompleteOAuthStepUp` (`OAuthStepUpResult`), `Security/DTOs/StepUpResults.cs` (`ProviderStepUpClaimResult`), `Common/Security/Auth/ICookieService.cs`; Infrastructure `Auth/Cookies/CookieService.cs`; API `Auth/Commands/StartOAuthStepUpEndpoint.cs`, `CompleteOAuthStepUpEndpoint.cs`, `SecurityStepUpEndpoints.cs` (`POST /api/v1/auth/security/step-up/claim-provider`) · FE `packages/features/account/src/web/security/account-security-surface.tsx` (provider prompt), `apps/web/src/routes/workspaces/$workspaceId/account/security.tsx`, `account.service.ts` (`providerStepUpPath`, `claimProviderStepUp`) · Design none (undrawn, built from tokens) · Tests Application `CompleteOAuthStepUpTests` 7, API `OAuthStepUpProofHandoffTests` 10, Integration `MfaFlowTests` (the provider proof tests), FE and mock as in K03 · Real e2e `frontend/e2e/real/provider-step-up.real.e2e.spec.ts` (the offer and the refusal; the provider login itself cannot run here)
- State: HARDENING. Built 2026-11-01 with the K03 fix at `4f75dfe77`. Runs end to end against the real backend up to the provider: the offer, the start, the refusal and the empty claim. Gap: no Google or Microsoft app is set up on the development stack, so a real provider login that grants the proof has not run; the grant, the cookie handoff, the claim, the single use and the use of the proofs by enrolment are proven with a fake provider (Application, API, Integration) and the mock backend plays the round trip in the page. Owner check and a real provider run are the way to DONE. Not covered: linking or unlinking a provider by step-up (G01, G02) and the first password for a person with no password (K08).
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
- State: INTEGRATING. Reviewed, nothing changed: the current password and (with a factor on) the proof are both checked and share the step-up lockout; the validator refuses the same password; every session, this one too, ends and the user's access tokens are cut off. Missing: tests and owner check, real spec.

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
- Evidence: BE `Profiles/Commands/UpdateEmail/UpdateEmail.cs` (needs a `ChangeSecurityIdentity` proof when the address changes, checked before anything else so an unproven session cannot probe which addresses are taken; still changes the email at once and answers "Email is already in use." to a proven one); API `Profile/Commands/UpdateEmailEndpoint.cs` (`POST /api/v1/profile/email`, body `{ email, stepUpToken }`, 412 without a proof) · FE none · Design none · Tests Application `UpdateEmailTests`, API `ProfileEndpointTests` · Real e2e none
- State: IMPLEMENTING. Built (security stop-gap inside the lead decision): the step-up. Missing: the pending-email model (the sign-in email still changes before the new address is proven), the old-address notice, the neutral answer, other sessions ending on confirmation, the screen, an artboard. `UpdateEmailTests` call the command without a proof and need updating. A session without proof can no longer change the sign-in email, which closed an account takeover path (change the email, then reset the password to it).

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
- Input: name (required, at most 200, the one limit sign-up shares); optional external avatar address (an absolute https address without credentials, at most 500 characters).
- Output: the updated user; the sign-in email and credentials are not touched.
- Main flow: 1. The person edits the name on the Profile page. 2. The save bar appears after a change. 3. The client sends `PATCH /api/v1/profile`.
- No-op / rejection / concurrency / retry: an unchanged name is a no-op; an empty name is a validation error; the write carries no expected version (single owner, last write wins; decision below).
- UI states: pristine, dirty with save bar, pending, success, validation error (empty, or longer than 200 characters).
- Rules: ID section 7 (display change never changes security identity); IAREQ010, IAREQ011.
- Acceptance: Given a new name, when saved, then it shows in `/auth/me`. Given a blank name, then a validation error.
- Evidence: BE `Profiles/Commands/UpdateProfile/UpdateProfile.cs`; API `Profile/Commands/UpdateProfileEndpoint.cs` (`PATCH /api/v1/profile`) · FE `.../account/profile.tsx`, `use-update-profile.ts` · Design Final-Account, Final-M-Account · Tests Application `UpdateProfileTests`, Domain `UserNameTests`, API `ProfileEndpointTests`; mock e2e `account-pages.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING. Built: the display name limit was 100 in the user and 200 in the validators (a name of 101 to 200 characters passed validation and failed as a rule error), now one constant `User.MaxNameLength` = 200 used by the user and both validators; the page refuses a longer name; the external photo address must be an absolute https address without credentials (it was any string, shown in other members' browsers); a signed-in person who no longer exists answers 401 like the session read (this holds for every handler that acts for the signed-in person). Missing: tests and owner check (the Domain test that refuses 101 characters, `UserNameTests`, and any 404 assertion for a missing person need updating), real spec. The external address still duplicates M03; the web never sends it.

### UC-IDN-M02 Read and save my preferences
- Capability: M Identity profile read and update
- Actors and permission: signed-in person; own preferences.
- Preconditions: none.
- Input: partial body with required `expectedVersion` (0 when never saved): locale, time zone, theme, phone, `phoneVisibleToWorkspaceMembers`.
- Output: the saved preferences with a new version.
- Main flow: 1. The page reads `GET /api/v1/profile/preferences`. 2. The person edits and saves with `PATCH /api/v1/profile/preferences`. 3. The phone is normalized to E.164 (`+` and 8 to 15 digits); a blank clears it.
- No-op / rejection / concurrency / retry: no change bumps no version; a stale version answers 412 and the page offers reload; a missing version is a validation error; an invalid phone is refused; the phone shows to others only under the rule of ID section 7.
- UI states: loading, pristine, dirty, pending, validation error, conflict (nothing was saved; the page says so and shows what is stored), success.
- Rules: ID section 7 (D15); ID-015.
- Acceptance: Given two devices, when both save from version 3, then one succeeds and one gets 412 and changes nothing.
- Evidence: BE `Profiles/Queries/GetAccountPreferences`, `Profiles/Commands/UpdateAccountPreferences/UpdateAccountPreferences.cs`; API `Profile/Commands/AccountPreferencesEndpoints.cs` (`GET`, `PATCH /api/v1/profile/preferences`) · FE `.../account/profile.tsx`, `appearance.tsx`, `packages/features/account/src/core/phone.ts`, `web/profile/account-profile-surface.tsx` · Design Final-Account, Final-M-Account · Tests Domain `UserProfileAccountPreferencesTests`, `UserPreferencesTests`, Application `UpdateAccountPreferencesTests`, `GetAccountPreferencesTests`, API `AccountPreferencesEndpointTests`, FE `phone.unit.test.ts` · Real e2e none
- State: INTEGRATING. Built: the page shows a conflict sentence on a 412 instead of the generic error. Missing: tests and owner check, real spec. The profile page saves the name, the preferences and the job title in three calls; a failure after the first leaves it saved (the page refetches and says nothing was saved only for a preferences conflict).

### UC-IDN-M03 Upload my profile photo
- Capability: M Identity profile read and update
- Actors and permission: signed-in person; own photo.
- Preconditions: none.
- Input: an image of at most 2,000,000 bytes; PNG, JPEG or WebP recognized from the bytes.
- Output: the photo stored; the previous object is left for the background sweep.
- Main flow: 1. The person picks a file. 2. The client sends `PUT /api/v1/profile/avatar`. 3. The page shows the new photo.
- No-op / rejection / concurrency / retry: SVG, GIF or any other type answers 415 `identity.avatar.unsupported-format`; too large answers 413 `identity.avatar.too-large`; changing the name never touches the photo.
- UI states: pending (the photo buttons wait and a status line says the photo is updating), success, unsupported, too large (the web refuses above 1,500,000 bytes before sending, the producer above 2,000,000).
- Rules: ID section 7 (photo).
- Acceptance: Given a PNG renamed `.jpg`, when uploaded, then it is accepted by its bytes; given an SVG, then 415.
- Evidence: BE `Profiles/Commands/UploadAvatar/UploadAvatar.cs`; API `Profile/Commands/AvatarEndpoints.cs` (`PUT /api/v1/profile/avatar`) · FE `.../account/profile.tsx` · Design Final-Account · Tests Application `AvatarCommandsTests`, API `AvatarEndpointTests`, Domain `UserAvatarTests`; mock e2e `account-pages.mock.e2e.spec.ts` · Real e2e none
- State: INTEGRATING. Built: the pending state. Missing: tests and owner check, real spec.

### UC-IDN-M04 Remove my profile photo
- Capability: M Identity profile read and update
- Actors and permission: signed-in person; own photo.
- Preconditions: none.
- Input: none.
- Output: no photo; the object is swept later.
- Main flow: 1. The person chooses Remove. 2. The client sends `DELETE /api/v1/profile/avatar`.
- No-op / rejection / concurrency / retry: removing when none exists is a no-op.
- UI states: destructive confirmation (in place, the safe answer is focused and focus returns to the button), pending, success.
- Rules: ID section 7.
- Acceptance: Given a photo, when removed, then a read of it answers 404.
- Evidence: BE `Profiles/Commands/RemoveAvatar/RemoveAvatar.cs`; API `Profile/Commands/AvatarEndpoints.cs` (`DELETE /api/v1/profile/avatar`) · FE `.../account/profile.tsx` · Design Final-Account · Tests Application `AvatarCommandsTests`, API `AvatarEndpointTests` · Real e2e none
- State: INTEGRATING. Built: the confirmation and the pending state. Missing: tests and owner check, real spec; proof that the sweep removes only unreferenced objects.

### UC-IDN-M05 Read a person's photo
- Capability: M Identity profile read and update
- Actors and permission: the person, or an active member of a Workspace they share (the product text names no guest exclusion; the stricter non-guest rule is the phone's, ID section 7).
- Preconditions: an uploaded photo.
- Input: user id; version in `v`.
- Output: the image streamed with `nosniff`, a locked-down CSP and private immutable cache.
- Main flow: 1. A face renders `GET /api/v1/users/{userId}/avatar?v=...`.
- No-op / rejection / concurrency / retry: any other reader, and any person without an uploaded photo, gets the same 404; a share link or public view never reads it.
- UI states: image, initials fallback.
- Rules: ID section 7; ID-012.
- Acceptance: Given a reader who shares no workspace, when they read it, then 404 identical to "no photo".
- Evidence: BE `Profiles/Queries/GetUserAvatar`; API `Users/MapUserAvatarEndpoints.cs` (`GET /api/v1/users/{userId}/avatar`) · FE `packages/features/account/src/web/profile/account-profile-surface.tsx` · Design Final-Account · Tests Application `GetUserAvatarTests`, API `AvatarEndpointTests` · Real e2e none
- State: INTEGRATING. Nothing changed in this build; the read already answers one 404 for a stranger, for a person without a photo and for a person who does not exist. Missing: tests and owner check, real spec, cross-workspace negative at the HTTP level.

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
