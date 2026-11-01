---
document_id: WRK-UC-BILLING
document_type: workstream-use-case-catalog
status: active
owner: billing-entitlements-team
applies_to:
  - billing
  - plans
  - subscriptions
  - entitlements
  - usage
  - invoices
  - payment-methods
  - billing-administration
evidence:
  - docs/product/billing.md
  - docs/workstreams/teams/billing-entitlements.md
  - docs/workstreams/executions/billing-entitlements/billing-entitlements.spec.md
  - docs/workstreams/executions/billing-entitlements/billing-entitlements.certification.md
  - docs/workstreams/executions/billing-entitlements/billing-entitlements.tests.md
  - backend/src/Notrelix.API/Endpoints/Billing/
  - frontend/packages/features/billing/
review_on:
  - capability-state-change
  - product-semantic-change
  - billing-api-change
  - provider-selection
---

# Use cases: Billing

Context code `BIL`. Product meaning: [billing](../../product/billing.md). Team spec: [billing-entitlements](../teams/billing-entitlements.md). Execution pack: [spec](../executions/billing-entitlements/billing-entitlements.spec.md), [certification](../executions/billing-entitlements/billing-entitlements.certification.md), [tests](../executions/billing-entitlements/billing-entitlements.tests.md). Router and rules: [README](README.md). Delivery context: [capability-delivery-map](../capability-delivery-map.md), [cross-team-dependencies](../cross-team-dependencies.md).

Two numbering systems meet in this file. `BIL-001` to `BIL-033` in the product doc are rules (cited as "product BIL-008"). `BIL-001` to `BIL-024` in the team spec are capabilities; this catalog calls them by letter (Part 2). `BILREQnnn` are execution-pack requirements.

# 1. Assessment

## 1.1 Where Billing stands

Billing is the thinnest delivered context compared with its domain model. The tenant API serves two reads: the plan and the capabilities. The platform operator console (`/operator/v1`, a separate principal and database login) serves the plan catalog by revision, the plan change, manual overrides and subscription termination, each audited. The plan catalog is in the database (revisions, never hard-coded). Entitlement, usage and capacity logic that other contexts need (seat gate, automation rule capacity) works. The domain aggregates for plans, prices, invoices, payment methods, customers, provider events and metered usage exist but no handler uses them. No payment provider exists, by owner decision.

The frontend has two worlds. The real route shows a capabilities page with a read-only plan card (the plan is the operator's to change); the operator console is its own host (`frontend/apps/operator`). A full "Plans and billing" experience (overview, comparison, invoices, seat purchase, downgrade, past due) runs only on mock handlers behind `backendCapabilities.billingPlans`.

## 1.2 Capabilities and state counts

Letter maps to the team spec capability. Total use cases: **99**.

| Letter | Capability | Use cases | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | DONE | BLOCKED |
|---|---|---|---|---|---|---|---|---|
| A | BIL-001 Plan/catalog model | 5 | 1 | 2 | 1 | 1 | 0 | 0 |
| B | BIL-002 Subscription lifecycle | 6 | 5 | 1 | 0 | 0 | 0 | 0 |
| C | BIL-003 Subscription transition policy | 2 | 1 | 1 | 0 | 0 | 0 | 0 |
| D | BIL-004 Entitlement model | 3 | 1 | 0 | 2 | 0 | 0 | 0 |
| E | BIL-005 Entitlement evaluation | 4 | 2 | 0 | 0 | 2 | 0 | 0 |
| F | BIL-006 Product entitlement contract | 4 | 2 | 1 | 1 | 0 | 0 | 0 |
| G | BIL-007 Usage event/record contract | 2 | 2 | 0 | 0 | 0 | 0 | 0 |
| H | BIL-008 Usage aggregation | 4 | 3 | 1 | 0 | 0 | 0 | 0 |
| I | BIL-009 Usage limit enforcement handoff | 5 | 1 | 1 | 3 | 0 | 0 | 0 |
| J | BIL-010 Billing administration | 5 | 2 | 2 | 0 | 1 | 0 | 0 |
| K | BIL-011 Payment-method lifecycle | 5 | 4 | 1 | 0 | 0 | 0 | 0 |
| L | BIL-012 Invoice lifecycle | 6 | 2 | 3 | 0 | 0 | 0 | 1 |
| M | BIL-013 Payment lifecycle | 3 | 2 | 1 | 0 | 0 | 0 | 0 |
| N | BIL-014 Provider customer/subscription mapping | 4 | 4 | 0 | 0 | 0 | 0 | 0 |
| O | BIL-015 Provider webhook verification | 1 | 1 | 0 | 0 | 0 | 0 | 0 |
| P | BIL-016 Provider webhook idempotency | 3 | 3 | 0 | 0 | 0 | 0 | 0 |
| Q | BIL-017 Provider state reconciliation | 3 | 2 | 1 | 0 | 0 | 0 | 0 |
| R | BIL-018 Upgrade/downgrade/cancel | 12 | 5 | 5 | 1 | 1 | 0 | 0 |
| S | BIL-019 Trial/grace/delinquency | 6 | 5 | 1 | 0 | 0 | 0 | 0 |
| T | BIL-020 Billing frontend | 4 | 0 | 2 | 0 | 2 | 0 | 0 |
| U | BIL-021 Billing events | 2 | 1 | 1 | 0 | 0 | 0 | 0 |
| V | BIL-022 Billing observability | 2 | 2 | 0 | 0 | 0 | 0 | 0 |
| W | BIL-023 Migration/backfill | 2 | 1 | 1 | 0 | 0 | 0 | 0 |
| X | BIL-024 Hardening/security | 6 | 4 | 2 | 0 | 0 | 0 | 0 |
| | **All** | **99** | **56** | **27** | **8** | **7** | **0** | **1** |

No use case is DONE: none has a recorded owner check or a 1-px design diff. HARDENING is given only where a spec under `frontend/e2e/real/` runs the path (`billing-plan.real.e2e.spec.ts`, which now drives the operator console, and `feature-availability.real.e2e.spec.ts`).

## 1.3 The ten most important gaps

1. **The real product is mock-only.** Overview, comparison, invoices, seat quote and purchase, downgrade, contact sales and past due have no backend endpoint (UC-BIL-A02, B02, L01, R03, R10, R11).
2. **No billing permission.** The plan and capability reads now use `ViewBilling` (Owner only, a custom role may add it; owner-approved tightening 2026-10-31, an Admin no longer reads billing); a tenant has no plan switch, the platform operator changes the plan; billing managers and delegation do not exist (UC-BIL-J02, J05).
3. **No payment provider, no money path.** Payment methods, invoices, payments, checkout, webhooks and reconciliation are DEFINED only (capabilities K to Q). Decision 46 defers it; the domain is ready, the handlers are not.
4. **No lifecycle worker.** Renewal, trial end, scheduled downgrade, cancellation and expiry never run (UC-BIL-B04 to B06, C02, R08).
5. **Delinquency is not enforced.** Past due is a domain state with no trigger, and the seat gate ignores it (UC-BIL-S01, S02).
6. **Seats and storage are enforced in one place or none.** Only invitation acceptance checks seats; storage is displayed but nothing enforces it (UC-BIL-F03, F04).
7. **Entitlements are not derived.** Limits are read from the plan constant; no entitlement rows, no lineage, no version, so caches and propagation cannot exist (UC-BIL-D01, U02).
8. **Metered usage does not exist.** Only capacity (rule count) is metered through the ledger (UC-BIL-G01, H01 to H03).
9. **Denials carry no reason.** `BillingCapabilityFact` has no reason, so the UI cannot tell not entitled from limit reached (UC-BIL-E04, T03).
10. **Plan-change safety.** Account-level plan change has no expected version, no idempotency key, and no uniqueness guard for one active subscription (see 1.4).

## 1.4 Defects in shipped code

| Defect | Where |
|---|---|
| Two concurrent first plan changes by operators can create two active account subscriptions: the operator service adds a row when none is found and the subscription table has no unique index on active account rows (only `WorkspaceId` and `PlanId` indexes). The resolver hides it by taking the newest. | `backend/src/Notrelix.Application/Features/Billing/Operator/OperatorAccountBillingService.cs` (`ChangePlanAsync`); `backend/src/Notrelix.Infrastructure/Data/Configurations/Billing/SubscriptionConfiguration.cs` |
| Seat gate is an optional constructor argument; if it is not registered, invitation acceptance skips the check (fail open). | `backend/src/Notrelix.Application/Features/Workspaces/Invitations/Services/InvitationAcceptanceService.cs`, `.../Commands/AcceptInvitation/AcceptInvitation.cs` |
| Storage limit is read and shown but no producer enforces it. | `backend/src/Notrelix.Application/Features/Billing/Entitlements/Services/BillingCapabilityFactsProvider.cs` (no caller outside Billing) |
| Capability list reads four capabilities one by one, each re-reading account usage (bounded at four, but three reads for seats and storage). | `backend/src/Notrelix.Application/Features/Billing/Entitlements/Queries/GetWorkspaceBillingCapabilities/GetWorkspaceBillingCapabilities.cs` |
| Catalog is a code constant while the Plan, PlanPrice, PlanLimit aggregates and tables exist unused (dual authority risk, BILSTOP001). Frontend catalog stub also exists. | `backend/src/Notrelix.Application/Features/Billing/Plans/BillingPlanCatalog.cs`; `frontend/packages/features/billing/src/web/billing-page.tsx` |
| Dead frontend code: `@notrelix/features-billing` exports a hard-coded three-tier page and unused `Subscription`, `Invoice`, `Entitlement` types; the web route does not use it. | `frontend/packages/features/billing/src/` |
| Mock-only paths: 14 mock routes (`/billing`, `/plans`, `/invoices`, `/invoices/:id`, `/invoices/:id/pdf`, `/invoices/:id/pay`, `/downgrade-preview`, `/downgrade` (POST, DELETE), `/seat-quote`, `/seat-purchases`, `/seat-purchases/:id/check`, `/contact-sales`, `/seat-summary`) have no producer. | `frontend/packages/dev/mock-backend/src/contexts/billing/billing.handlers.ts`, `frontend/apps/web/src/routes/workspaces/$workspaceId/billing/billing-api.ts` |
| Billing query keys are workspace-scoped; product says account-scoped. | `frontend/packages/features/billing/src/core/query/keys.ts`, `billing/use-billing.ts` |
| `Billing:Mode=DevNull` makes every subscription requirement pass; guarded to Development and Testing at registration (verified), no test of the guard found in this review. | `backend/src/Notrelix.Infrastructure/DependencyInjection/BillingRegistration.cs` |
| Frontend tier list (free, pro, business, enterprise) differs from the backend catalog (free, pro, enterprise). | `frontend/packages/features/workspace/src/web/billing/billing-models.ts` |

## 1.5 Enterprise baseline

Compared against Stripe Billing, Atlassian Cloud and Slack. Sources: [Stripe: cancel subscriptions](https://docs.stripe.com/billing/subscriptions/cancel), [Atlassian: improved billing experience](https://support.atlassian.com/subscriptions-and-billing/docs/understand-the-improved-atlassian-billing-experience), [Atlassian: manage your bill](https://confluence.atlassian.com/cloud/manage-your-bill-for-atlassian-products-873871345.html), [Slack: fair billing policy](https://slack.com/help/articles/218915077).

| Leader behavior | Notrelix today | Use case |
|---|---|---|
| Cancel at period end, reversible until the period closes; cancellation takes effect on a separate event (Stripe). | Domain method only. | UC-BIL-R06 to R08 |
| Trial with reminder before the end and a rule for no payment method (Stripe). | None. | UC-BIL-B03, B04, S06 |
| Retries and notices while payment fails, states past due then unpaid (Stripe). | Past due state only. | UC-BIL-S01 to S05 |
| Dedicated billing admin role, several allowed; invoices and payment method under a billing profile (Atlassian). | None; interim permission. | UC-BIL-J02, J04, J05 |
| Change plan from Billing, Enterprise by contact form, monthly to annual by request (Atlassian). | Operator plan change (the tenant has no switch); contact sales mock. | UC-BIL-R01, R02, A05 |
| Member changes prorated with credits; minimum of one paid member; downgrade at renewal or now (Slack). | Seat purchase mock only. | UC-BIL-R11, R12 |
| Hosted portal for payment details; no card data in the app (Stripe, Slack). | None. | UC-BIL-K01, N03, X02 |

Enterprise-baseline use cases added: UC-BIL-J04, K05, L06, R12, S05, S06. Rules needed are in Part 5.

# 2. Index

Letter map (team spec capabilities, in listed order): A = BIL-001 Plan/catalog model; B = BIL-002 Subscription lifecycle; C = BIL-003 Subscription transition policy; D = BIL-004 Entitlement model; E = BIL-005 Entitlement evaluation; F = BIL-006 Product entitlement contract; G = BIL-007 Usage event/record contract; H = BIL-008 Usage aggregation; I = BIL-009 Usage limit enforcement handoff; J = BIL-010 Billing administration; K = BIL-011 Payment-method lifecycle; L = BIL-012 Invoice lifecycle; M = BIL-013 Payment lifecycle; N = BIL-014 Provider customer/subscription mapping; O = BIL-015 Provider webhook verification; P = BIL-016 Provider webhook idempotency; Q = BIL-017 Provider state reconciliation; R = BIL-018 Upgrade/downgrade/cancel; S = BIL-019 Trial/grace/delinquency; T = BIL-020 Billing frontend; U = BIL-021 Billing events; V = BIL-022 Billing observability; W = BIL-023 Migration/backfill; X = BIL-024 Hardening/security.

Columns: BE = backend (Yes, Part = partial, Dom = domain only, No); FE = frontend (Real, Mock, No); Design = artboard state in `Final-Billing.dc.html`; Real e2e = spec under `frontend/e2e/real/`.

| ID | Use case | Capability | Actors | BE | FE | Design | Real e2e | State | Gap |
|---|---|---|---|---|---|---|---|---|---|
| UC-BIL-A01 | See the current plan and the plan catalog | A | Workspace Owner (`ViewBilling` on the workspace; a custom role may add it; owner-approved tightening 2026-10-31, was Owner and Admin; see UC-BIL-J05) | Yes | Real | tong-quan | billing-plan | HARDENING | Catalog is a code constant (`BillingPlanCatalog`), not the Plan aggregate |
| UC-BIL-A02 | Compare plans side by side | A | Billing managers (Owner, delegated manager) | No | Mock | so-sanh-goi | No | IMPLEMENTING | Mock-only (`backendCapabilities.billingPlans` equals the mock flag). Backend must serve plans with price rows |
| UC-BIL-A03 | Maintain the plan catalog by revision | A | Platform operator | Yes | Real | No | No | INTEGRATING | No real spec publishes a revision; no design artboard; prices are 0 VND placeholders |
| UC-BIL-A04 | Keep price history readable after a price change | A | System; read by billing managers on invoices | Dom | No | No | No | DEFINED | No price source exists |
| UC-BIL-A05 | Ask sales about Enterprise | A | Billing managers | No | Mock | so-sanh-goi | No | IMPLEMENTING | No endpoint `POST .../billing/contact-sales` |
| UC-BIL-B01 | Give every account a deterministic default subscription | B | System | Part | No | No | No | DEFINED | Bootstrap is only a read-time fallback |
| UC-BIL-B02 | See subscription status, period and next change | B | Billing managers | No | Mock | tong-quan, chi-xem | No | IMPLEMENTING | Mock-only |
| UC-BIL-B03 | Start a trial | B | Account owner or billing manager | Dom | No | No | No | DEFINED | No trial rule exists |
| UC-BIL-B04 | End a trial by converting or expiring | B | System (lifecycle worker) | No | No | No | No | DEFINED | No worker |
| UC-BIL-B05 | Renew a subscription at period end | B | System | Dom | No | No | No | DEFINED | No renewal worker |
| UC-BIL-B06 | Expire a subscription | B | System | Dom | No | No | No | DEFINED | No trigger |
| UC-BIL-C01 | Refuse an invalid subscription transition | C | System | Dom | No | No | No | IMPLEMENTING | Transition table is not closed or tested for Trialing, PastDue, Unpaid, Incomplete |
| UC-BIL-C02 | Apply a scheduled change at the period boundary | C | System | No | No | No | No | DEFINED | No worker |
| UC-BIL-D01 | Derive entitlements from a subscription | D | System | Dom | No | No | No | DEFINED | No derivation |
| UC-BIL-D02 | Grant a manual entitlement override | D | Platform operator | Yes | Real | No | No | INTEGRATING | Account scope only (workspace scope not built); no real spec; no design artboard |
| UC-BIL-D03 | Revoke or let a manual override expire | D | Same operator as UC-BIL-D02 | Yes | Real | No | No | INTEGRATING | Only Manual overrides are revoked from the console; no real spec |
| UC-BIL-E01 | Resolve a capability decision for a workspace | E | Any product context through `IBillingCapabilityFacts` (server only) | Yes | No | No | feature-availability | HARDENING | Seat and storage usage is computed per call (up to three reads) |
| UC-BIL-E02 | See what billing grants, with limit and use | E | Workspace Owner and Admin | Yes | Real | tong-quan | feature-availability, billing-plan | HARDENING | Four capabilities read one by one (bounded) |
| UC-BIL-E03 | Fail closed when the decision cannot be made | E | System | Part | No | No | No | DEFINED | No fault-injection test |
| UC-BIL-E04 | Give a stable reason for every denial | E | System; consumers and UI | No | No | No | No | DEFINED | Fact record is `IsAvailable/Limit/Used/Remaining` only |
| UC-BIL-F01 | Admit a member or guest seat when an invitation is accepted | F | Invited person (server-side gate in Workspaces) | Yes | Mock | het-cho | No | INTEGRATING | Gate injected as optional (`= null`), fail open if unregistered |
| UC-BIL-F02 | Require an active subscription for a protected request | F | System (access-facts provider) | Yes | No | No | No | IMPLEMENTING | No product request uses the marker today |
| UC-BIL-F03 | Stop an upload that exceeds storage | F | Member uploading a file (server-side) | Part | No | No | No | DEFINED | Storage is displayed but never enforced |
| UC-BIL-F04 | Count seats the same way on every way of joining | F | System | No | No | No | No | DEFINED | Direct-add and other join paths are unchecked (only invitation acceptance calls the gate) |
| UC-BIL-G01 | Ingest a metered usage fact once | G | Producer context (Automation executions, others) through a Billing port | Dom | No | No | No | DEFINED | Metering is not enabled |
| UC-BIL-G02 | Correct usage with compensating evidence | G | System or billing operator (principal as in UC-BIL-D02) | No | No | No | No | DEFINED | Depends on UC-BIL-G01 |
| UC-BIL-H01 | Read current-period usage | H | Billing managers | No | Mock | tong-quan | No | DEFINED | Backend has no metered periods |
| UC-BIL-H02 | Close a usage period and start the next | H | System | Dom | No | No | No | DEFINED | Late-usage rule is open: decision in Part 4 |
| UC-BIL-H03 | Rebuild a usage aggregate from the ledger | H | System repair path | No | No | No | No | DEFINED | Needs the repair principal (see UC-BIL-D02) |
| UC-BIL-H04 | See usage split across the account's workspaces | H | Billing managers | Part | Mock | tong-quan | No | IMPLEMENTING | No per-workspace split or run counts from the backend |
| UC-BIL-I01 | Take one capacity slot for a new quota-bearing resource | I | Producer context (Automation rule creation) | Yes | No | No | No | INTEGRATING | No real-database concurrency run in this catalog's evidence |
| UC-BIL-I02 | Give a capacity slot back when the resource is deleted | I | Producer context (Automation rule deletion) | Yes | No | No | No | INTEGRATING | No real e2e |
| UC-BIL-I03 | Keep committed usage when a limit shrinks | I | System | Yes | Mock | ha-goi | billing-plan | INTEGRATING | No e2e of an over-limit downgrade |
| UC-BIL-I04 | Repair capacity from the ledger | I | System repair path | No | No | No | No | DEFINED | No repair command |
| UC-BIL-I05 | Show over-limit resources before a downgrade | I | Billing managers | No | Mock | ha-goi | No | IMPLEMENTING | Mock-only. Same use case as UC-BIL-R03 |
| UC-BIL-J01 | Let workspace Owner and Admin view billing | J | Workspace Owner, Admin | Yes | Real | No | billing-plan | HARDENING | Interim permission |
| UC-BIL-J02 | Delegate billing management to a member | J | Account owner | No | Mock | tong-quan | No | IMPLEMENTING | No Governance action for billing administration |
| UC-BIL-J03 | Show a view-only billing page to people who cannot manage | J | Workspace people without billing management | No | Mock | chi-xem | No | IMPLEMENTING | Real backend only offers `canChangePlan` |
| UC-BIL-J04 | Keep invoice profile: company, tax id, address, email (enterprise baseline) | J | Billing managers | Dom | Mock | No | No | DEFINED | No edit flow, no artboard state |
| UC-BIL-J05 | Authorize billing with its own permission | J | Governance owner decides; Billing consumes | Part | No | No | No | DEFINED | Owner of Governance permission list must add the actions |
| UC-BIL-K01 | Add a payment method through the provider's hosted form | K | Billing managers | Dom | No | No | No | DEFINED | Provider not chosen |
| UC-BIL-K02 | See the card or bank transfer on file | K | Billing managers | No | Mock | tong-quan, qua-han | No | IMPLEMENTING | Mock-only |
| UC-BIL-K03 | Replace the default payment method | K | Billing managers | Dom | No | No | No | DEFINED | No handler |
| UC-BIL-K04 | Remove a payment method | K | Billing managers | Dom | No | No | No | DEFINED | No handler |
| UC-BIL-K05 | Warn before a card expires or when it is declined (enterprise baseline) | K | Billing managers (notice) | No | No | No | No | DEFINED | Needs Collaboration notification contract |
| UC-BIL-L01 | List invoices | L | Billing managers | No | Mock | hoa-don | No | IMPLEMENTING | Mock-only |
| UC-BIL-L02 | Open an invoice | L | Billing managers | No | Mock | hoa-don | No | IMPLEMENTING | Mock-only |
| UC-BIL-L03 | Download an invoice PDF | L | Billing managers | No | Mock | hoa-don | No | IMPLEMENTING | Mock-only |
| UC-BIL-L04 | Issue an invoice for a charge | L | System | Dom | No | No | No | DEFINED | No handler |
| UC-BIL-L05 | Void or credit an invoice | L | Billing operator | Dom | No | No | No | BLOCKED | Same principal decision as UC-BIL-D02 |
| UC-BIL-L06 | Show VAT and buyer tax data on invoices (enterprise baseline) | L | System | No | Mock | hoa-don | No | DEFINED | No tax model |
| UC-BIL-M01 | Pay an open invoice | M | Billing managers | No | Mock | qua-han | No | IMPLEMENTING | Mock-only |
| UC-BIL-M02 | Record payment settlement from the provider | M | System (webhook worker) | Dom | No | No | No | DEFINED | No provider |
| UC-BIL-M03 | Record a failed payment | M | System | Dom | No | qua-han | No | DEFINED | No provider |
| UC-BIL-N01 | Create the provider customer for an account | N | System | No | No | No | No | DEFINED | Provider not chosen (owner decision 46: no payment provider yet) |
| UC-BIL-N02 | Start a checkout or subscription change | N | Billing managers | No | No | No | No | DEFINED | Provider not chosen (owner decision 46: no payment provider yet) |
| UC-BIL-N03 | Open the provider's billing portal | N | Billing managers | No | No | No | No | DEFINED | Provider not chosen (owner decision 46: no payment provider yet) |
| UC-BIL-N04 | Read the state of a billing operation | N | Billing managers | No | No | No | No | DEFINED | Provider not chosen (owner decision 46: no payment provider yet) |
| UC-BIL-O01 | Verify a provider webhook and store the receipt | O | System (public endpoint, not browser-authorized) | No | No | No | No | DEFINED | Provider not chosen (owner decision 46: no payment provider yet) |
| UC-BIL-P01 | Make duplicate webhook delivery harmless | P | System | No | No | No | No | DEFINED | Provider not chosen (owner decision 46: no payment provider yet) |
| UC-BIL-P02 | Ignore unknown provider events safely | P | System | No | No | No | No | DEFINED | Provider not chosen (owner decision 46: no payment provider yet) |
| UC-BIL-P03 | Apply out-of-order events correctly | P | System | No | No | No | No | DEFINED | Provider not chosen (owner decision 46: no payment provider yet) |
| UC-BIL-Q01 | Reconcile a subscription with the provider | Q | System; scoped repair by privileged operator | No | No | No | No | DEFINED | Provider not chosen (owner decision 46: no payment provider yet) |
| UC-BIL-Q02 | Resolve an operation with unknown outcome | Q | System | No | No | No | No | DEFINED | Provider not chosen (owner decision 46: no payment provider yet) |
| UC-BIL-Q03 | Check a seat purchase that is still waiting for the bank | Q | Billing managers | No | Mock | dang-xu-ly | No | IMPLEMENTING | Mock-only |
| UC-BIL-R01 | Move an account to another plan (platform operator) | R | Platform operator | Yes | Real | so-sanh-goi | billing-plan | HARDENING | First plan change can race; design not diffed |
| UC-BIL-R02 | Upgrade to a paid plan with payment | R | Billing managers | No | No | so-sanh-goi | No | DEFINED | Needs a provider (decision 46) and a price source |
| UC-BIL-R03 | Preview a downgrade | R | Billing managers | No | Mock | ha-goi | No | IMPLEMENTING | Mock-only |
| UC-BIL-R04 | Schedule a downgrade for the period end | R | Billing managers | No | Mock | ha-goi | No | IMPLEMENTING | Mock-only |
| UC-BIL-R05 | Cancel a scheduled downgrade | R | Billing managers | No | Mock | tong-quan | No | IMPLEMENTING | Mock-only |
| UC-BIL-R06 | Schedule cancellation at the period end | R | Billing managers | Dom | No | No | No | DEFINED | No handler or endpoint |
| UC-BIL-R07 | Resume a scheduled cancellation | R | Billing managers | No | No | No | No | DEFINED | No domain method to clear the flag |
| UC-BIL-R08 | Apply cancellation at the period end | R | System | Dom | No | No | No | DEFINED | No worker |
| UC-BIL-R09 | Terminate a subscription immediately | R | Platform operator | Yes | Real | No | No | INTEGRATING | No real spec; no design artboard |
| UC-BIL-R10 | Get a quote for extra seats | R | Billing managers | No | Mock | mua-cho | No | IMPLEMENTING | Mock-only |
| UC-BIL-R11 | Buy extra seats | R | Billing managers | No | Mock | mua-cho, dang-xu-ly | No | IMPLEMENTING | Mock-only |
| UC-BIL-R12 | Reduce seats at renewal (enterprise baseline) | R | Billing managers | No | No | No | No | DEFINED | No flow or artboard |
| UC-BIL-S01 | Mark a subscription past due after a failed payment | S | System | Dom | No | qua-han | No | DEFINED | No trigger |
| UC-BIL-S02 | Block only new members while overdue | S | System | No | Mock | qua-han | No | DEFINED | Gate does not read PastDue |
| UC-BIL-S03 | Recover from past due when payment settles | S | System | No | No | No | No | DEFINED | Domain lacks the transition |
| UC-BIL-S04 | See the past-due banner and retry payment | S | Billing managers | No | Mock | qua-han | No | IMPLEMENTING | Mock-only |
| UC-BIL-S05 | Send dunning notices (enterprise baseline) | S | System; recipients are billing managers | No | No | No | No | DEFINED | Needs notification contract with Collaboration |
| UC-BIL-S06 | Remind before a trial ends (enterprise baseline) | S | System; recipients are billing managers | No | No | No | No | DEFINED | No trial yet (UC-BIL-B03) |
| UC-BIL-T01 | Open Plans and billing with every state | T | Workspace Owner, Admin | Yes | Real | tong-quan | feature-availability | HARDENING | Two pages behind `backendCapabilities.billingPlans` |
| UC-BIL-T02 | Stop an invitation at the seat limit and offer more seats | T | Workspace Admin inviting people | No | Mock | het-cho | No | IMPLEMENTING | Mock-only seat summary |
| UC-BIL-T03 | Tell not entitled from not authorized | T | Anyone using a gated feature | No | Mock | het-cho | No | IMPLEMENTING | No stable reason codes from the backend |
| UC-BIL-T04 | Say plainly what is not available yet | T | Workspace Owner, Admin | Yes | Real | No | feature-availability | HARDENING | Remove when UC-BIL-L01 is real |
| UC-BIL-U01 | Publish canonical commercial facts | U | System | Dom | No | No | No | IMPLEMENTING | No consumer |
| UC-BIL-U02 | Propagate an entitlement change to consumers | U | System | No | No | No | No | DEFINED | No cache exists |
| UC-BIL-V01 | Trace one billing operation end to end | V | Engineers on call | No | No | No | No | DEFINED | No operation model yet |
| UC-BIL-V02 | Alert on reconciliation lag and entitlement errors | V | Engineers on call | No | No | No | No | DEFINED | No signals defined |
| UC-BIL-W01 | Backfill subscriptions, entitlements and lineage | W | System (migration) | No | No | No | No | DEFINED | `Account.PlanCode` migration not planned in code |
| UC-BIL-W02 | Replace the hard-coded frontend catalog with the API | W | Frontend team | Part | Real | No | No | IMPLEMENTING | The tenant plan card and the operator console read the catalog from the API; the dead package stub (`features-billing`, prices that match nothing) is still there |
| UC-BIL-X01 | Keep one account's billing data from another | X | System | Yes | No | No | No | IMPLEMENTING | No cross-tenant test per billing endpoint |
| UC-BIL-X02 | Never touch card data or secrets | X | System | Dom | No | No | No | DEFINED | No redaction test |
| UC-BIL-X03 | Make commercial commands idempotent and rate-limited | X | System | Part | Mock | No | No | IMPLEMENTING | No key on `PUT .../billing/plan` |
| UC-BIL-X04 | Audit every billing change | X | Auditors and billing managers | No | No | No | No | DEFINED | Governance audit contract not used by Billing |
| UC-BIL-X05 | Coordinate Billing when an account closes | X | System (Accounts owns closure) | No | No | No | No | DEFINED | No consumer of the closure fact |
| UC-BIL-X06 | Keep commercial evidence after product or account deletion | X | System | No | No | No | No | DEFINED | Retention period not decided (Part 4) |

# 3. Use cases

## A. BIL-001 Plan/catalog model

### UC-BIL-A01 See the current plan and the plan catalog
- Capability: A. BIL-001 Plan/catalog model
- Actors and permission: Workspace Owner (`ViewBilling` on the workspace; a custom role may add it; owner-approved tightening 2026-10-31, was Owner and Admin; see UC-BIL-J05).
- Preconditions: Caller is signed in and holds the workspace. The workspace belongs to an account.
- Input: Workspace id from the route.
- Output: Current plan code and name, its limits, the catalog (free, pro, enterprise) and `canChangePlan`.
- Main flow: 1. Pipeline authorizes the caller on the workspace. 2. Handler resolves the account's newest active, in-period subscription; with none, plan is Free. 3. Handler returns the plan, the catalog and whether the caller may change the plan (account owner and environment allows).
- No-op / rejection / concurrency / retry: Caller without the permission or without the workspace gets the identical NotFound from the pipeline. Read only: no version, no event, no retry concern.
- UI states: Loading, success, permission denied (page is hidden), error.
- Rules: Product §5 BIL-001, §6, §7, §8 BIL-002; spec BILREQ001, BILREQ008.
- Acceptance: Given an account with no subscription, when an Admin opens billing, then the plan is Free and the catalog lists three plans. Given a caller outside the workspace, when they call the endpoint, then they get NotFound.
- Evidence: BE `GET /api/v1/workspaces/{workspaceId}/billing/plan` (Billing.Plan.Get, `GetWorkspaceBillingPlanQueryHandler`) · FE `frontend/apps/web/src/routes/workspaces/$workspaceId/billing.tsx`, `billing/billing-capabilities-page.tsx`, `billing/billing-api.ts` (`plan`) · Design `Final-Billing.dc.html` `tong-quan` · Tests `backend/tests/Notrelix.Application.Tests/Features/Billing/``BillingPlanTests.cs` · Real e2e `frontend/e2e/real/billing-plan.real.e2e.spec.ts`
- State: HARDENING — Catalog is a code constant (`BillingPlanCatalog`), not the Plan aggregate; three plans, the artboard shows four tiers; no cross-tenant API negative test; no owner check; design not diffed.

### UC-BIL-A02 Compare plans side by side
- Capability: A. BIL-001 Plan/catalog model
- Actors and permission: Billing managers (Owner, delegated manager).
- Preconditions: Account has a current plan.
- Input: None.
- Output: A table of tiers with price, people, storage, runs, reports, webhooks, audit days, SSO, Power BI, plus the current tier and pending downgrade.
- Main flow: 1. Manager opens `so-sanh-goi`. 2. UI loads the plans read model. 3. UI marks the current plan and offers the next action per tier (buy seats, downgrade preview, contact sales).
- No-op / rejection / concurrency / retry: Non-manager sees the view-only state (UC-BIL-J03). Read only.
- UI states: Loading, success, error, read-only.
- Rules: Product §100; spec BILREQ008, BILREQ095, BILREQ111.
- Acceptance: Given a manager, when they open the comparison, then each tier shows price and limits from the catalog read model and the current tier is marked.
- Evidence: BE none (route `GET .../billing/plans` is not built) · FE `frontend/packages/features/workspace/src/web/billing/``billing-compare-surface.tsx`, `billing/billing-api.ts` (`plans`) · Design `Final-Billing.dc.html` `so-sanh-goi` · Tests `frontend/packages/features/workspace/src/web/billing/``__tests__/billing.interaction.component.test.tsx` · Real e2e none (mock `frontend/e2e/mock/billing.mock.e2e.spec.ts`)
- State: IMPLEMENTING — Mock-only (`backendCapabilities.billingPlans` equals the mock flag). Backend must serve plans with price rows; amounts in the mock are whole VND.

### UC-BIL-A03 Maintain the plan catalog by revision
- Capability: A. BIL-001 Plan/catalog model
- Actors and permission: the platform operator (owner decision 2026-10-31).
- Preconditions: Plan family exists or is being created.
- Input: Plan family key, revision, display name, FeatureCode limits with unit and scope, status.
- Output: A new immutable plan revision; an older revision deprecated or archived. Existing subscriptions keep their revision.
- Main flow: 1. Operator creates a revision. 2. Billing validates limit units and stable FeatureCodes. 3. Operator deprecates or archives an old revision; sales stop, subscriptions continue.
- No-op / rejection / concurrency / retry: Referenced plan history is never deleted. Re-submitting the same revision is a no-op. Duplicate FeatureCode on one plan is rejected.
- UI states: Operator console, plan catalog screen (table of revisions, publish dialog, deprecate and archive with a reason). Not a customer screen.
- Rules: Product §9 plan lifecycle, §10 BIL-003, §13, §14 BIL-005, §15, §16 BIL-006, §17; spec BILREQ001-003, BILREQ006-007, BILREQ132.
- Acceptance: Given an archived revision, when a subscription still references it, then entitlements still resolve. Given two limits for one FeatureCode, when saved, then it is rejected.
- Evidence: BE `POST /operator/v1/billing/plans` (`Operator.Billing.Plans.CreateRevision`), `.../{planId}/deprecate`, `.../archive`, `GET /operator/v1/billing/plans`; `Plan.Create(familyKey, revision, ...)`, `billing.plans.family_key/revision/tier` unique per family, baseline `free`, `pro`, `enterprise` inserted only when absent (`PlanCatalogSeed`) · FE `frontend/apps/operator` plan catalog screen · Design none · Tests Domain `PlanRevisionTests`, Application `OperatorBillingServiceTests` (21: same content is idempotent, explicit next revision, unknown FeatureCode, last active `free` protected, stale version) and `BillingPlanTests` (12), Integration `OperatorConsoleIntegrationTests` (29) · Real e2e none
- State: INTEGRATING — Backend and operator screen connect and are tested against a real Postgres; no real spec publishes a revision; no design artboard; the baseline prices are 0 VND placeholders; units and scope of limits beyond the five FeatureCodes are not modelled.

### UC-BIL-A04 Keep price history readable after a price change
- Capability: A. BIL-001 Plan/catalog model
- Actors and permission: System; read by billing managers on invoices.
- Preconditions: A plan has price rows.
- Input: Plan price version, currency, period, effective date.
- Output: Prices are versioned; invoices and past periods show the price that applied then.
- Main flow: 1. A price change creates a new price version. 2. New subscriptions use it; existing ones are grandfathered until a change. 3. Invoice lines copy product, price, quantity and period.
- No-op / rejection / concurrency / retry: No silent edit of a used price. Replay of the same change is a no-op.
- UI states: Shown only through invoices (UC-BIL-L02).
- Rules: Product §11, §12 BIL-004; spec BILREQ004, BILREQ005, BILREQ112.
- Acceptance: Given a price change, when an old invoice is opened, then it still shows the old unit price.
- Evidence: BE none (`PlanPrice` entity only) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No price source exists; the artboard price (whole VND, yearly) has no backend.

### UC-BIL-A05 Ask sales about Enterprise
- Capability: A. BIL-001 Plan/catalog model
- Actors and permission: Billing managers.
- Preconditions: Enterprise is contract-priced.
- Input: None; the request carries the caller and account.
- Output: A stored contact-sales request with time.
- Main flow: 1. Manager chooses Contact sales on the Enterprise tier. 2. Client sends an idempotent request. 3. Billing stores the request; the UI confirms.
- No-op / rejection / concurrency / retry: A retry with the same idempotency key stores one request.
- UI states: Loading, success, error, pending.
- Rules: Product decision (owner, 2026-10-26) 46; product §100.
- Acceptance: Given a manager, when they contact sales twice with one key, then one request is stored.
- Evidence: BE none · FE `billing/billing-api.ts` (`contactSales`), `frontend/packages/features/workspace/src/web/billing/``billing-compare-surface.tsx` · Design `Final-Billing.dc.html` `so-sanh-goi` · Tests mock handler `frontend/packages/dev/mock-backend/src/contexts/billing/billing.handlers.ts` · Real e2e none
- State: IMPLEMENTING — No endpoint `POST .../billing/contact-sales`; no stored request; no delivery to a sales mailbox.

## B. BIL-002 Subscription lifecycle

### UC-BIL-B01 Give every account a deterministic default subscription
- Capability: B. BIL-002 Subscription lifecycle
- Actors and permission: System.
- Preconditions: Account exists and is active.
- Input: Account id.
- Output: An active Free subscription row, created once.
- Main flow: 1. On account creation or first billing read, Billing creates the Free subscription if none exists. 2. Re-running creates nothing.
- No-op / rejection / concurrency / retry: Idempotent by account id. No provider needed for Free.
- UI states: None.
- Rules: Product §21, §105; spec BILREQ009-011.
- Acceptance: Given a new account, when bootstrap runs twice, then one Free subscription exists.
- Evidence: BE partial: `BillingPlanResolver` falls back to Free when no row exists; nothing creates the row · FE none · Design none · Tests `backend/tests/Notrelix.Application.Tests/Features/Billing/``BillingPlanTests.cs` (`An_account_without_a_subscription_is_on_the_free_plan`) · Real e2e none
- State: DEFINED — Bootstrap is only a read-time fallback; no row, no event, no lineage for Free accounts.

### UC-BIL-B02 See subscription status, period and next change
- Capability: B. BIL-002 Subscription lifecycle
- Actors and permission: Billing managers.
- Preconditions: Account has a subscription.
- Input: Workspace id.
- Output: Plan tier, status (active or past due), period start and end, auto-renew date, unit price, VAT, pending downgrade, managers, payment, usage split.
- Main flow: 1. Manager opens the overview (`tong-quan`). 2. UI loads one overview read model. 3. UI renders plan, usage meters, payment and recent invoices.
- No-op / rejection / concurrency / retry: Non-manager sees the view-only state. Read only.
- UI states: Loading, success, error, read-only, pending purchase, past due.
- Rules: Product §21, §25, §100; spec BILREQ052, BILREQ095-097.
- Acceptance: Given a manager, when they open billing, then plan, period, usage and payment are shown without a second call per section.
- Evidence: BE none (`GET .../billing` not built) · FE `frontend/apps/web/src/routes/workspaces/$workspaceId/billing.tsx` (`BillingPlansPage`), `frontend/packages/features/workspace/src/web/billing/``billing-overview-surface.tsx`, `billing/billing-api.ts` (`overview`) · Design `Final-Billing.dc.html` `tong-quan`, `chi-xem` · Tests `frontend/packages/features/workspace/src/web/billing/``__tests__/billing.interaction.component.test.tsx` · Real e2e none (mock `billing.mock.e2e.spec.ts`)
- State: IMPLEMENTING — Mock-only; backend returns plan and capabilities only. Query keys are workspace-scoped, product requires account scope (BILREQ097).

### UC-BIL-B03 Start a trial
- Capability: B. BIL-002 Subscription lifecycle
- Actors and permission: Account owner or billing manager.
- Preconditions: Account has not used a trial of this plan (repeat-trial policy, Part 5).
- Input: Plan, trial length.
- Output: Subscription in Trialing with trial end and trial-source entitlements.
- Main flow: 1. Manager starts a trial. 2. Billing creates a Trialing subscription (no forced Active). 3. Entitlements derive with source Trial.
- No-op / rejection / concurrency / retry: Second trial of the same plan is rejected. Same operation id twice yields one subscription.
- UI states: Loading, success, validation error, pending.
- Rules: Product §28; spec BILREQ013, BILREQ014.
- Acceptance: Given an eligible account, when it starts a trial, then status is Trialing and the end date is shown.
- Evidence: BE none (`SubscriptionStatus.Trialing` exists; no start path) · FE none · Design `Final-Billing.dc.html` none · Tests none · Real e2e none
- State: DEFINED — No trial rule exists; decision recorded in Part 4.

### UC-BIL-B04 End a trial by converting or expiring
- Capability: B. BIL-002 Subscription lifecycle
- Actors and permission: System (lifecycle worker).
- Preconditions: Subscription is Trialing and the trial end passed.
- Input: Subscription id, time.
- Output: Active (payment method present) or back to Free; reminder sent before.
- Main flow: 1. Worker finds ended trials. 2. With a payment method it activates; otherwise it moves to Free. 3. Entitlements re-derive.
- No-op / rejection / concurrency / retry: Re-run is a no-op. Never deletes product data.
- UI states: Banner for trial end; success; read-only after expiry.
- Rules: Product §28, §30 BIL-011; spec BILREQ018, BILREQ021.
- Acceptance: Given a trial past its end without a payment method, when the worker runs twice, then the account is on Free once.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No worker; no scheduler in Billing.

### UC-BIL-B05 Renew a subscription at period end
- Capability: B. BIL-002 Subscription lifecycle
- Actors and permission: System.
- Preconditions: Subscription is Active and its period ended.
- Input: Subscription id.
- Output: New period, an invoice, a renewal fact.
- Main flow: 1. Worker renews. 2. Billing starts the next period. 3. Billing issues the invoice (UC-BIL-L04).
- No-op / rejection / concurrency / retry: Duplicate run renews once (operation identity).
- UI states: Success; pending while payment settles.
- Rules: Product §25; spec BILREQ018, BILREQ105.
- Acceptance: Given an active subscription at period end, when renewal runs twice, then one new period exists.
- Evidence: BE partial: `Subscription.Renew` in `backend/src/Notrelix.Domain/Billing/``Subscriptions/Subscription.cs`; used by plan change to refresh a lapsed term · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No renewal worker; the dev plan change gives a 3650-day term.

### UC-BIL-B06 Expire a subscription
- Capability: B. BIL-002 Subscription lifecycle
- Actors and permission: System.
- Preconditions: Subscription ended and cannot renew.
- Input: Subscription id.
- Output: Status Expired; account falls to Free limits.
- Main flow: 1. Worker marks it expired. 2. Entitlements re-derive. 3. Product data stays.
- No-op / rejection / concurrency / retry: Re-run is a no-op.
- UI states: Read-only banner when over limit.
- Rules: Product §22 BIL-008, §30; spec BILREQ013.
- Acceptance: Given an expired subscription, when the plan resolver runs, then limits are Free's and no data is removed.
- Evidence: BE partial: `Subscription.Expire` · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No trigger.

## C. BIL-003 Subscription transition policy

### UC-BIL-C01 Refuse an invalid subscription transition
- Capability: C. BIL-003 Subscription transition policy
- Actors and permission: System.
- Preconditions: A change is requested on a subscription.
- Input: Target state or plan.
- Output: Either the allowed transition or a stable rule error; nothing else changes.
- Main flow: 1. Domain checks status. 2. Canceled and Expired cannot change plan. 3. A repeat of the current plan is a no-op.
- No-op / rejection / concurrency / retry: No-op bumps no version and raises no event. Rule code `Billing_Subscription_CannotChangePlanOfInactive`.
- UI states: Error shown as a plain message.
- Rules: Product §22 BIL-008; spec BILREQ013, BILREQ015.
- Acceptance: Given a Canceled subscription, when a plan change is attempted, then it is rejected and the row is unchanged.
- Evidence: BE `backend/src/Notrelix.Domain/Billing/``Subscriptions/Subscription.cs`, `Rules/SubscriptionRules.cs` · FE none · Design none · Tests `backend/tests/Notrelix.Application.Tests/Features/Billing/``BillingPlanTests.cs` · Real e2e none
- State: IMPLEMENTING — Transition table is not closed or tested for Trialing, PastDue, Unpaid, Incomplete.

### UC-BIL-C02 Apply a scheduled change at the period boundary
- Capability: C. BIL-003 Subscription transition policy
- Actors and permission: System.
- Preconditions: A downgrade or cancellation is scheduled.
- Input: Subscription id, effective time.
- Output: The plan or status changes only at the boundary; entitlements follow.
- Main flow: 1. Worker reads due changes. 2. Billing applies each once. 3. Facts are published.
- No-op / rejection / concurrency / retry: Duplicate run applies once. Upgrade timing is immediate, downgrade at the boundary.
- UI states: Pending change shown until applied.
- Rules: Product §31, §32, §33; spec BILREQ015, BILREQ022, BILREQ023.
- Acceptance: Given a downgrade scheduled for 2027-01-01, when the worker runs after that time twice, then the plan is lower once.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No worker.

## D. BIL-004 Entitlement model

### UC-BIL-D01 Derive entitlements from a subscription
- Capability: D. BIL-004 Entitlement model
- Actors and permission: System.
- Preconditions: Subscription or plan revision changed.
- Input: Account id, plan revision.
- Output: Entitlement rows with source Subscription, scope Account, lineage; stale ones expired.
- Main flow: 1. Billing reconciles on each change. 2. Rows are created, changed or expired. 3. History stays.
- No-op / rejection / concurrency / retry: Concurrent reconcile is safe; same input yields the same rows.
- UI states: None directly.
- Rules: Product §36, §37 BIL-013, §40; spec BILREQ024, BILREQ026-028, BILREQ106.
- Acceptance: Given a plan change, when reconcile runs twice, then entitlements match the new plan once.
- Evidence: BE none: limits are read straight from `BillingPlanCatalog`; `Entitlement` entity exists · FE none · Design none · Tests `backend/tests/Notrelix.Application.Tests/Features/Billing/``BillingCapabilityFactsProviderTests.cs` (explicit rows only) · Real e2e none
- State: DEFINED — No derivation; no lineage; only manual-style rows are read.

### UC-BIL-D02 Grant a manual entitlement override
- Capability: D. BIL-004 Entitlement model
- Actors and permission: the platform operator (owner decision 2026-10-31); support access to tenant data is a later, consent-based step and is not part of this.
- Preconditions: Account exists.
- Input: Account scope (workspace scope is not built), FeatureCode, limit or unlimited, reason, optional expiry.
- Output: An Entitlement with source Manual and an audit entry.
- Main flow: 1. Operator submits. 2. Billing validates. 3. Row is created and audited.
- No-op / rejection / concurrency / retry: a reason is required; an unknown FeatureCode is a validation error; an active override for the same feature is replaced and the change audited; no reason or an unknown account writes nothing.
- UI states: Operator console, tenant detail (overrides table and grant form).
- Rules: Product §40, §41; spec BILREQ029, BILREQ030, BILREQ103.
- Acceptance: Given an operator, when they grant an override, then it wins by precedence and is audited.
- Evidence: BE `POST /operator/v1/accounts/{accountId}/billing/overrides` (`Operator.Billing.GrantOverride`), `BillingCapabilityFactsProvider` (an active entitlement is the effective limit) · FE `frontend/apps/operator` tenant detail · Design none · Tests Application `OperatorBillingServiceTests` (21), Integration `OperatorConsoleIntegrationTests` (29: the tenant sees the overridden limit, audit row with before and after) · Real e2e none
- State: INTEGRATING — Backend and operator screen connect and are tested; account scope only; no real spec; no design artboard.

### UC-BIL-D03 Revoke or let a manual override expire
- Capability: D. BIL-004 Entitlement model
- Actors and permission: the platform operator, as UC-BIL-D02.
- Preconditions: An override exists.
- Input: Entitlement id, reason.
- Output: Status Revoked or Expired; decisions fall back to the plan.
- Main flow: 1. Operator revokes, or time passes. 2. Resolution ignores it. 3. Fact is published.
- No-op / rejection / concurrency / retry: only Manual overrides are revoked from the console (a subscription-sourced entitlement is refused); a reason and the expected version are required; a second revoke is a no-op that writes no audit row.
- UI states: Operator console, tenant detail (revoke with a reason).
- Rules: Product §43; spec BILREQ030, BILREQ031.
- Acceptance: Given an expired override, when a decision is made, then the plan limit applies.
- Evidence: BE `POST /operator/v1/billing/overrides/{entitlementId}/revoke` (`Operator.Billing.RevokeOverride`), `Entitlement.Revoke`, `MarkExpired`, `IsActiveAt` and the expiry filter in `BillingCapabilityFactsProvider` · FE `frontend/apps/operator` tenant detail · Design none · Tests Application `OperatorBillingServiceTests` (21), `BillingCapabilityFactsProviderTests`, Integration `OperatorConsoleIntegrationTests` (29) · Real e2e none
- State: INTEGRATING — Backend and operator screen connect and are tested; expiry is evaluated at read time (no sweep marks the row Expired); no real spec.

## E. BIL-005 Entitlement evaluation

### UC-BIL-E01 Resolve a capability decision for a workspace
- Capability: E. BIL-005 Entitlement evaluation
- Actors and permission: Any product context through `IBillingCapabilityFacts` (server only).
- Preconditions: Account and workspace known.
- Input: Account id, workspace id, capability code, requested amount.
- Output: Available or not, limit, used, remaining.
- Main flow: 1. Billing picks the winning active, unexpired entitlement (workspace scope before account, newest first). 2. With none, the plan limit applies. 3. Explicit unlimited differs from zero.
- No-op / rejection / concurrency / retry: Zero limit means unavailable; unknown capability means unavailable. Read only.
- UI states: None (consumers show the result).
- Rules: Product §37 BIL-013, §41, §42 BIL-015; spec BILREQ024, BILREQ029, BILREQ032.
- Acceptance: Given a workspace-scope entitlement and an account-scope one, when resolved, then the workspace one wins. Given limit 0, then not available.
- Evidence: BE `Features/Billing/Entitlements/Services/BillingCapabilityFactsProvider.cs` · FE none · Design none · Tests `backend/tests/Notrelix.Application.Tests/Features/Billing/``BillingCapabilityFactsProviderTests.cs` · Real e2e `frontend/e2e/real/feature-availability.real.e2e.spec.ts` (through UC-BIL-E02)
- State: HARDENING — Seat and storage usage is computed per call (up to three reads); no entitlement version; no cache; no cross-tenant test at the fact seam.

### UC-BIL-E02 See what billing grants, with limit and use
- Capability: E. BIL-005 Entitlement evaluation
- Actors and permission: Workspace Owner and Admin.
- Preconditions: Caller holds the workspace.
- Input: Workspace id.
- Output: One row each for automation rules, member seats, guest seats, storage: granted, unlimited, limit, used, remaining.
- Main flow: 1. Pipeline authorizes. 2. Handler asks the facts seam per capability. 3. Storage is shown to two decimals.
- No-op / rejection / concurrency / retry: Same NotFound for outsiders. Read only.
- UI states: Loading, success, error, permission denied, meter at limit.
- Rules: Product §100; spec BILREQ052, BILREQ099.
- Acceptance: Given Pro, when an Admin opens billing, then automation shows `N of 100` and seats `N of 500`.
- Evidence: BE `GET /api/v1/workspaces/{workspaceId}/billing/capabilities` (Billing.Capabilities.List, `GetWorkspaceBillingCapabilitiesQueryHandler`) · FE `billing/billing-capabilities-page.tsx`, `billing/billing-api.ts` (`capabilities`) · Design `Final-Billing.dc.html` `tong-quan` · Tests `backend/tests/Notrelix.Application.Tests/Features/Billing/``GetWorkspaceBillingCapabilitiesTests.cs`, `backend/tests/Notrelix.Integration.Tests/Rls/BillingUsagePolicyTests.cs` · Real e2e `frontend/e2e/real/feature-availability.real.e2e.spec.ts`, `billing-plan.real.e2e.spec.ts`
- State: HARDENING — Four capabilities read one by one (bounded); no cache or realtime refresh; design not diffed.

### UC-BIL-E03 Fail closed when the decision cannot be made
- Capability: E. BIL-005 Entitlement evaluation
- Actors and permission: System.
- Preconditions: Entitlement data or usage read fails.
- Input: Capability request.
- Output: Denied with a retryable error, never granted by default.
- Main flow: 1. Facts provider throws or returns nothing. 2. Consumer denies. 3. Error is logged with a correlation id.
- No-op / rejection / concurrency / retry: Missing fact is treated as not available (capacity path uses a zero ceiling).
- UI states: Error, retry.
- Rules: Product §89 BIL-030, §90; spec BILREQ032, BILREQ128.
- Acceptance: Given a failing usage read, when a seat is requested, then the request is refused, not admitted.
- Evidence: BE partial: null fact gives zero ceiling in `Capacity/BillingCapacityActions.cs`; `BillingSeatGate` treats null as no seat · FE none · Design none · Tests `backend/tests/Notrelix.Application.Tests/Features/Billing/``BillingCapacityActionsTests.cs` · Real e2e none
- State: DEFINED — No fault-injection test; `AcceptInvitation` receives the seat gate as optional, so a missing registration skips the check (fail open).

### UC-BIL-E04 Give a stable reason for every denial
- Capability: E. BIL-005 Entitlement evaluation
- Actors and permission: System; consumers and UI.
- Preconditions: A capability decision is negative.
- Input: Capability request.
- Output: A reason code: not entitled, limit reached, billing past due, unavailable.
- Main flow: 1. Facts return a reason. 2. API maps it to a stable error. 3. UI picks the right next step (UC-BIL-T03).
- No-op / rejection / concurrency / retry: Reason never leaks plan internals to people without billing rights.
- UI states: Not entitled, limit reached, permission denied.
- Rules: Product §101; spec BILREQ033, BILREQ056, BILREQ099.
- Acceptance: Given limit reached, when creation is refused, then the error code is the limit code, not a generic 403.
- Evidence: BE none (`BillingCapabilityFact` has no reason field) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Fact record is `IsAvailable/Limit/Used/Remaining` only.

## F. BIL-006 Product entitlement contract

### UC-BIL-F01 Admit a member or guest seat when an invitation is accepted
- Capability: F. BIL-006 Product entitlement contract
- Actors and permission: Invited person (server-side gate in Workspaces).
- Preconditions: Invitation is valid; workspace has a plan.
- Input: Account, workspace, user, member or guest.
- Output: Seat admitted, or refused with a seat-limit error.
- Main flow: 1. Workspaces asks `IBillingSeatGate.CanAdmitAsync`. 2. A person who already holds that kind of seat in the account is admitted. 3. Otherwise a free seat must exist.
- No-op / rejection / concurrency / retry: At the limit: refused, nothing is added. A retry of an accepted invitation does not take a second seat. Past-due blocking is UC-BIL-S02.
- UI states: Seat-limit dialog (UC-BIL-T02), error.
- Rules: Product §81, §84, §91; owner decision 46; spec BILREQ090, BILREQ035.
- Acceptance: Given Free with 10 members, when an eleventh accepts, then acceptance is refused. Given an existing member of another workspace of the account, then admitted.
- Evidence: BE `Features/Billing/Entitlements/Services/BillingSeatGate.cs`, call in `Features/Workspaces/Invitations/Services/InvitationAcceptanceService.cs` · FE `frontend/packages/features/workspace/src/web/billing/``billing-dialogs.tsx` · Design `Final-Billing.dc.html` `het-cho` · Tests `backend/tests/Notrelix.Application.Tests/Features/Billing/``BillingPlanTests.cs`, `backend/tests/Notrelix.Integration.Tests/Auth/InvitationTokenFlowHttpTests.cs` · Real e2e none
- State: INTEGRATING — Gate injected as optional (`= null`), fail open if unregistered; only this one path checks seats; no real e2e; no concurrent last-seat test.

### UC-BIL-F02 Require an active subscription for a protected request
- Capability: F. BIL-006 Product entitlement contract
- Actors and permission: System (access-facts provider).
- Preconditions: A request implements `IRequireSubscription`.
- Input: Account id, minimum tier.
- Output: Satisfied or denied inside the access policy.
- Main flow: 1. Access facts ask `IBillingSubscriptionFacts.SatisfiesRequirementAsync`. 2. Billing compares tier. 3. Policy only reads the boolean.
- No-op / rejection / concurrency / retry: Production never uses the dev bypass; `DevNull` is refused outside Development and Testing.
- UI states: Not entitled message.
- Rules: Product §37 BIL-013; spec BILREQ090; decision 108 (no plan gates in Work Management now).
- Acceptance: Given a request needing Pro, when the account is Free, then it is denied.
- Evidence: BE `Features/Billing/Subscriptions/Services/BillingSubscriptionFactsProvider.cs`, `Common/Requests/Gates/IRequireSubscription.cs`, `Infrastructure/Billing/DevNullBillingSubscriptionFacts.cs` · FE none · Design none · Tests `backend/tests/Notrelix.Application.Tests/Features/Billing/``BillingSubscriptionFactsProviderTests.cs` · Real e2e none
- State: IMPLEMENTING — No product request uses the marker today; tier comparison reads `SubscriptionTier` (BILREQ017 says tier is not an authority).

### UC-BIL-F03 Stop an upload that exceeds storage
- Capability: F. BIL-006 Product entitlement contract
- Actors and permission: Member uploading a file (server-side).
- Preconditions: Plan has a storage limit.
- Input: Workspace id, file size.
- Output: Upload refused with a storage-limit reason, or accepted.
- Main flow: 1. Documents or Work Management asks for the storage capability. 2. Billing decides from account-wide bytes. 3. Product enforces.
- No-op / rejection / concurrency / retry: Existing files are never removed on downgrade.
- UI states: Limit reached, not entitled.
- Rules: Product §83, §91; spec BILREQ051.
- Acceptance: Given 2 GB used on Free, when a file is uploaded, then it is refused.
- Evidence: BE partial: `STORAGE_GB` is read and shown; no storage producer calls the gate (grep of `backend/src` outside Billing finds none) · FE none · Design none · Tests `backend/tests/Notrelix.Application.Tests/Features/Billing/``GetWorkspaceBillingCapabilitiesTests.cs` · Real e2e none
- State: DEFINED — Storage is displayed but never enforced.

### UC-BIL-F04 Count seats the same way on every way of joining
- Capability: F. BIL-006 Product entitlement contract
- Actors and permission: System.
- Preconditions: Someone becomes a member or guest by link, direct add or SSO.
- Input: Account, workspace, user.
- Output: Same seat rule as UC-BIL-F01.
- Main flow: 1. Each join path calls the seat gate. 2. Refusal leaves membership unchanged.
- No-op / rejection / concurrency / retry: Needs Workspaces cooperation; Billing does not write membership.
- UI states: Seat-limit error.
- Rules: Product §77, §84; spec BILREQ090; cross-team: [cross-team-dependencies](../cross-team-dependencies.md) section 16.
- Acceptance: Given the seat limit reached, when a member is added directly, then it is refused.
- Evidence: BE none beyond UC-BIL-F01 · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Direct-add and other join paths are unchecked (only invitation acceptance calls the gate).

## G. BIL-007 Usage event/record contract

### UC-BIL-G01 Ingest a metered usage fact once
- Capability: G. BIL-007 Usage event/record contract
- Actors and permission: Producer context (Automation executions, others) through a Billing port.
- Preconditions: A source operation succeeded.
- Input: Account, workspace, metric key, quantity, logical event id, time.
- Output: One accepted usage record in the ledger.
- Main flow: 1. Producer sends the fact after commit. 2. Billing dedups by logical id. 3. Ledger row is appended.
- No-op / rejection / concurrency / retry: Same id and payload returns the earlier result; same id with another payload conflicts. Failed source operations send nothing.
- UI states: None.
- Rules: Product §46-§50 BIL-017, BIL-018, §93 BIL-031; spec BILREQ043-045, BILREQ091, BILREQ136.
- Acceptance: Given a retried fact, when ingested twice, then usage grows once.
- Evidence: BE none for metering (`UsageMetric`, `UsageMetricHistory` entities; the ledger is used only by capacity, UC-BIL-I01) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Metering is not enabled; no metric vocabulary beyond capacity codes.

### UC-BIL-G02 Correct usage with compensating evidence
- Capability: G. BIL-007 Usage event/record contract
- Actors and permission: System or billing operator (principal as in UC-BIL-D02).
- Preconditions: A wrong fact was accepted.
- Input: Original fact id, corrected quantity, reason.
- Output: A reversal or adjustment row; totals follow; history stays.
- Main flow: 1. Correction submitted. 2. Billing appends a compensating row. 3. Aggregate rebuilds.
- No-op / rejection / concurrency / retry: Never edits history. Repeat of the same correction is a no-op.
- UI states: None.
- Rules: Product §53, §52 BIL-019; spec BILREQ049.
- Acceptance: Given a correction, when totals are read, then they equal original plus adjustment.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Depends on UC-BIL-G01.

## H. BIL-008 Usage aggregation

### UC-BIL-H01 Read current-period usage
- Capability: H. BIL-008 Usage aggregation
- Actors and permission: Billing managers.
- Preconditions: Metered metric exists.
- Input: Account, metric.
- Output: Used and limit for the period.
- Main flow: 1. Billing sums accepted facts for the period. 2. UI shows a meter.
- No-op / rejection / concurrency / retry: Read only.
- UI states: Loading, success, error.
- Rules: Product §54; spec BILREQ046, BILREQ136.
- Acceptance: Given 40 runs in the month, when read, then the meter shows 40.
- Evidence: BE none · FE `frontend/packages/features/workspace/src/web/billing/``billing-overview-surface.tsx` (runs meter, mock) · Design `Final-Billing.dc.html` `tong-quan` · Tests `frontend/packages/features/workspace/src/web/billing/``__tests__/billing.interaction.component.test.tsx` · Real e2e none
- State: DEFINED — Backend has no metered periods.

### UC-BIL-H02 Close a usage period and start the next
- Capability: H. BIL-008 Usage aggregation
- Actors and permission: System.
- Preconditions: Period end reached.
- Input: Account, period.
- Output: Period closed; new period opens; history stays.
- Main flow: 1. Worker closes periods. 2. Late facts follow the late-usage rule. 3. Next period starts at zero.
- No-op / rejection / concurrency / retry: Reset never deletes history. Re-run is a no-op.
- UI states: None.
- Rules: Product §55 BIL-020, §56; spec BILREQ046-048.
- Acceptance: Given period end, when it closes twice, then one new period exists.
- Evidence: BE none (`UsagePeriod` entity) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Late-usage rule is open: decision in Part 4.

### UC-BIL-H03 Rebuild a usage aggregate from the ledger
- Capability: H. BIL-008 Usage aggregation
- Actors and permission: System repair path.
- Preconditions: Totals look wrong.
- Input: Account, metric.
- Output: Total recomputed and compared.
- Main flow: 1. Repair reads the ledger. 2. Billing rebuilds the aggregate. 3. Difference is logged.
- No-op / rejection / concurrency / retry: Privileged; repeatable.
- UI states: None.
- Rules: Product §52 BIL-019; spec BILREQ050, BILREQ136.
- Acceptance: Given a drifted total, when rebuilt, then it equals the ledger sum.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Needs the repair principal (see UC-BIL-D02).

### UC-BIL-H04 See usage split across the account's workspaces
- Capability: H. BIL-008 Usage aggregation
- Actors and permission: Billing managers.
- Preconditions: Account has several workspaces.
- Input: Workspace id.
- Output: Seats, runs and storage used, per workspace and in total.
- Main flow: 1. Overview loads account usage. 2. UI shows per-workspace shares.
- No-op / rejection / concurrency / retry: Counts only; a caller sees workspaces they hold or counts without names.
- UI states: Loading, success, error.
- Rules: Product §77, §84; spec BILREQ052, BILREQ082.
- Acceptance: Given two workspaces, when a manager opens the overview, then both appear and the total matches.
- Evidence: BE partial: `ops.billing_account_usage` returns account totals (seats, guests, storage bytes) used by UC-BIL-E02 · FE `frontend/packages/features/workspace/src/web/billing/``billing-overview-surface.tsx` (mock) · Design `Final-Billing.dc.html` `tong-quan` · Tests `backend/tests/Notrelix.Integration.Tests/Rls/BillingUsagePolicyTests.cs` · Real e2e none
- State: IMPLEMENTING — No per-workspace split or run counts from the backend.

## I. BIL-009 Usage limit enforcement handoff

### UC-BIL-I01 Take one capacity slot for a new quota-bearing resource
- Capability: I. BIL-009 Usage limit enforcement handoff
- Actors and permission: Producer context (Automation rule creation).
- Preconditions: Capability has a ceiling; the request is inside the producer's transaction.
- Input: Operation identity: account, workspace, capability, amount, logical operation id, source resource, actor, time.
- Output: Usage row increased and one ledger row appended, atomically with the resource.
- Main flow: 1. Producer calls `IBillingCapacityActions.ConsumeAsync`. 2. Billing replays or conflicts by logical id. 3. Usage row consumes; ledger row is written in the same transaction.
- No-op / rejection / concurrency / retry: Replay with same payload returns AlreadyConsumed. Same id with different payload raises `CapacityOperationConflictException`. Over the ceiling the aggregate refuses.
- UI states: Limit reached on the rule form.
- Rules: Product §92-§95 BIL-031, BIL-032; spec BILREQ035-039, BILREQ107.
- Acceptance: Given one slot left, when two creates race, then one wins and one is refused. Given a retried id, then one consume.
- Evidence: BE `Features/Billing/Capacity/BillingCapacityActions.cs`, caller `Features/Automation/Rules/Commands/CreateAutomationRule/CreateAutomationRule.cs` · FE none · Design none · Tests `backend/tests/Notrelix.Application.Tests/Features/Billing/``BillingCapacityActionsTests.cs`, `backend/tests/Notrelix.Application.Tests/Features/Automation/Rules/CreateAutomationRuleBillingGateTests.cs`, `backend/tests/Notrelix.Architecture.Tests/LayerRules/BillingCapacitySemanticsArchitectureTests.cs` · Real e2e none
- State: INTEGRATING — No real-database concurrency run in this catalog's evidence; no real e2e; only Automation uses it.

### UC-BIL-I02 Give a capacity slot back when the resource is deleted
- Capability: I. BIL-009 Usage limit enforcement handoff
- Actors and permission: Producer context (Automation rule deletion).
- Preconditions: A consumed slot exists.
- Input: Operation identity as in UC-BIL-I01.
- Output: Usage decreased, a negative ledger row appended.
- Main flow: 1. Producer calls `ReleaseAsync` inside its transaction. 2. Billing replays or conflicts by logical id. 3. Usage releases.
- No-op / rejection / concurrency / retry: Release with no usage row is a no-op (AlreadyReleased). Replay safe.
- UI states: None.
- Rules: Product §94; spec BILREQ041.
- Acceptance: Given a deleted rule, when released twice, then capacity returns once.
- Evidence: BE `Features/Billing/Capacity/BillingCapacityActions.cs`, caller `Features/Automation/Rules/Commands/DeleteAutomationRule/DeleteAutomationRule.cs` · FE none · Design none · Tests `backend/tests/Notrelix.Application.Tests/Features/Billing/``BillingCapacityActionsTests.cs` · Real e2e none
- State: INTEGRATING — No real e2e; restore-from-trash does not re-consume (open).

### UC-BIL-I03 Keep committed usage when a limit shrinks
- Capability: I. BIL-009 Usage limit enforcement handoff
- Actors and permission: System.
- Preconditions: Plan limit falls below current usage.
- Input: Capability, new ceiling.
- Output: Ceiling reconfigured; usage kept; new consumption denied.
- Main flow: 1. Next capacity call reconciles the ceiling from the current fact. 2. Usage above it blocks consumption only.
- No-op / rejection / concurrency / retry: Missing grant means zero ceiling, never unlimited.
- UI states: Over-limit notice; read-only for new items.
- Rules: Product §34 BIL-012, §91; spec BILREQ040.
- Acceptance: Given 8 rules and a limit of 5, when a rule is created, then it is refused and the 8 stay.
- Evidence: BE `BillingCapacityActions.ReconcileLimitsAsync` · FE `frontend/packages/features/workspace/src/web/billing/``billing-downgrade-surface.tsx` (preview, mock) · Design `Final-Billing.dc.html` `ha-goi` · Tests `backend/tests/Notrelix.Application.Tests/Features/Billing/``BillingCapacityActionsTests.cs` · Real e2e `frontend/e2e/real/billing-plan.real.e2e.spec.ts` (limits follow plan; over-usage not covered)
- State: INTEGRATING — No e2e of an over-limit downgrade; reconcile happens only on the next call.

### UC-BIL-I04 Repair capacity from the ledger
- Capability: I. BIL-009 Usage limit enforcement handoff
- Actors and permission: System repair path.
- Preconditions: Usage row and source resources disagree.
- Input: Account, workspace, capability.
- Output: Usage row equals ledger sum and source count.
- Main flow: 1. Repair reads ledger and source count. 2. Billing writes an adjustment row.
- No-op / rejection / concurrency / retry: Privileged; history kept.
- UI states: None.
- Rules: Product §52; spec BILREQ035, BILREQ045, BILREQ135.
- Acceptance: Given a drifted row, when repaired, then it equals the source count.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No repair command; needs repair principal.

### UC-BIL-I05 Show over-limit resources before a downgrade
- Capability: I. BIL-009 Usage limit enforcement handoff
- Actors and permission: Billing managers.
- Preconditions: Plan below current usage is considered.
- Input: Target plan.
- Output: List of capabilities that go over and what becomes read-only.
- Main flow: 1. Manager opens the preview. 2. Billing compares usage to target limits. 3. UI lists effects.
- No-op / rejection / concurrency / retry: Read only; nothing changes.
- UI states: Loading, success, error, destructive confirmation (none: non-destructive).
- Rules: Product §102; spec BILREQ023.
- Acceptance: Given 8 rules and a target limit of 5, when previewed, then 3 over-limit rules are listed.
- Evidence: BE none (`GET .../billing/downgrade-preview` not built) · FE `frontend/packages/features/workspace/src/web/billing/``billing-downgrade-surface.tsx`, `billing/billing-api.ts` (`downgradePreview`) · Design `Final-Billing.dc.html` `ha-goi` · Tests `frontend/packages/features/workspace/src/web/billing/``__tests__/billing.interaction.component.test.tsx` · Real e2e none
- State: IMPLEMENTING — Mock-only. Same use case as UC-BIL-R03; kept here for the usage view.

## J. BIL-010 Billing administration

### UC-BIL-J01 Let workspace Owner and Admin view billing
- Capability: J. BIL-010 Billing administration
- Actors and permission: Workspace Owner, Admin.
- Preconditions: Caller holds the workspace.
- Input: Workspace id.
- Output: Billing reads succeed for them; others get NotFound.
- Main flow: 1. Pipeline checks `ViewBilling` on resource `billing.plan` or `billing.capability`. 2. Handler runs.
- No-op / rejection / concurrency / retry: Not entitled and not authorized are different (UC-BIL-T03).
- UI states: Permission denied hides the page and its nav.
- Rules: Product §78, §79 BIL-028; spec BILREQ054, BILREQ055.
- Acceptance: Given a Member, when they read billing, then NotFound. Given an Admin, then success.
- Evidence: BE `Features/Billing/Plans/Queries/GetWorkspaceBillingPlan/GetWorkspaceBillingPlan.cs`, `Entitlements/Queries/.../GetWorkspaceBillingCapabilities.cs` · FE `frontend/apps/web/src/routes/workspaces/$workspaceId/billing.tsx` (`forbidden` state hides nav) · Design none · Tests `backend/tests/Notrelix.Application.Tests/Features/Billing/``GetWorkspaceBillingCapabilitiesTests.cs` · Real e2e `frontend/e2e/real/billing-plan.real.e2e.spec.ts`
- State: HARDENING — Interim permission; see UC-BIL-J05. No cross-tenant test.

### UC-BIL-J02 Delegate billing management to a member
- Capability: J. BIL-010 Billing administration
- Actors and permission: Account owner.
- Preconditions: Target is an account member.
- Input: User id, grant or remove.
- Output: A billing manager listed with the month assigned; they can manage billing.
- Main flow: 1. Owner assigns. 2. Governance records the permission. 3. UI lists managers.
- No-op / rejection / concurrency / retry: Removing the last manager keeps the owner. Repeat is a no-op.
- UI states: Loading, success, permission denied, destructive confirmation.
- Rules: Product §78, §79 BIL-028; spec BILREQ053, BILREQ054, BILREQ092; Governance owns the permission.
- Acceptance: Given a delegate, when they open billing, then they can manage; a non-manager sees view-only.
- Evidence: BE none · FE `frontend/packages/features/workspace/src/web/billing/``billing-overview-surface.tsx` (managers list, mock) · Design `Final-Billing.dc.html` `tong-quan`, `chi-xem` · Tests `frontend/packages/features/workspace/src/web/billing/``__tests__/billing.interaction.component.test.tsx` · Real e2e none
- State: IMPLEMENTING — No Governance action for billing administration; needs the Governance owner's permission name (Part 5).

### UC-BIL-J03 Show a view-only billing page to people who cannot manage
- Capability: J. BIL-010 Billing administration
- Actors and permission: Workspace people without billing management.
- Preconditions: Viewer lacks manage right.
- Input: Workspace id.
- Output: Overview without action buttons; says who can help.
- Main flow: 1. Overview returns `viewer.canManage` false. 2. UI hides actions and names the managers.
- No-op / rejection / concurrency / retry: Server still refuses their writes.
- UI states: Read-only, permission denied.
- Rules: Product §101; spec BILREQ099.
- Acceptance: Given a viewer, when they open billing, then no buy or downgrade control is shown and managers are named.
- Evidence: BE none (`viewer.canManage` exists only in the mock) · FE `frontend/apps/web/src/routes/workspaces/$workspaceId/billing.tsx`, `frontend/packages/features/workspace/src/web/billing/``billing-overview-surface.tsx` · Design `Final-Billing.dc.html` `chi-xem` · Tests `frontend/packages/features/workspace/src/web/billing/``__tests__/billing.interaction.component.test.tsx` · Real e2e none (real spec `billing-plan.real.e2e.spec.ts` checks no switcher)
- State: IMPLEMENTING — Real backend only offers `canChangePlan`.

### UC-BIL-J04 Keep invoice profile: company, tax id, address, email (enterprise baseline)
- Capability: J. BIL-010 Billing administration
- Actors and permission: Billing managers.
- Preconditions: Account exists.
- Input: Company, tax id, address, billing email (validated).
- Output: Profile stored on the Billing Customer; used on invoices.
- Main flow: 1. Manager edits. 2. Billing validates and stores. 3. Next invoice uses it.
- No-op / rejection / concurrency / retry: Version check on edit; same values are a no-op.
- UI states: Validation error, success, conflict.
- Rules: Product §18, §19 BIL-007; spec BILREQ053; enterprise baseline, Part 5.
- Acceptance: Given a changed tax id, when saved, then new invoices carry it and old ones do not change.
- Evidence: BE none (`BillingCustomer` entity) · FE `frontend/packages/features/workspace/src/web/billing/``billing-models.ts` (`BillingInvoiceProfileDto`, read only) · Design none · Tests none · Real e2e none
- State: DEFINED — No edit flow, no artboard state.

### UC-BIL-J05 Authorize billing with its own permission
- Capability: J. BIL-010 Billing administration
- Actors and permission: Governance owner decides; Billing consumes.
- Preconditions: Governance defines a billing action.
- Input: Billing view, manage plan, manage payment, export invoices.
- Output: Each billing command and read names its own permission.
- Main flow: 1. Governance adds actions. 2. Billing requests use them. 3. Tests prove each.
- No-op / rejection / concurrency / retry: Entitlement never grants administration.
- UI states: Permission denied, read-only.
- Rules: Product §78, §79 BIL-028; spec BILREQ054, BILREQ055, BILREQ092; Part 5.
- Acceptance: Given a person with a paid feature but no billing permission, when they read invoices, then NotFound.
- Evidence: BE partial: the two billing reads use `PermissionAction.ViewBilling` (Owner only; custom roles may add it) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Owner of Governance permission list must add the actions.

## K. BIL-011 Payment-method lifecycle

### UC-BIL-K01 Add a payment method through the provider's hosted form
- Capability: K. BIL-011 Payment-method lifecycle
- Actors and permission: Billing managers.
- Preconditions: A payment provider is configured (none yet).
- Input: None from Notrelix; the provider collects the card.
- Output: A PaymentMethod with provider reference, brand, last 4, expiry, status.
- Main flow: 1. Manager starts. 2. Billing creates an operation and returns a hosted URL. 3. Provider callback records safe metadata only.
- No-op / rejection / concurrency / retry: Notrelix never receives card numbers or CVC. Retry reuses the operation.
- UI states: Loading, pending, success, error.
- Rules: Product §63, §64 BIL-023, §65; owner decision 46 (no provider yet); spec BILREQ076, BILREQ077.
- Acceptance: Given a completed hosted form, when the callback arrives, then only brand, last 4 and expiry are stored.
- Evidence: BE none (`PaymentMethod` entity, event `PaymentMethodAddedDomainEvent`) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Provider not chosen.

### UC-BIL-K02 See the card or bank transfer on file
- Capability: K. BIL-011 Payment-method lifecycle
- Actors and permission: Billing managers.
- Preconditions: Account has a method.
- Input: Workspace id.
- Output: Brand, last 4, expiry, status ok or declined, or transfer.
- Main flow: 1. Overview returns safe metadata. 2. UI shows it.
- No-op / rejection / concurrency / retry: Read only; no card data.
- UI states: Loading, success, error, declined.
- Rules: Product §63; spec BILREQ052, BILREQ138.
- Acceptance: Given a Visa ending 4821, when a manager opens billing, then `Visa •••• 4821` is shown.
- Evidence: BE none · FE `frontend/packages/features/workspace/src/web/billing/``billing-overview-surface.tsx` · Design `Final-Billing.dc.html` `tong-quan`, `qua-han` · Tests `frontend/e2e/mock/billing.mock.e2e.spec.ts` · Real e2e none
- State: IMPLEMENTING — Mock-only.

### UC-BIL-K03 Replace the default payment method
- Capability: K. BIL-011 Payment-method lifecycle
- Actors and permission: Billing managers.
- Preconditions: At least one method exists.
- Input: Method id.
- Output: Exactly one default.
- Main flow: 1. Manager picks. 2. Billing sets default and unsets the old in one commit.
- No-op / rejection / concurrency / retry: Concurrent changes leave one default (version check). Same default is a no-op.
- UI states: Loading, success, conflict.
- Rules: Product §63; spec BILREQ078, BILREQ108.
- Acceptance: Given two concurrent changes, when both finish, then one default exists.
- Evidence: BE none (`PaymentMethod.SetAsDefault`, `UnsetAsDefault`) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No handler.

### UC-BIL-K04 Remove a payment method
- Capability: K. BIL-011 Payment-method lifecycle
- Actors and permission: Billing managers.
- Preconditions: Method exists; not the only one while a paid plan is active.
- Input: Method id.
- Output: Method deactivated at provider and in Billing.
- Main flow: 1. Manager confirms. 2. Billing deactivates through an operation. 3. History stays.
- No-op / rejection / concurrency / retry: Removing the only method of a paid plan is refused.
- UI states: Destructive confirmation, error.
- Rules: Product §63; spec BILREQ076.
- Acceptance: Given the only method of a paid plan, when removed, then it is refused.
- Evidence: BE none (`PaymentMethod.Deactivate`) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No handler.

### UC-BIL-K05 Warn before a card expires or when it is declined (enterprise baseline)
- Capability: K. BIL-011 Payment-method lifecycle
- Actors and permission: Billing managers (notice).
- Preconditions: Method expires soon or a charge failed.
- Input: Method status.
- Output: In-app and email notice naming who can fix it.
- Main flow: 1. Worker checks expiry. 2. Notification goes to managers.
- No-op / rejection / concurrency / retry: One notice per period.
- UI states: Banner.
- Rules: Product §29; enterprise baseline (Stripe, Atlassian), Part 5.
- Acceptance: Given a card expiring next month, when the worker runs twice, then one notice is sent.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Needs Collaboration notification contract.

## L. BIL-012 Invoice lifecycle

### UC-BIL-L01 List invoices
- Capability: L. BIL-012 Invoice lifecycle
- Actors and permission: Billing managers.
- Preconditions: Account has invoices.
- Input: Workspace id.
- Output: Invoices with number, date, due date, status, kind, quantity, period.
- Main flow: 1. Manager opens `hoa-don`. 2. UI lists newest first.
- No-op / rejection / concurrency / retry: Account-scoped; paged when long. Read only.
- UI states: Loading, empty, success, error, read-only.
- Rules: Product §59; spec BILREQ138.
- Acceptance: Given three invoices, when listed, then newest is first.
- Evidence: BE none (`GET .../billing/invoices` not built) · FE `frontend/packages/features/workspace/src/web/billing/``billing-invoices-surface.tsx`, `billing/billing-api.ts` (`invoices`) · Design `Final-Billing.dc.html` `hoa-don` · Tests `frontend/packages/features/workspace/src/web/billing/``__tests__/billing.interaction.component.test.tsx` · Real e2e none
- State: IMPLEMENTING — Mock-only; no pagination contract; statuses in the mock are `paid` and `overdue`, domain has five.

### UC-BIL-L02 Open an invoice
- Capability: L. BIL-012 Invoice lifecycle
- Actors and permission: Billing managers.
- Preconditions: Invoice belongs to the account.
- Input: Invoice id.
- Output: Header, buyer, line (kind, quantity, unit price, period, amount), subtotal, VAT, total, method.
- Main flow: 1. Manager opens an invoice. 2. UI shows the stored lines.
- No-op / rejection / concurrency / retry: Other account's invoice gives NotFound. Read only.
- UI states: Loading, success, error.
- Rules: Product §60 BIL-022, §61; spec BILREQ073.
- Acceptance: Given an invoice id of another account, when opened, then NotFound.
- Evidence: BE none · FE `frontend/packages/features/workspace/src/web/billing/``billing-invoices-surface.tsx`, `billing/billing-api.ts` (`invoice`) · Design `Final-Billing.dc.html` `hoa-don` · Tests mock handler `frontend/packages/dev/mock-backend/src/contexts/billing/billing.handlers.ts` · Real e2e none
- State: IMPLEMENTING — Mock-only.

### UC-BIL-L03 Download an invoice PDF
- Capability: L. BIL-012 Invoice lifecycle
- Actors and permission: Billing managers.
- Preconditions: Invoice is issued.
- Input: Invoice id.
- Output: A file name and a short-lived download link.
- Main flow: 1. Manager chooses download. 2. Billing authorizes and returns the link.
- No-op / rejection / concurrency / retry: Link expires; each call audited.
- UI states: Pending, success, error.
- Rules: Product §96; spec BILREQ073.
- Acceptance: Given an issued invoice, when downloaded, then a PDF with the stored lines is returned and the call is audited.
- Evidence: BE none · FE `billing/billing-api.ts` (`invoicePdf`) · Design `Final-Billing.dc.html` `hoa-don` · Tests mock handler `frontend/packages/dev/mock-backend/src/contexts/billing/billing.handlers.ts` · Real e2e none
- State: IMPLEMENTING — Mock-only; no PDF producer; no audit.

### UC-BIL-L04 Issue an invoice for a charge
- Capability: L. BIL-012 Invoice lifecycle
- Actors and permission: System.
- Preconditions: Renewal, seat purchase or plan change produced a charge.
- Input: Subscription, lines, period.
- Output: Invoice Draft to Open with lines; fact published.
- Main flow: 1. Billing creates the invoice. 2. It issues it. 3. Provider reference attached when one exists.
- No-op / rejection / concurrency / retry: One invoice per operation.
- UI states: None.
- Rules: Product §59, §60 BIL-022; spec BILREQ073, BILREQ074.
- Acceptance: Given one renewal run twice, then one invoice exists.
- Evidence: BE none (`Invoice.Issue`, events `InvoiceCreated`, `InvoiceIssued`) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No handler.

### UC-BIL-L05 Void or credit an invoice
- Capability: L. BIL-012 Invoice lifecycle
- Actors and permission: Billing operator. Principal undefined.
- Preconditions: Invoice is issued.
- Input: Invoice id, reason.
- Output: Invoice Void or a credit line; the original stays.
- Main flow: 1. Operator submits. 2. Billing voids or credits.
- No-op / rejection / concurrency / retry: BLOCKED until the operator principal is decided; paid invoice cannot be voided.
- UI states: Operator tool only.
- Rules: Product §60; spec BILREQ074.
- Acceptance: Given a paid invoice, when voided, then it is refused.
- Evidence: BE none (`Invoice.Void`) · FE none · Design none · Tests none · Real e2e none
- State: BLOCKED — Same principal decision as UC-BIL-D02.

### UC-BIL-L06 Show VAT and buyer tax data on invoices (enterprise baseline)
- Capability: L. BIL-012 Invoice lifecycle
- Actors and permission: System.
- Preconditions: Invoice profile set.
- Input: VAT percent, tax id.
- Output: Subtotal, VAT, total, buyer block on the invoice.
- Main flow: 1. Billing computes VAT at issue. 2. Stores it on the invoice.
- No-op / rejection / concurrency / retry: Stored values never recomputed.
- UI states: None.
- Rules: Product §61; enterprise baseline, Part 5.
- Acceptance: Given 10 percent VAT, when issued, then VAT equals 10 percent of the subtotal and stays.
- Evidence: BE none · FE `frontend/packages/features/workspace/src/web/billing/``billing-models.ts` (`vatPercent`) · Design none · Tests none · Real e2e none
- State: DEFINED — No tax model.

## M. BIL-013 Payment lifecycle

### UC-BIL-M01 Pay an open invoice
- Capability: M. BIL-013 Payment lifecycle
- Actors and permission: Billing managers.
- Preconditions: Invoice is Open or overdue.
- Input: Invoice id, idempotency key.
- Output: Payment attempt; invoice Paid after settlement.
- Main flow: 1. Manager chooses Pay. 2. Billing creates a payment operation. 3. UI shows pending until the provider settles.
- No-op / rejection / concurrency / retry: Same key creates one attempt. Unknown outcome stays pending, never shown as paid.
- UI states: Pending, success, error.
- Rules: Product §65, §66, §103; spec BILREQ059-061, BILREQ075.
- Acceptance: Given a double click, when paid with one key, then one attempt exists.
- Evidence: BE none · FE `billing/billing-api.ts` (`payInvoice`), `frontend/packages/features/workspace/src/web/billing/``billing-invoices-surface.tsx` · Design `Final-Billing.dc.html` `qua-han` · Tests mock handler `frontend/packages/dev/mock-backend/src/contexts/billing/billing.handlers.ts` · Real e2e none
- State: IMPLEMENTING — Mock-only.

### UC-BIL-M02 Record payment settlement from the provider
- Capability: M. BIL-013 Payment lifecycle
- Actors and permission: System (webhook worker).
- Preconditions: A verified receipt exists.
- Input: Provider event.
- Output: Payment settled; invoice Paid; fact published.
- Main flow: 1. Worker maps the event. 2. Billing records settlement once. 3. Subscription recovers (UC-BIL-S03).
- No-op / rejection / concurrency / retry: Duplicate event changes nothing.
- UI states: None.
- Rules: Product §69-§71; spec BILREQ075.
- Acceptance: Given a duplicate paid event, then one settlement.
- Evidence: BE none (`Invoice.MarkPaid`) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No provider.

### UC-BIL-M03 Record a failed payment
- Capability: M. BIL-013 Payment lifecycle
- Actors and permission: System.
- Preconditions: Provider reports failure.
- Input: Provider event.
- Output: Invoice failed; subscription past due.
- Main flow: 1. Worker maps event. 2. Billing records failure. 3. UC-BIL-S01 runs.
- No-op / rejection / concurrency / retry: Product data unchanged.
- UI states: Declined notice.
- Rules: Product §66, §30 BIL-011; spec BILREQ075.
- Acceptance: Given a failure, then invoice is failed and no product resource changes.
- Evidence: BE none (`Invoice.MarkFailed`, `InvoiceFailedDomainEvent`) · FE none · Design `Final-Billing.dc.html` `qua-han` · Tests none · Real e2e none
- State: DEFINED — No provider.

## N. BIL-014 Provider customer/subscription mapping

### UC-BIL-N01 Create the provider customer for an account
- Capability: N. BIL-014 Provider customer/subscription mapping
- Actors and permission: System.
- Preconditions: Account has a Billing Customer, no provider customer.
- Input: Account id, provider.
- Output: Provider customer reference stored, scoped to the provider.
- Main flow: 1. Billing creates an operation. 2. Adapter calls the provider with the operation id as key. 3. Reference is stored.
- No-op / rejection / concurrency / retry: Retry creates one customer. Provider id never becomes the Account id.
- UI states: None.
- Rules: Product §18-§20, §68 BIL-024; spec BILREQ058, BILREQ059.
- Acceptance: Given a retry after a crash, then one provider customer exists.
- Evidence: BE none (`BillingCustomer` entity) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Provider not chosen (owner decision 46: no payment provider yet).

### UC-BIL-N02 Start a checkout or subscription change
- Capability: N. BIL-014 Provider customer/subscription mapping
- Actors and permission: Billing managers.
- Preconditions: Provider configured; caller may manage billing.
- Input: Target plan, operation id.
- Output: An accepted operation id first; a hosted URL after settlement.
- Main flow: 1. Billing stores the operation (prepare). 2. A worker calls the provider (effect). 3. Result is settled and exposed.
- No-op / rejection / concurrency / retry: No provider call inside the request transaction. Unknown outcome forbids blind retry.
- UI states: None.
- Rules: Product §74, §73 BIL-026; spec BILREQ057, BILREQ059-062, BILREQ137.
- Acceptance: Given a repeated click, then one provider session exists.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Provider not chosen (owner decision 46: no payment provider yet).

### UC-BIL-N03 Open the provider's billing portal
- Capability: N. BIL-014 Provider customer/subscription mapping
- Actors and permission: Billing managers.
- Preconditions: Provider customer exists.
- Input: None.
- Output: A short-lived hosted portal URL.
- Main flow: 1. Billing authorizes. 2. Creates a portal session through an operation. 3. UI redirects.
- No-op / rejection / concurrency / retry: URL not logged; a redirect is not authority.
- UI states: None.
- Rules: Product §65; spec BILREQ062, BILREQ137.
- Acceptance: Given a manager, then a portal link opens; a non-manager gets NotFound.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Provider not chosen (owner decision 46: no payment provider yet).

### UC-BIL-N04 Read the state of a billing operation
- Capability: N. BIL-014 Provider customer/subscription mapping
- Actors and permission: Billing managers.
- Preconditions: An operation exists.
- Input: Operation id.
- Output: Pending, succeeded, failed or unknown, with safe result.
- Main flow: 1. UI polls the operation. 2. Billing returns its settled state.
- No-op / rejection / concurrency / retry: Account-scoped.
- UI states: Pending, success, error.
- Rules: Product §103; spec BILREQ137.
- Acceptance: Given a pending operation, then the UI shows pending, not success.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Provider not chosen (owner decision 46: no payment provider yet).

## O. BIL-015 Provider webhook verification

### UC-BIL-O01 Verify a provider webhook and store the receipt
- Capability: O. BIL-015 Provider webhook verification
- Actors and permission: System (public endpoint, not browser-authorized).
- Preconditions: Provider sends an event.
- Input: Signed body.
- Output: A verified receipt stored; acknowledgement after durable write.
- Main flow: 1. Endpoint verifies signature and age. 2. Stores the raw receipt apart from the Domain payload. 3. Acknowledges.
- No-op / rejection / concurrency / retry: Bad signature returns an error and stores nothing. Secrets never logged.
- UI states: None.
- Rules: Product §69; spec BILREQ064, BILREQ065, BILREQ067, BILREQ102.
- Acceptance: Given a forged body, then it is rejected and nothing is stored.
- Evidence: BE none (`BillingEvent` entity, `ProviderEventId`) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Provider not chosen (owner decision 46: no payment provider yet).

## P. BIL-016 Provider webhook idempotency

### UC-BIL-P01 Make duplicate webhook delivery harmless
- Capability: P. BIL-016 Provider webhook idempotency
- Actors and permission: System.
- Preconditions: A receipt with the same provider event id exists.
- Input: Provider event id.
- Output: No second record; one effect.
- Main flow: 1. Database unique key on provider event id. 2. Duplicate is acknowledged and ignored.
- No-op / rejection / concurrency / retry: No duplicate Subscription, Invoice, Entitlement or usage.
- UI states: None.
- Rules: Product §70 BIL-025; spec BILREQ066, BILREQ127.
- Acceptance: Given two deliveries, then one effect.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Provider not chosen (owner decision 46: no payment provider yet).

### UC-BIL-P02 Ignore unknown provider events safely
- Capability: P. BIL-016 Provider webhook idempotency
- Actors and permission: System.
- Preconditions: Event type has no mapping.
- Input: Event.
- Output: Receipt marked Ignored.
- Main flow: 1. Mapper finds no mapping. 2. Receipt is Ignored and kept.
- No-op / rejection / concurrency / retry: No state change.
- UI states: None.
- Rules: Product §87 BIL-029; spec BILREQ068.
- Acceptance: Given an unknown type, then it is Ignored and acknowledged.
- Evidence: BE none (`BillingEventStatus.Ignored`) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Provider not chosen (owner decision 46: no payment provider yet).

### UC-BIL-P03 Apply out-of-order events correctly
- Capability: P. BIL-016 Provider webhook idempotency
- Actors and permission: System.
- Preconditions: Older event arrives after a newer one.
- Input: Event with provider time or revision.
- Output: Newer state kept.
- Main flow: 1. Mapper compares revision and canonical lifecycle. 2. Stale event is recorded but not applied.
- No-op / rejection / concurrency / retry: Arrival order never decides.
- UI states: None.
- Rules: Product §71; spec BILREQ069.
- Acceptance: Given cancel then a late renew event, then the subscription stays canceled.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Provider not chosen (owner decision 46: no payment provider yet).

## Q. BIL-017 Provider state reconciliation

### UC-BIL-Q01 Reconcile a subscription with the provider
- Capability: Q. BIL-017 Provider state reconciliation
- Actors and permission: System; scoped repair by privileged operator.
- Preconditions: Drift suspected or schedule due.
- Input: Subscription id.
- Output: Billing state matches the provider-owned fields; differences logged.
- Main flow: 1. Worker reads provider state. 2. Billing applies only provider-owned fields through valid transitions.
- No-op / rejection / concurrency / retry: Repair authority is scoped and audited.
- UI states: None.
- Rules: Product §23, §24 BIL-009, §72; spec BILREQ070-072, BILREQ103.
- Acceptance: Given a missed webhook, when reconcile runs, then state converges.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Provider not chosen (owner decision 46: no payment provider yet).

### UC-BIL-Q02 Resolve an operation with unknown outcome
- Capability: Q. BIL-017 Provider state reconciliation
- Actors and permission: System.
- Preconditions: Provider call timed out.
- Input: Operation id.
- Output: Operation settles to succeeded or failed.
- Main flow: 1. Worker queries the provider by the operation key. 2. Billing settles.
- No-op / rejection / concurrency / retry: Never retried blindly.
- UI states: None.
- Rules: Product §73 BIL-026; spec BILREQ060, BILREQ061, BILREQ126.
- Acceptance: Given a timeout after provider success, then no second charge.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Provider not chosen (owner decision 46: no payment provider yet).

### UC-BIL-Q03 Check a seat purchase that is still waiting for the bank
- Capability: Q. BIL-017 Provider state reconciliation
- Actors and permission: Billing managers.
- Preconditions: A purchase is pending.
- Input: Workspace id, purchase id.
- Output: Purchase moves to succeeded or stays pending; last-checked time.
- Main flow: 1. Dialog shows `dang-xu-ly`. 2. Manager chooses check. 3. Billing re-reads the purchase.
- No-op / rejection / concurrency / retry: Never shows success before settlement.
- UI states: Pending, success, error.
- Rules: Product §103; spec BILREQ062, BILREQ137.
- Acceptance: Given a pending purchase, when checked and still unsettled, then it stays pending with the new check time.
- Evidence: BE none (`POST .../billing/seat-purchases/{purchaseId}/check` not built) · FE `billing/billing-api.ts` (`checkPurchase`), `frontend/packages/features/workspace/src/web/billing/``billing-dialogs.tsx` · Design `Final-Billing.dc.html` `dang-xu-ly` · Tests `frontend/e2e/mock/billing.mock.e2e.spec.ts` · Real e2e none
- State: IMPLEMENTING — Mock-only.

## R. BIL-018 Upgrade/downgrade/cancel

### UC-BIL-R01 Move an account to another plan (platform operator)
- Capability: R. BIL-018 Upgrade/downgrade/cancel
- Actors and permission: the platform operator (owner decision 2026-10-31). A tenant, an Account Owner included, has no plan switch: the development-only tenant switch and its endpoint were removed.
- Preconditions: The target is an active catalog revision; the Account exists.
- Input: Account id, plan revision id, reason (required), optional expected subscription version.
- Output: The account's subscription moves to the revision (created if none; a lapsed term is renewed); the new billing state is returned; limits follow at once; an audit row records actor, reason, before, after and time.
- Main flow: 1. Operator opens the tenant. 2. Picks a revision and confirms with a reason. 3. Billing validates, changes the subscription and audits it in one transaction.
- No-op / rejection / concurrency / retry: Same revision with a live term: no-op, no audit. A revision that is no longer sold: 409. Stale version: 409. The first plan change can race (no unique index on active account subscriptions, see Part 2 defects).
- UI states: Operator console, tenant detail (select, reason dialog); the tenant sees a read-only plan card.
- Rules: Product §31-§34 BIL-012; owner decision 2026-10-31 (the platform operator changes plans); spec BILREQ016.
- Acceptance: Given a tenant on Pro, when the operator moves it to Free, then the tenant's billing page shows the Free limits. Given a workspace Admin who is not the Owner, then no switcher and no plan read.
- Evidence: BE `PUT /operator/v1/accounts/{accountId}/billing/plan` (`Operator.Billing.ChangePlan`, `OperatorAccountBillingService.ChangePlanAsync`) · FE `frontend/apps/operator` tenant detail; `billing/billing-capabilities-page.tsx` (read-only card) · Design `Final-Billing.dc.html` `so-sanh-goi` (tenant side only) · Tests Application `OperatorBillingServiceTests` (21), `BillingPlanTests` (12), Integration `OperatorConsoleIntegrationTests` (29: plan change and override change the entitlement the tenant sees) · Real e2e `frontend/e2e/real/billing-plan.real.e2e.spec.ts` (passed 2026-11-01: operator signs in, moves the tenant Free, Enterprise, Pro; the tenant owner sees 10, unlimited, 500; the audit row shows the reason)
- State: HARDENING — Runs end to end on the real stack; design not diffed (no operator artboard); first-change race; the tenant UI says nothing about who changed the plan.

### UC-BIL-R02 Upgrade to a paid plan with payment
- Capability: R. BIL-018 Upgrade/downgrade/cancel
- Actors and permission: Billing managers.
- Preconditions: Provider configured; method on file.
- Input: Target plan, operation id.
- Output: Subscription changed at once; prorated charge; entitlements widen.
- Main flow: 1. Manager picks a plan. 2. Billing creates an operation. 3. On settlement the plan changes and entitlements re-derive.
- No-op / rejection / concurrency / retry: One operation per click. Never bypasses Governance. Shows pending before success.
- UI states: Pending, success, error.
- Rules: Product §32; spec BILREQ016, BILREQ022.
- Acceptance: Given a manager on Free, when they upgrade and pay, then Pro limits apply after settlement.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Needs a provider (decision 46) and a price source.

### UC-BIL-R03 Preview a downgrade
- Capability: R. BIL-018 Upgrade/downgrade/cancel
- Actors and permission: Billing managers.
- Preconditions: A lower plan exists.
- Input: Target plan.
- Output: Effective date, lost capabilities, over-limit resources, what becomes read-only.
- Main flow: 1. Manager opens `ha-goi`. 2. UI loads the preview. 3. Manager reads before choosing.
- No-op / rejection / concurrency / retry: Read only.
- UI states: Loading, success, error.
- Rules: Product §102; spec BILREQ023.
- Acceptance: Given a downgrade to Pro, then the page shows the date and the features that go.
- Evidence: BE none · FE `frontend/packages/features/workspace/src/web/billing/``billing-downgrade-surface.tsx`, `billing/billing-api.ts` (`downgradePreview`) · Design `Final-Billing.dc.html` `ha-goi` · Tests `frontend/e2e/mock/billing.mock.e2e.spec.ts` · Real e2e none
- State: IMPLEMENTING — Mock-only; mock supports only `to: pro`.

### UC-BIL-R04 Schedule a downgrade for the period end
- Capability: R. BIL-018 Upgrade/downgrade/cancel
- Actors and permission: Billing managers.
- Preconditions: Preview seen; a later plan exists.
- Input: Target plan, idempotency key.
- Output: Pending downgrade shown; plan stays until the boundary.
- Main flow: 1. Manager confirms. 2. Billing stores the scheduled change. 3. UI shows the pending downgrade.
- No-op / rejection / concurrency / retry: Same key stores one schedule. Existing data is never deleted.
- UI states: Destructive confirmation, pending, success, error.
- Rules: Product §33, §34 BIL-012; spec BILREQ022, BILREQ023.
- Acceptance: Given a confirmed downgrade, then the plan is unchanged until 2027-01-01 and the page says so.
- Evidence: BE none (`POST .../billing/downgrade` not built) · FE `billing/billing-api.ts` (`confirmDowngrade`), `frontend/packages/features/workspace/src/web/billing/``billing-downgrade-surface.tsx` · Design `Final-Billing.dc.html` `ha-goi` · Tests mock handler `frontend/packages/dev/mock-backend/src/contexts/billing/billing.handlers.ts` · Real e2e none
- State: IMPLEMENTING — Mock-only; applies at boundary needs UC-BIL-C02.

### UC-BIL-R05 Cancel a scheduled downgrade
- Capability: R. BIL-018 Upgrade/downgrade/cancel
- Actors and permission: Billing managers.
- Preconditions: A downgrade is pending.
- Input: Workspace id.
- Output: Pending downgrade removed.
- Main flow: 1. Manager chooses keep plan. 2. Billing deletes the schedule.
- No-op / rejection / concurrency / retry: No pending change is a no-op.
- UI states: Success, error.
- Rules: Product §27 BIL-010; spec BILREQ019.
- Acceptance: Given a pending downgrade, when cancelled, then the page shows no pending change.
- Evidence: BE none (`DELETE .../billing/downgrade` not built) · FE `billing/billing-api.ts` (`cancelDowngrade`) · Design `Final-Billing.dc.html` `tong-quan` · Tests mock handler `frontend/packages/dev/mock-backend/src/contexts/billing/billing.handlers.ts` · Real e2e none
- State: IMPLEMENTING — Mock-only.

### UC-BIL-R06 Schedule cancellation at the period end
- Capability: R. BIL-018 Upgrade/downgrade/cancel
- Actors and permission: Billing managers.
- Preconditions: Subscription is paid and active.
- Input: Subscription id.
- Output: Cancel-at-period-end flag and request time; access stays until the end.
- Main flow: 1. Manager confirms. 2. Domain sets the flag. 3. Fact published.
- No-op / rejection / concurrency / retry: Repeat is a no-op (no version bump). Cancellation request and effective cancellation are separate facts.
- UI states: Destructive confirmation, success.
- Rules: Product §26, §27 BIL-010; spec BILREQ019.
- Acceptance: Given an active subscription, when cancellation is scheduled twice, then one request is recorded.
- Evidence: BE partial: `Subscription.ScheduleCancellation`, event `SubscriptionCancellationScheduledDomainEvent` · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No handler or endpoint; no artboard state.

### UC-BIL-R07 Resume a scheduled cancellation
- Capability: R. BIL-018 Upgrade/downgrade/cancel
- Actors and permission: Billing managers.
- Preconditions: Flag set; period not ended.
- Input: Subscription id.
- Output: Flag cleared.
- Main flow: 1. Manager resumes. 2. Domain clears the flag.
- No-op / rejection / concurrency / retry: No flag is a no-op.
- UI states: Success.
- Rules: Product §26; spec BILREQ019.
- Acceptance: Given a scheduled cancellation, when resumed, then the subscription renews normally.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No domain method to clear the flag.

### UC-BIL-R08 Apply cancellation at the period end
- Capability: R. BIL-018 Upgrade/downgrade/cancel
- Actors and permission: System.
- Preconditions: Flag set and period ended.
- Input: Subscription id.
- Output: Subscription Canceled; account falls to Free.
- Main flow: 1. Worker applies. 2. Entitlements re-derive. 3. Data stays.
- No-op / rejection / concurrency / retry: Re-run is a no-op.
- UI states: Read-only banner if over limit.
- Rules: Product §26, §27, §30; spec BILREQ019.
- Acceptance: Given a due cancellation, when the worker runs twice, then the account is on Free once.
- Evidence: BE partial: `Subscription.CancelImmediately` · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No worker.

### UC-BIL-R09 Terminate a subscription immediately
- Capability: R. BIL-018 Upgrade/downgrade/cancel
- Actors and permission: the platform operator (owner decision 2026-10-31).
- Preconditions: Fraud, legal or account closure.
- Input: Subscription id, reason.
- Output: Subscription Canceled now; audited.
- Main flow: 1. Operator submits. 2. Billing cancels. 3. Fact published.
- No-op / rejection / concurrency / retry: a reason and the expected version are required; terminating a Canceled or Expired subscription is a no-op that writes no audit row; a stale version is 409.
- UI states: Operator console, tenant detail (destructive confirmation with a reason).
- Rules: Product §76 BIL-027; spec BILREQ020.
- Acceptance: Given an operator, when they terminate, then the account is on Free and an audit row exists.
- Evidence: BE `POST /operator/v1/accounts/{accountId}/billing/terminate` (`Operator.Billing.Terminate`), `Subscription.CancelImmediately` · FE `frontend/apps/operator` tenant detail · Design none · Tests Application `OperatorBillingServiceTests` (21), Integration `OperatorConsoleIntegrationTests` (29) · Real e2e none
- State: INTEGRATING — Backend and operator screen connect and are tested; the account then resolves to the default plan limits; no real spec; the fact published to other contexts is the domain event only.

### UC-BIL-R10 Get a quote for extra seats
- Capability: R. BIL-018 Upgrade/downgrade/cancel
- Actors and permission: Billing managers.
- Preconditions: Plan sells seats.
- Input: Quantity.
- Output: Unit price, period, days, amount, VAT, total, seats after, card and email.
- Main flow: 1. Manager enters a quantity. 2. UI loads the quote. 3. Manager reviews.
- No-op / rejection / concurrency / retry: Invalid quantity is a validation error. Read only.
- UI states: Loading, validation error, success.
- Rules: Product §31, §35; spec BILREQ016.
- Acceptance: Given 6 seats, when quoted, then total equals amount plus VAT.
- Evidence: BE none (`GET .../billing/seat-quote` not built) · FE `billing/billing-api.ts` (`seatQuote`), `frontend/packages/features/workspace/src/web/billing/``billing-dialogs.tsx` · Design `Final-Billing.dc.html` `mua-cho` · Tests `frontend/e2e/mock/billing.mock.e2e.spec.ts` · Real e2e none
- State: IMPLEMENTING — Mock-only.

### UC-BIL-R11 Buy extra seats
- Capability: R. BIL-018 Upgrade/downgrade/cancel
- Actors and permission: Billing managers.
- Preconditions: Quote shown; payment available (card or bank transfer).
- Input: Quantity, idempotency key.
- Output: A purchase (pending, then succeeded), an invoice, higher seat total.
- Main flow: 1. Manager confirms. 2. Client sends with an idempotency key. 3. Billing returns a pending purchase. 4. On settlement seats rise.
- No-op / rejection / concurrency / retry: Same key returns the same purchase. Seats never rise before settlement.
- UI states: Pending, success, error, conflict.
- Rules: Product §35, §103; owner decision 46 (payments mock-only); spec BILREQ059.
- Acceptance: Given a double submit with one key, then one purchase and one invoice.
- Evidence: BE none (`POST .../billing/seat-purchases` not built) · FE `billing/billing-api.ts` (`buySeats`), `frontend/packages/features/workspace/src/web/billing/``billing-dialogs.tsx` · Design `Final-Billing.dc.html` `mua-cho`, `dang-xu-ly` · Tests `frontend/e2e/mock/billing.mock.e2e.spec.ts` · Real e2e none
- State: IMPLEMENTING — Mock-only; `SubscriptionItem` entity unused; seat purchase has no real payment.

### UC-BIL-R12 Reduce seats at renewal (enterprise baseline)
- Capability: R. BIL-018 Upgrade/downgrade/cancel
- Actors and permission: Billing managers.
- Preconditions: Seats owned exceed need.
- Input: New seat count.
- Output: Seat total lowers at the period end; never below seats in use.
- Main flow: 1. Manager sets the count. 2. Billing schedules it. 3. Applied at boundary.
- No-op / rejection / concurrency / retry: Below seats in use is refused.
- UI states: Validation error, pending.
- Rules: Product §31, §33; enterprise baseline (Slack, Atlassian), Part 5.
- Acceptance: Given 20 owned and 12 used, when reduced to 10, then refused.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No flow or artboard.

## S. BIL-019 Trial/grace/delinquency

### UC-BIL-S01 Mark a subscription past due after a failed payment
- Capability: S. BIL-019 Trial/grace/delinquency
- Actors and permission: System.
- Preconditions: Payment failed.
- Input: Subscription id, invoice.
- Output: Status PastDue; grace and restriction dates set.
- Main flow: 1. Billing marks past due. 2. Sets grace until and restricted from. 3. Notices go out (UC-BIL-S05).
- No-op / rejection / concurrency / retry: Repeat is a no-op. Product data is untouched.
- UI states: Past-due banner.
- Rules: Product §29, §30 BIL-011, §90; owner decision 46; spec BILREQ021.
- Acceptance: Given a failed invoice, when marked twice, then one past-due period exists.
- Evidence: BE partial: `Subscription.MarkPastDue`, event `SubscriptionPastDueDomainEvent` · FE none · Design `Final-Billing.dc.html` `qua-han` · Tests none · Real e2e none
- State: DEFINED — No trigger; grace length not decided (Part 4).

### UC-BIL-S02 Block only new members while overdue
- Capability: S. BIL-019 Trial/grace/delinquency
- Actors and permission: System.
- Preconditions: Subscription is past due beyond grace.
- Input: Account, seat request.
- Output: New member and guest admissions refused; existing data and members keep working.
- Main flow: 1. Seat gate reads past-due state. 2. Refuses additions only.
- No-op / rejection / concurrency / retry: Never blocks reads or edits.
- UI states: Banner, seat-limit dialog.
- Rules: Owner decision 46 (overdue blocks new members only, never data); product §30 BIL-011, §91.
- Acceptance: Given an overdue account, when an invitation is accepted, then refused; when a board is edited, then allowed.
- Evidence: BE none (`BillingSeatGate` ignores subscription status) · FE `frontend/packages/features/workspace/src/web/billing/``billing-overview-surface.tsx` (banner) · Design `Final-Billing.dc.html` `qua-han` · Tests none · Real e2e none
- State: DEFINED — Gate does not read PastDue.

### UC-BIL-S03 Recover from past due when payment settles
- Capability: S. BIL-019 Trial/grace/delinquency
- Actors and permission: System.
- Preconditions: Subscription is PastDue.
- Input: Settlement event.
- Output: Status Active; restrictions lift.
- Main flow: 1. UC-BIL-M02 settles. 2. Billing returns the subscription to Active.
- No-op / rejection / concurrency / retry: Recover twice is a no-op.
- UI states: Success.
- Rules: Product §29; spec BILREQ021.
- Acceptance: Given a paid overdue invoice, then new members are allowed again.
- Evidence: BE none (no `Recover` method on `Subscription`) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Domain lacks the transition.

### UC-BIL-S04 See the past-due banner and retry payment
- Capability: S. BIL-019 Trial/grace/delinquency
- Actors and permission: Billing managers.
- Preconditions: Subscription is past due.
- Input: Workspace id.
- Output: Banner with invoice, card last 4, grace date, restriction date, Pay action.
- Main flow: 1. Overview returns `pastDue`. 2. UI shows banner. 3. Manager pays (UC-BIL-M01).
- No-op / rejection / concurrency / retry: Non-managers see who to ask.
- UI states: Banner, pending, read-only.
- Rules: Product §29, §100; owner decision 46.
- Acceptance: Given an overdue invoice, then the banner names the grace date and the card.
- Evidence: BE none · FE `frontend/packages/features/workspace/src/web/billing/``billing-overview-surface.tsx` · Design `Final-Billing.dc.html` `qua-han` · Tests `frontend/e2e/mock/billing.mock.e2e.spec.ts` · Real e2e none
- State: IMPLEMENTING — Mock-only.

### UC-BIL-S05 Send dunning notices (enterprise baseline)
- Capability: S. BIL-019 Trial/grace/delinquency
- Actors and permission: System; recipients are billing managers.
- Preconditions: Payment failed or retry due.
- Input: Invoice, schedule.
- Output: Email and in-app notices at set steps.
- Main flow: 1. Worker applies the schedule. 2. Collaboration delivers.
- No-op / rejection / concurrency / retry: One notice per step.
- UI states: Notices.
- Rules: Product §29; enterprise baseline (Stripe retries and emails), Part 5.
- Acceptance: Given a failure, when the schedule runs, then notices go at day 0, 3 and 7.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Needs notification contract with Collaboration.

### UC-BIL-S06 Remind before a trial ends (enterprise baseline)
- Capability: S. BIL-019 Trial/grace/delinquency
- Actors and permission: System; recipients are billing managers.
- Preconditions: Trial ends in 3 days.
- Input: Subscription.
- Output: One reminder.
- Main flow: 1. Worker finds trials ending soon. 2. Sends one reminder.
- No-op / rejection / concurrency / retry: Once per trial.
- UI states: Banner.
- Rules: Product §28; enterprise baseline (Stripe `trial_will_end`), Part 5.
- Acceptance: Given a trial ending in 3 days, when the worker runs twice, then one reminder is sent.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No trial yet (UC-BIL-B03).

## T. BIL-020 Billing frontend

### UC-BIL-T01 Open Plans and billing with every state
- Capability: T. BIL-020 Billing frontend
- Actors and permission: Workspace Owner, Admin.
- Preconditions: Route reached.
- Input: Workspace id.
- Output: The page, or a loading, error or forbidden state.
- Main flow: 1. Route chooses the full page (mock flag) or the capabilities page. 2. UI shows loading, error, forbidden (nav hidden) or data.
- No-op / rejection / concurrency / retry: Server remains authority.
- UI states: Loading, success, error, permission denied.
- Rules: Product §100; spec BILREQ095-097.
- Acceptance: Given a Member, when they open the route, then forbidden and no billing nav.
- Evidence: BE `GET /api/v1/workspaces/{workspaceId}/billing/capabilities` (Billing.Capabilities.List, `GetWorkspaceBillingCapabilitiesQueryHandler`) · FE `frontend/apps/web/src/routes/workspaces/$workspaceId/billing.tsx`, `billing/billing-capabilities-page.tsx` · Design `Final-Billing.dc.html` `tong-quan` · Tests `frontend/packages/features/billing/src/web/__tests__/billing-page.interaction.component.test.tsx` · Real e2e `frontend/e2e/real/feature-availability.real.e2e.spec.ts`
- State: HARDENING — Two pages behind `backendCapabilities.billingPlans`; the package `@notrelix/features-billing` holds a hard-coded stub page not used by the route; offline and conflict states missing.

### UC-BIL-T02 Stop an invitation at the seat limit and offer more seats
- Capability: T. BIL-020 Billing frontend
- Actors and permission: Workspace Admin inviting people.
- Preconditions: Seat summary shows no free seat.
- Input: Emails, role.
- Output: Invites up to the free seats; for the rest a way to buy seats or ask a manager.
- Main flow: 1. Dialog loads the seat summary. 2. It sends the allowed ones. 3. It names the manager when the viewer cannot buy.
- No-op / rejection / concurrency / retry: Server gate is authority (UC-BIL-F01).
- UI states: Loading, validation error, pending, permission denied.
- Rules: Product §91, §101; owner decision 46.
- Acceptance: Given 6 seats left, when 8 are invited, then 6 go and 2 are stopped with a buy link.
- Evidence: BE none (`GET /workspaces/{id}/seat-summary` not built) · FE `billing/seat-invite.tsx`, `frontend/packages/features/workspace/src/web/billing/``billing-dialogs.tsx` · Design `Final-Billing.dc.html` `het-cho` · Tests `frontend/e2e/mock/billing.mock.e2e.spec.ts` · Real e2e none
- State: IMPLEMENTING — Mock-only seat summary; invitation acceptance on the server is the only real check.

### UC-BIL-T03 Tell not entitled from not authorized
- Capability: T. BIL-020 Billing frontend
- Actors and permission: Anyone using a gated feature.
- Preconditions: A request is refused.
- Input: Error reason.
- Output: Not entitled names who manages billing; not authorized names who can grant access.
- Main flow: 1. UI reads the error code. 2. Shows the right message and action.
- No-op / rejection / concurrency / retry: Upgrade prompt only for entitlement gaps.
- UI states: Permission denied, limit reached.
- Rules: Product §101; spec BILREQ098, BILREQ099; `DESIGN.md` section 25.
- Acceptance: Given a limit reached, then the prompt names billing managers; given a permission gap, then it names workspace admins.
- Evidence: BE none (needs UC-BIL-E04) · FE partial: seat dialog copy only · Design `Final-Billing.dc.html` `het-cho` · Tests none · Real e2e none
- State: IMPLEMENTING — No stable reason codes from the backend.

### UC-BIL-T04 Say plainly what is not available yet
- Capability: T. BIL-020 Billing frontend
- Actors and permission: Workspace Owner, Admin.
- Preconditions: Real backend has no invoices or payments.
- Input: None.
- Output: A notice that invoices and payment methods are not available yet.
- Main flow: 1. Capabilities page shows the notice. 2. No fake data.
- No-op / rejection / concurrency / retry: None.
- UI states: Success, empty.
- Rules: Product §103 (no final success before reconciliation); spec BILREQ095.
- Acceptance: Given the real backend, then the notice is visible and no console error appears.
- Evidence: BE `GET /api/v1/workspaces/{workspaceId}/billing/capabilities` (Billing.Capabilities.List, `GetWorkspaceBillingCapabilitiesQueryHandler`) · FE `billing/billing-capabilities-page.tsx` · Design none · Tests none · Real e2e `frontend/e2e/real/feature-availability.real.e2e.spec.ts`
- State: HARDENING — Remove when UC-BIL-L01 is real.

## U. BIL-021 Billing events

### UC-BIL-U01 Publish canonical commercial facts
- Capability: U. BIL-021 Billing events
- Actors and permission: System.
- Preconditions: A subscription, entitlement, invoice or plan changed.
- Input: Domain event.
- Output: Public fact in the outbox, after commit.
- Main flow: 1. Aggregate raises the event. 2. Outbox publishes after commit. 3. Consumers read stable facts.
- No-op / rejection / concurrency / retry: Duplicate delivery is harmless to consumers. Provider payload is never exposed.
- UI states: None.
- Rules: Product §86, §87 BIL-029; spec BILREQ085-087.
- Acceptance: Given a plan change, then one `SubscriptionChanged` fact is published after commit.
- Evidence: BE partial: domain events in `backend/src/Notrelix.Domain/Billing/``*/Events/`, raised by `Subscription.ChangePlan` · FE none · Design none · Tests none · Real e2e none
- State: IMPLEMENTING — No consumer; events are not versioned as public contracts.

### UC-BIL-U02 Propagate an entitlement change to consumers
- Capability: U. BIL-021 Billing events
- Actors and permission: System.
- Preconditions: Entitlements changed.
- Input: Fact.
- Output: Consumers and caches refresh.
- Main flow: 1. Billing publishes. 2. Consumers invalidate.
- No-op / rejection / concurrency / retry: Lagging projection fails closed for risky features.
- UI states: None.
- Rules: Product §44, §45 BIL-016, §88; spec BILREQ034, BILREQ088.
- Acceptance: Given a downgrade, when the fact arrives, then a cached decision is dropped.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No cache exists; reads are live.

## V. BIL-022 Billing observability

### UC-BIL-V01 Trace one billing operation end to end
- Capability: V. BIL-022 Billing observability
- Actors and permission: Engineers on call.
- Preconditions: An operation ran.
- Input: Operation or correlation id.
- Output: One trace: request, provider effect, webhook, settlement.
- Main flow: 1. Id flows through logs and metrics. 2. Engineer searches.
- No-op / rejection / concurrency / retry: No secrets or card data in logs.
- UI states: None.
- Rules: Spec BILREQ119; team spec section 71, 72.
- Acceptance: Given an operation id, then every step appears in one search.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No operation model yet.

### UC-BIL-V02 Alert on reconciliation lag and entitlement errors
- Capability: V. BIL-022 Billing observability
- Actors and permission: Engineers on call.
- Preconditions: Signals exist.
- Input: Metrics.
- Output: Alert fires on stuck operations or decision errors.
- Main flow: 1. Metrics emitted. 2. Alert rules fire.
- No-op / rejection / concurrency / retry: None.
- UI states: None.
- Rules: Spec BILREQ120, BILREQ121.
- Acceptance: Given a stuck operation, then an alert fires within the set time.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No signals defined.

## W. BIL-023 Migration/backfill

### UC-BIL-W01 Backfill subscriptions, entitlements and lineage
- Capability: W. BIL-023 Migration/backfill
- Actors and permission: System (migration).
- Preconditions: Accounts exist without Billing rows.
- Input: Account ids.
- Output: Free subscription and lineage per account; old plan fields mapped.
- Main flow: 1. Migration reads accounts. 2. Creates rows once. 3. Verifies counts.
- No-op / rejection / concurrency / retry: Re-run creates nothing. Staged expand and contract; no drop before consumers move.
- UI states: None.
- Rules: Product §105; spec BILREQ109, BILREQ113, BILREQ115, BILREQ118.
- Acceptance: Given a re-run, then row counts do not change.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — `Account.PlanCode` migration not planned in code.

### UC-BIL-W02 Replace the hard-coded frontend catalog with the API
- Capability: W. BIL-023 Migration/backfill
- Actors and permission: Frontend team.
- Preconditions: Plan read model exists.
- Input: None.
- Output: Plan names, prices and limits come from the backend.
- Main flow: 1. Remove the constants. 2. Read the catalog from the API.
- No-op / rejection / concurrency / retry: No second copy of plan truth.
- UI states: Loading, error.
- Rules: Product §105; spec BILREQ096, BILREQ111.
- Acceptance: Given a catalog change in the backend, then the page shows it without a frontend release.
- Evidence: BE `GET /api/v1/workspaces/{workspaceId}/billing/plan` (Billing.Plan.Get, `GetWorkspaceBillingPlanQueryHandler`; the plan name comes from the database catalog), `GET /operator/v1/billing/plans` · FE the route's plan card (`billing/billing-api.ts`, `currentName`) and `frontend/apps/operator` read the catalog from the API; the stub `frontend/packages/features/billing/src/web/billing-page.tsx` (constants Free, Pro, Business) and `src/core/types/billing.ts` remain · Design none · Tests `frontend/packages/features/billing/src/__tests__/billing.unit.test.ts`, `frontend/packages/dev/mock-backend/src/__tests__/billing-plan.unit.test.ts` · Real e2e `billing-plan.real.e2e.spec.ts`
- State: IMPLEMENTING — No route or screen uses a hard-coded catalog any more, but the package stub with its story, fixtures and ui-evidence case is still in the tree (removing it is a ui-evidence change, left out of the operator lane).

## X. BIL-024 Hardening/security

### UC-BIL-X01 Keep one account's billing data from another
- Capability: X. BIL-024 Hardening/security
- Actors and permission: System.
- Preconditions: Any billing read or write.
- Input: Account scope.
- Output: Cross-account access returns NotFound.
- Main flow: 1. Pipeline scopes by workspace and account. 2. Database policies back it up. 3. Account-wide counts come from definer functions returning numbers only.
- No-op / rejection / concurrency / retry: Identical NotFound.
- UI states: Permission denied.
- Rules: Product §96; spec BILREQ082-084; RULE tenant isolation (`NRX-003`, `NRX-004`).
- Acceptance: Given a user of account A, when they read account B billing, then NotFound.
- Evidence: BE `Infrastructure/Billing/BillingUsageReader.cs` · FE none · Design none · Tests `backend/tests/Notrelix.Integration.Tests/Rls/BillingUsagePolicyTests.cs` · Real e2e none
- State: IMPLEMENTING — No cross-tenant test per billing endpoint; Billing tables have no catalog, transactional and worker policy split proven.

### UC-BIL-X02 Never touch card data or secrets
- Capability: X. BIL-024 Hardening/security
- Actors and permission: System.
- Preconditions: Any payment flow.
- Input: None.
- Output: Only provider references and safe display data are stored or logged.
- Main flow: 1. Provider-hosted forms collect cards. 2. Logs redact. 3. Tests scan for PAN-like fields.
- No-op / rejection / concurrency / retry: A field that accepts card numbers is a defect.
- UI states: None.
- Rules: Product §64 BIL-023, §65; spec BILREQ077, BILREQ101.
- Acceptance: Given a full payment flow, then no log or table holds a card number.
- Evidence: BE none (no payment code yet; `PaymentMethod` stores brand and last 4 only) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No redaction test.

### UC-BIL-X03 Make commercial commands idempotent and rate-limited
- Capability: X. BIL-024 Hardening/security
- Actors and permission: System.
- Preconditions: Any billing command.
- Input: Idempotency key.
- Output: One effect per key; abuse throttled.
- Main flow: 1. Command carries a key. 2. Duplicate returns the first result. 3. Limiter caps calls.
- No-op / rejection / concurrency / retry: Plan change today has neither.
- UI states: Pending.
- Rules: Product §70 BIL-025; spec BILREQ100, BILREQ105; team spec section 69.
- Acceptance: Given a repeated command with one key, then one effect.
- Evidence: BE partial: capacity actions dedup by logical id; plan change has none · FE `billing/billing-api.ts` sends keys for mock calls · Design none · Tests `backend/tests/Notrelix.Application.Tests/Features/Billing/``BillingCapacityActionsTests.cs` · Real e2e none
- State: IMPLEMENTING — No key on `PUT .../billing/plan`; no limiter.

### UC-BIL-X04 Audit every billing change
- Capability: X. BIL-024 Hardening/security
- Actors and permission: Auditors and billing managers.
- Preconditions: A billing mutation happened.
- Input: Actor, action, scope.
- Output: An audit entry naming who, what and when.
- Main flow: 1. Command writes the audit row. 2. Managers read it.
- No-op / rejection / concurrency / retry: No secrets in entries.
- UI states: Success.
- Rules: Product §96; spec BILREQ100; team spec section 70.
- Acceptance: Given a plan change, then an audit entry names the actor and both plans.
- Evidence: BE none (domain events only) · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Governance audit contract not used by Billing.

### UC-BIL-X05 Coordinate Billing when an account closes
- Capability: X. BIL-024 Hardening/security
- Actors and permission: System (Accounts owns closure).
- Preconditions: Accounts closes an account.
- Input: Account id.
- Output: Subscription canceled, future billing stopped, invoices kept.
- Main flow: 1. Accounts publishes closure. 2. Billing cancels and keeps evidence.
- No-op / rejection / concurrency / retry: Billing never deletes the Account. Repeat is a no-op.
- UI states: None.
- Rules: Product §75, §76 BIL-027; spec BILREQ079.
- Acceptance: Given a closed account, then no new invoice is issued and old ones remain.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — No consumer of the closure fact.

### UC-BIL-X06 Keep commercial evidence after product or account deletion
- Capability: X. BIL-024 Hardening/security
- Actors and permission: System.
- Preconditions: Resources, users or accounts deleted.
- Input: None.
- Output: Invoices, payments and usage evidence stay, minimized.
- Main flow: 1. No cascade into Billing tables. 2. Personal data minimized where law allows.
- No-op / rejection / concurrency / retry: A cascade delete is a defect.
- UI states: None.
- Rules: Product §97, §98 BIL-033, §99; spec BILREQ080, BILREQ081.
- Acceptance: Given a deleted workspace, then its invoices remain readable by the account.
- Evidence: BE none · FE none · Design none · Tests none · Real e2e none
- State: DEFINED — Retention period not decided (Part 4).

## Endpoints with no use case

None. All three Billing endpoints appear in Evidence lines: `GET .../billing/plan` (UC-BIL-A01), `PUT .../billing/plan` (UC-BIL-R01), `GET .../billing/capabilities` (UC-BIL-E02, T01, T04). Routes used only by the mock frontend (listed in 1.4) have no producer and are named in the use cases that need them.

# 4. Product decisions applied

- Product decision (owner, 2026-10-26): Billing options accepted: the artboard's four tiers, account-level scope, no payment provider yet and payments mock-only, overdue blocks new members only and never data, plan change is development-only (superseded 2026-10-31: the platform operator changes the plan, a tenant never does), contact sales is stored as a request. Used by UC-BIL-A05, F01, R01, R11, S02, S04, K01.
- Product decision (owner, 2026-10-26): no billing plan gates in Work Management for now; capability checks stay behind the entitlement port and allow. Used by UC-BIL-F02.
- Lead decision (2026-10-30): Notrelix has three roles for billing today. The account owner may switch plan in development. Workspace Owner and Admin may read billing (interim). Billing managers (owner plus delegates) become the managing role when Governance adds billing actions (UC-BIL-J05).
- Lead decision (2026-10-30): one trial per account per plan family, 14 days, no card required, on conversion without a payment method the account moves to Free (Stripe pause-style behavior, [Stripe test guidance](https://tessl.io/registry/testland/stripe-subscription-billing-test-author)). Used by UC-BIL-B03, B04.
- Lead decision (2026-10-30): past due has a 14-day grace with retries and notices at day 0, 3 and 7; after grace only new members and guests are blocked (matches decision 46). Used by UC-BIL-S01, S02, S05.
- Lead decision (2026-10-30): late usage after period close is applied to the next period and flagged as late; a correction row is available to move it. Used by UC-BIL-H02.
- Lead decision (2026-10-30): invoices, payments and usage evidence are kept 10 years; personal data on them is minimized on user deletion. Used by UC-BIL-X06.
- Lead decision (2026-10-30): seat removal applies at renewal and never below seats in use; credits are not cash. Used by UC-BIL-R12.
- Lead decision (2026-10-30): privileged billing actions (manual override, void or credit, immediate termination, repairs) need a platform operator principal that does not exist. UC-BIL-D02, D03 and R09 were built on that principal 2026-10-31 (owner decision: a separate operator account, its own sign-in and database login, an audit row per action) and are INTEGRATING; UC-BIL-L05 stays BLOCKED (it also needs invoices).

# 5. Canonical updates required

For `docs/product/billing.md`:

- Add a section "Trial": 14 days, no card, one per account per plan family, conversion rule (Part 4).
- Add to "Past due" and "Grace policy": 14-day grace; retries and notices at day 0, 3, 7; after grace only new members and guests are blocked; reads and edits never.
- Add to "Late usage": late facts apply to the next period and are flagged late.
- Add to "Retention": 10-year retention of invoices, payments and usage evidence.
- Add a section "Billing administration roles": account owner, billing managers (delegates), workspace Owner and Admin (interim, read only); one account can have several managers.
- Add a section "Invoice profile": company, tax id, address, email on the Billing Customer; VAT stored on each invoice at issue and never recomputed.
- Add a section "Seats": removal at renewal, not below seats in use; extra seats prorated by days left in the period.
- Add a section "Notices": card expiry, dunning and trial-end notices go to billing managers through Collaboration.
- State the plan tiers: the backend catalog has three (free, pro, enterprise), the artboard four (adds business); the owner must confirm the tier list before the catalog is built.

For `docs/product/governance.md`: add billing actions (view billing, manage plan, manage payment method, export invoices) so Billing stops reusing `ManageWorkspaceSettings`.

# 6. Not covered

| Product-doc section | Reason |
|---|---|
| 1 Mission, 2 Owns, 3 Does not own, 4 Language, 104 Current source alignment, 105 Ambiguity watch, 118 Related owners, 119 Final rule | Orientation and review text; no behavior. Rules live in the cited use cases. |
| 5-8, 13-17 (Plan identity, FeatureCode, limit units, unlimited) | Reached through UC-BIL-A03 and UC-BIL-E01; they are data rules, not separate flows. |
| 9, 10 (Plan lifecycle) | UC-BIL-A03. |
| 11, 12 (Plan price) | UC-BIL-A04. |
| 18-20 (Billing Customer, provider customer ID) | UC-BIL-N01, J04. |
| 35 (Subscription Item) | Used by UC-BIL-R11; no separate user flow. |
| 38-45 (Entitlement scope, source, precedence, cache) | UC-BIL-D01 to D03, E01, U02. |
| 46-58 (Usage) | UC-BIL-G, H, I. Usage versus Analytics (57, 58) is a boundary rule owned by [analytics](../../product/analytics.md); no Billing flow. |
| 77-85 (relations to Workspaces, Governance, Work Management, Automation, Integrations, Documents, Collaboration, Analytics) | Consumer contracts: UC-BIL-F01 to F04, I01, I02, J05, F03. Work Management gate is deliberately absent (decision 108). Integrations connection-count gate has no use case yet because Integrations has not asked for one. |
| 106-110 Change impact; 111-115 Checklists; 116 Testing; 117 Stop conditions | Review aids. Checklist items are covered by the Acceptance lines of the use cases they name; stop conditions are in the Rules lines of UC-BIL-C01, E03, X02, X06. |
| Pause subscription (listed "where supported" in section 22) | Not supported in the product; no use case until the owner asks. |
| Add-on and promo entitlements (`EntitlementSource.AddOn`, `Promo`) | Spec BILREQ134 says these are not exposed as completed use cases. |
| Multi-currency, tax engines, coupons | Not in product docs; not enterprise baseline for v1. |
