# Group 4 digest: Automation, Analytics, Billing, Integrations

Source: docs/product/{automation,analytics,billing,integrations}.md (read fully). Rule IDs quoted verbatim.
Caveat for all four: the docs say state lists are "potential / where approved"; exact enum names live in backend source (not read). Names below are marked EXACT (appear as-written in the doc as current source types) or DOC (doc's candidate vocabulary, lowercase). Do not invent extra states in mockups.
Roles: NONE of the four docs names Owner/Admin/member/billing-owner/Manager roles. They only say "Governance decides who may ...". Treat any role gating as Governance-defined; mock permission-denied states, not specific role names.

---------------------------------------------------------------
## 0. Cross-cutting UI rules (apply everywhere in this group)
- "Not entitled" is NOT "Not authorized" (BIL billing sec.101): two different empty/blocked states with different next actions (upgrade/ask billing admin vs. ask resource admin). Never show every restriction as "upgrade required" (billing sec.100).
- Durable query is authoritative; realtime is a hint (AUT-031, ANA-036, INT-032): every live status needs a refetch/recover path, no permanent "stuck" spinner.
- Never show secrets, stack traces, raw provider payloads, raw card data anywhere (AUT-029, AUT-035, INT-005, BIL-023).
- Every number needs label, unit, period, filter, freshness, comparison basis (ANA-027, analytics sec.76).
- Technical retries/attempts are not user-facing repeated events (AUT-030, INT-010).

---------------------------------------------------------------
## 1. AUTOMATION (owner: automation context)
### Nouns (ubiquitous language)
- Automation Rule: trigger + optional conditions + ordered/defined actions + owner/scope + lifecycle + execution principal. User sees: list of rules, name/description, on/off, config, history.
- Trigger: event | schedule | manual (manual "if supported"). Condition: deterministic predicate. Action: approved typed operation (never arbitrary code, AUT-028).
- Execution: one logical run of one Rule (durable). Action Step: one action attempt/outcome inside an Execution.
- Schedule: intent (one-time, recurring, calendar/interval-based, time zone, DST, missed-fire). Automation Template: reusable creation input (instantiating makes an ordinary Rule).
- Source status types (EXACT names of types): AutomationRuleStatus, AutomationExecutionStatus, ScheduledJobStatus, AutomationTriggerType, AutomationActionType, ScheduleDefinition, ScheduledJob.
### Lifecycle enums
- Rule status (DOC sec.9, "where approved"; source has AutomationRuleStatus): draft / enabled / disabled / archived. Draft may hold incomplete config; enabled must be complete+valid. Archive != disable != delete (hidden from normal management, history retained, no new runs, restorable).
- Execution status (DOC sec.49; "exact statuses are executable evidence"): created/pending, running, succeeded, failed, cancelled; plus optional partial / awaiting-retry. (Frontend scaffold currently uses queued/running/succeeded/failed/cancelled.)
- Action Step result (DOC sec.41): succeeded, failed-terminal, failed-retryable, unknown, skipped.
- Failure-UX states the doc says users must be able to tell apart (sec.106): Rule invalid / Rule disabled / condition false / execution running / action retrying / permission revoked / target deleted / provider unavailable / provider outcome unknown / terminal failed.
- ScheduledJobStatus values not listed in doc.
### Rule IDs with user-visible consequence
- AUT-001 — Rule shows explicit owner, Account/Workspace scope, trigger, conditions, actions, lifecycle, execution principal.
- AUT-002 — Server-side enable validation is authoritative; an invalid/incomplete rule cannot be switched on (toggle must surface the validation error, not just hide the field).
- AUT-003 — Disable stops NEW runs after the effective point; execution history stays visible (disabled rule still has history).
- AUT-004 — Each execution shows which Rule revision/config snapshot it ran; in-flight runs do not switch to an edited rule. (Rule revision != concurrency Version.) Editing an enabled rule must define effect on queued/running/future/scheduled (sec.12) - mock an explicit "applies to future runs only" notice.
- AUT-005 — Event triggers are logical product/integration events (UI shows trigger by product name, not class/table).
- AUT-006 — Automation reacts after the source change commits; UI should not imply the side effect happens inline/instantly in the same action.
- AUT-007 / AUT-008 — Schedule must show time zone + DST behavior ("09:00 every weekday" is incomplete without a time zone); missed-fire policy (skip / fire once / catch up / bounded) is a product choice shown in schedule config.
- AUT-009 / AUT-016 — Duplicate delivery -> one execution; history shows one run per trigger occurrence.
- AUT-010 — Conditions deterministic; missing/inaccessible fact has a defined outcome (false / terminal config error / retryable), not silent fail-open.
- AUT-011 — "Condition evaluated false" is NOT a failure: show as "skipped / not matched", distinct from errors, missing dependency, permission failure.
- AUT-012 / AUT-013 / AUT-028 — Actions are an allow-listed, typed set; each action editor has a schema-validated form (no free JSON/code).
- AUT-014 — Multi-action order/dependency is explicit; retry does not re-run already-committed prior actions.
- AUT-015 — Provider actions (e.g. "send Slack message") depend on an Integration connection; rule editor must point to / require a connection and surface its state.
- AUT-017 — Step/execution only show Succeeded once the target effect is durably known.
- AUT-018 — Retry never repeats a side effect blindly; "unknown outcome" (timeout) is shown as uncertain/reconciling, not auto "retry" (sec.56).
- AUT-019 / AUT-035 — History shows stable automation meaning with an actionable, redacted failure reason + next step; no broker attempts or stack traces.
- AUT-020 — Creating a rule while authorized does not guarantee later authorization; later runs can fail "permission revoked" (visible failure).
- AUT-021 — Execution history != Governance Audit; two separate surfaces/labels.
- AUT-022 — Recursion is bounded (depth/dedup/suppression); loop-prone rules may be rejected or terminated; show causation (source event / source execution / depth) in history. Self-trigger is explicit per policy.
- AUT-023 — Billing supplies limits; Automation decides behavior under them (rule creation blocked / premium triggers disabled / execution paused, history preserved - exact policy not fixed).
- AUT-024 / AUT-025 — Concurrency can't duplicate effects; target context revalidates at action time (a matching condition does not guarantee the action succeeds).
- AUT-026 — Template = creation input only; editing a template later does not change existing rules.
- AUT-027 — "Agents" is NOT a canonical product concept; do not mock an agent/AI-agent surface in the core.
- AUT-029 — Rule config never shows/stores secrets; references a connection by safe identity.
- AUT-030 — Execution facts per logical execution, not per attempt.
- AUT-031 — Execution/rule status recoverable from durable state if realtime missed.
- AUT-032 — Raw provider webhooks never trigger rules directly (only verified Integration facts).
- AUT-033 — No default action that grants roles/permissions/share links; do not offer permission-changing actions in the action picker.
- AUT-034 — Broken reference (deleted field/board/page) must be detected and surfaced; a rule must not stay "enabled" while it can never run -> show Invalid/Broken state with fix prompt. Field deletion may be blocked / affected rules disabled / migration required (sec.104).
### Execution identity / principal
- Doc does NOT fix one model (sec.60). Candidates: original initiating actor, Rule owner, Workspace automation service principal, explicit delegated capability. "Chosen model must be stable and reviewable" -> UI must show "Runs as <principal>". UNRESOLVED which one; do not pick.
- If that principal loses permission (sec.62, AUT-020): future action fails and/or the rule may be disabled "depending on policy"; failure is visible; no stale cached allow. Service principal, if present, is bounded, not system authority (sec.63).
- Permission-management high-risk actions forbidden by default (AUT-033). High-impact rule create/edit/enable/disable may be audited in Governance (sec.64).
### Failure / retry / idempotency to surface
- Run history fields (sec.57): trigger/source, rule revision, condition result, action steps, attempts/outcomes, failure reason, timing, principal, correlation (safe-redacted).
- Retryable (transient network, rate limit, outage) vs terminal (invalid target, invalid config, revoked permission, incompatible field) (sec.54). Partial failure policy (stop on first / continue independent / retry failed steps only / compensate) is a product decision, not fixed; compensation = explicit new action, no magic rollback (sec.53).
- Technical retries must not create repeated user attention/notifications (sec.91).
- No explicit "dead-letter" term in this doc; terminal failed + unknown are the end states.
### Limits
- Possible gates: rule count, execution count, premium trigger/action types, rate limits, recursion depth, per-rule concurrency, workspace quotas (sec.71-72). Over limit/downgrade: may block new rule creation, disable premium triggers/actions, preserve history, pause execution - policy "must be defined" (not fixed in doc).
### Does NOT own (affects screens)
Board/Item/Field (Work Management), Page/Block (Documents), Comment/Notification (Collaboration), provider connection/sync/webhook (Integrations), permissions (Governance), entitlement/limits (Billing), broker/retry infra (Platform). So: connection management lives in Integrations screens; notifications created by automation are Collaboration objects; plan/limit upsell belongs to Billing.

---------------------------------------------------------------
## 2. ANALYTICS / REPORTING (owner: analytics context)
### Nouns
- Metric (versioned business definition; stable key), Dimension, Dashboard (saved presentation config, scoped to owner/tenant), Dashboard Source, Widget (saved visualization on a dashboard: Widget Type + config + position), Reporting Snapshot (captured derived result at a source cutoff), Projection (derived data), Freshness.
- Source type names (EXACT): Dashboard, DashboardSource, DashboardSourceType, DashboardStatus, DashboardVisibility, DashboardWidget, DashboardWidgetType, WidgetConfig, WidgetPosition, WidgetType, ReportingSnapshot, ReportingSnapshotType, ReportSnapshotPayload. No generic Metric registry exists yet (ANA-034).
### Lifecycle enums
- Dashboard (DOC sec.19, "where current product supports it"; source has DashboardStatus): draft/active -> archived -> deleted. DashboardVisibility exists; values not named in doc; sec.90 mentions "default/shared/private" as possible.
- Widget types (DOC sec.25, "exact current types remain executable evidence"): number/KPI, table, bar, line, pie/donut, progress, timeline, workload, text/summary.
- Async report/export generation (DOC sec.102, "if implemented"): queued / running / completed / failed / expired.
- Missing-data meanings (ANA-029): null, zero, unknown/not-applicable, not-yet-projected, source-unavailable, unauthorized are DISTINCT displays.
### Rule IDs with user-visible consequence
- ANA-001 — Dashboards/snapshots are derived, never editable business data.
- ANA-002 — Filter/group/drill-down only changes what's shown; any mutation button routes to the source owner with normal authz (label it as navigation/source command).
- ANA-003 / ANA-028 — Metric version changes can break comparability: show break marker / separate series / migration note; don't draw one continuous trend across incompatible versions.
- ANA-004 — A named metric (e.g. completed-items) has one formula everywhere; no per-widget formulas.
- ANA-005 — Every metric/widget traceable to a source context.
- ANA-006 / ANA-033 — Deleting a dashboard/widget never deletes sources (Boards, Items, Pages, Executions, Billing facts). Delete confirm copy should say only the dashboard config is removed.
- ANA-007 / ANA-024 / ANA-025 — Dashboards are tenant-scoped; cross-tenant analytics is an explicit privileged capability, not for ordinary workspace users.
- ANA-008 — Dashboard visibility/sharing never exposes source resources/fields the viewer cannot read ("widgets never show data the viewer cannot read").
- ANA-009 — Source reference does not make Analytics owner of that data.
- ANA-010 / ANA-032 — Widget type has a validated config (required source/metric, dimensions, filters, time range, sort, top-N, empty behavior); invalid combos (e.g. time series without time dimension, pie without categorical dimension) must be blocked in the editor.
- ANA-011 — Rearranging widgets never reorders source items.
- ANA-012 — Rates define numerator/denominator/window/excluded states/zero-denominator behavior (show explainer for % tiles).
- ANA-013 — Time zone is part of the metric; browser locale must not silently change the number -> show the time zone basis for daily/weekly/monthly.
- ANA-014 / ANA-030 — Show freshness: last updated, source cutoff, projection lag, snapshot time; do not claim "live" for async projections; multi-source reports show completeness/partial state and a consistent cutoff. Stale result may be shown with timestamp, refreshing, or temporarily unavailable (sec.38).
- ANA-015 / ANA-016 / ANA-038 — Snapshot is captured history (generated time, source cutoff, schema version); read-only; a snapshot can legitimately differ from today's recomputation (show "as reported on <date>" vs "current").
- ANA-017 — Source deletion handled by privacy policy (recompute / anonymize / retain / purge), not cascade; do not promise reports vanish when data is deleted.
- ANA-018 / ANA-019 / ANA-020 / ANA-021 — Projection internals (rebuild/backfill/idempotent/no full JSON scans); UI implication: rebuild/backfill states may appear as "recalculating/partial" and must not double-count.
- ANA-022 — Analytics cannot charge/alter subscriptions, entitlements, invoices, usage ledger (Billing widgets are read-only reports).
- ANA-023 / ANA-031 — Aggregate visibility != detail visibility; small-group suppression/anonymization may apply (sec.67); drill-down re-checks source authz, so a clickable KPI can lead to an "access denied/limited" state.
- ANA-026 — Export (CSV/PDF) uses same formula and authz as screen; protected operation; generated artifacts have explicit retention/download authorization.
- ANA-027 — A precise number needs defined numerator/denominator/period/freshness (tooltip/metadata affordance).
- ANA-029 — see missing-data meanings above.
- ANA-034 — Metric engine may be ahead of implementation; mockups must not assume a generic metric builder exists.
- ANA-035 — No operational telemetry (CPU, HTTP latency, queue depth) as customer metrics.
- ANA-036 — Realtime tile updates reconcilable; refetch on gaps.
- ANA-037 — Derived data obeys source tenant/privacy/data-location obligations.
### Who may do what
- Dashboard edits = Analytics configuration; viewing/sharing = Governance-backed (sec.90). Export, drilldown, admin scope follow Governance (sec.65). No named roles.
### Failure/retry
- Report generation async states (queued/running/completed/failed/expired) if implemented; no retry/dead-letter semantics defined. Partial/delayed source -> surface "incomplete", not zero.
### Limits
- Analytics itself has no limit rules; "advanced-analytics" is an example FeatureCode in Billing (billing sec.13). Behavior when not entitled: Billing-defined.
### Does NOT own
Board/Item/Field truth, Page/Block, Comment/Notification, Automation Execution truth, Integration Connection/Sync truth, Plan/Subscription/Entitlement/Usage truth, resource permission. Automation/Integrations/Billing "health" widgets are read-only views; deep links go to the owner's screen (sec.85: "open filtered Board", "open failed Automation", "manage Integration").
- Scheduled reports (if supported, sec.75): report def, scope, recipient, cutoff, delivery (delivery belongs to Collaboration/Platform).

---------------------------------------------------------------
## 3. BILLING (owner: billing context)
### Nouns
- Billing Customer (commercial identity mapped to an Account; not the Account), Plan (GLOBAL catalog), Plan Price, Plan Limit, FeatureCode (stable id e.g. automation, advanced-analytics, premium-integration, workspace-count, member-count, storage), Subscription (+ SubscriptionItem, SubscriptionTier), Entitlement (source, status, target scope), Usage Metric / Usage Ledger / UsagePeriod / WorkspaceFeatureUsage, Invoice (+ line items), Payment Method (safe metadata only: provider ref, brand, last4, expiry display, status).
- Scope: subscription/customer is ACCOUNT-level; Workspace is an entitlement target or usage source (BIL-007, sec.77). Note frontend scaffold currently models Subscription.workspaceId and a "workspace subscription" page -> conflicts with Account-scoped commercial model; flag to designer.
### Lifecycle enums
- Plan status (EXACT type PlanStatus; DOC sec.9): active / deprecated / archived.
- Subscription status (DOC BIL-008; source type SubscriptionStatus): trialing / active / past-due / cancel-at-period-end / canceled / expired / paused (where supported). Provider states are mapped, never shown raw.
- Entitlement status (DOC sec.43; EntitlementStatus): active / inactive / expired / suspended. Entitlement source (DOC sec.40): plan, promotional override, trial, manual enterprise contract, migration.
- Invoice status (DOC sec.62; InvoiceStatus): draft / open / paid / void / uncollectible (or canonical equivalents). (Frontend scaffold uses paid/pending/failed - differs.)
- PaymentMethodStatus exists; values not listed.
- Uncertain provider outcome (BIL-026): pending / reconciling / unknown provider result.
- Billing periods: BillingPeriod type exists (values not listed).
### Rule IDs with user-visible consequence
- BIL-001 — Plans are a global catalog (one plan page for all, not per-workspace).
- BIL-002 — Gating is by entitlement, not plan name; UI guards must come from entitlement queries.
- BIL-003 — A plan can be deprecated (not sold) while existing subs continue; hide/mark deprecated plans in the picker but keep showing them on current subscriptions.
- BIL-004 — Past invoices/periods remain interpretable after price changes (grandfathered/historical pricing display).
- BIL-005 — FeatureCode is stable vocabulary (labels are separate, localizable).
- BIL-006 — Every limit shows unit/scope/period; zero means disabled vs zero capacity; "unlimited" is a distinct representation (not -1).
- BIL-007 — Billing Customer != Account.
- BIL-008 — Show canonical subscription state.
- BIL-009 — Provider webhook does not directly set state (UI may lag behind provider; show pending).
- BIL-010 — Cancellation requested vs effective are different: show "Cancels on <date>" while still active.
- BIL-011 — Payment failure/past-due never corrupts or deletes existing work; may restrict FUTURE capability under an explicit bounded grace policy.
- BIL-012 — Downgrade is NON-DESTRUCTIVE by default: block new creation, make excess resources read-only, pause premium automation, stop new provider sync, scheduled remediation, explicit admin action; never auto-delete boards/docs.
- BIL-013 — Entitlement answers: feature available? what limit? what usage remains? which source/version?
- BIL-014 — Entitlement scope != authorization scope (commercially available does not mean you may use/manage it).
- BIL-015 / BIL-016 — Entitlement resolution deterministic; cached decisions must refresh on subscription change, override, usage exhaustion, trial expiry.
- BIL-017 — Billable usage is not operational telemetry (no "API requests" meters unless mapped).
- BIL-018 — Usage ingestion idempotent (no double consumption).
- BIL-019 — Usage totals reproducible from ledger + explicit corrections (corrections shown as reversal/adjustment/credit, history preserved).
- BIL-020 — Period reset does not erase history (usage history by period viewable).
- BIL-021 — Billing usage != Analytics metric.
- BIL-022 — Issued invoices are evidence; corrections via credit/adjustment/replacement/void, not edits.
- BIL-023 — No raw PAN/CVC/bank credentials ever; payment collection via provider UI/tokenization; show only brand/last4/expiry/status.
- BIL-024 — Provider ids are references only (not shown as primary identity).
- BIL-025 — Duplicate callbacks/checkout don't create duplicate subs/invoices/entitlement changes/charges/payments.
- BIL-026 — Show pending/reconciling for uncertain outcomes; do not display final success before reconciliation (sec.103).
- BIL-027 — Account closure coordinates billing (cancel sub, stop billing, retain invoices, resolve credits); Accounts owns the closure.
- BIL-028 — Billing administration is separately authorized: benefiting from a paid feature does not grant viewing invoices or changing subscription.
- BIL-029 — Public billing events are translated, not provider payloads.
- BIL-030 — If entitlement can't be determined, do NOT default to unlimited/paid access unless an explicit bounded grace applies.
- BIL-031 — Usage recorded only from successful operations (failed/rolled-back op consumes no quota).
- BIL-032 — Hard quota concurrency handled by check/reserve/commit-or-release; UI can see transient "limit reached" even when count looked lower.
- BIL-033 — Invoice/payment/usage evidence retained independent of product/account deletion.
### Who may do what
- Governance decides who may: view billing, update plan, manage payment method, cancel subscription, export invoices (sec.78). Entitlement never authorizes administration (BIL-028). Billing surfaces may have stricter access/audit than ordinary workspace content (sec.96). No named roles ("billing owner" is not a term in the doc).
### Failure/retry/idempotency
- Webhooks signature-verified, replay-protected, idempotent, out-of-order tolerant (sec.69-71). Provider outage: keep prior accepted state, degrade per explicit policy (sec.72). Checkout/change sessions have stable operation identity. UI: pending/reconciling states, no duplicate-click effects.
### Limits / trial / past-due / cancel / downgrade
- Limits govern FUTURE operations: cannot create more Workspaces / add members / run new premium automation / connect another integration (sec.91). Over-limit existing data: separate safe policy (read-only/pause), not deletion.
- Trial must define start, end, eligible capabilities, conversion, expiry, payment requirement, repeat-trial policy (sec.28) - exact values NOT in doc. Trial is not just an Account status (sec.105: AccountStatus.Trialing != subscription lifecycle).
- Past-due: explicit grace/degradation; do not destroy work on first failure (sec.29). Grace must be explicit and bounded (sec.90), values not specified.
- Upgrade: expands entitlements, does not bypass Governance. Upgrade/downgrade must define effective time, proration, entitlement timing, usage-period implications, existing-resource policy, visible state (sec.31).
- Downgrade UX must communicate (sec.102): effective time; capabilities becoming unavailable; current over-limit resources; what becomes read-only/paused; what needs admin action.
- Billing UX must separate (sec.100): current Plan, Subscription status, effective next change, entitlement, usage/limit, payment issue, invoice, authorization.
### Does NOT own
Account lifecycle/enterprise admin, Workspace lifecycle/membership, resource permissions, product-resource state, provider secrets/webhook transport, authentication. Billing cannot delete/archive Boards/Items to enforce downgrade (sec.80).

---------------------------------------------------------------
## 4. INTEGRATIONS (owner: integrations context)
### Nouns
- Integration Connection (durable relationship between a Notrelix scope and external provider account/tenant/capability), Provider, Provider Credential Reference (secret never shown), Webhook Subscription, Webhook Delivery (technical, not a product event), Sync Cursor, Sync Direction, Mapping/Fingerprint, Provider Operation, CalendarIntegration / CalendarEvent (provider mapping, NOT the Work Management Calendar view, INT-023).
- Source types (EXACT): IntegrationConnection, IntegrationConnectionStatus, IntegrationProvider, IntegrationSyncCursor, SyncStatus, WebhookSubscription, WebhookDelivery, WebhookDeliveryStatus, WebhookSecretHash, CalendarIntegration, CalendarEvent, CalendarProvider, CalendarSyncDirection, CalendarSyncFingerprint.
- Provider examples cited: Google Calendar, Slack, GitHub (frontend scaffold lists slack/github/jira/linear/figma/custom).
### Lifecycle enums (kept as TWO separate statuses, INT-015)
- Connection state (DOC INT-002 "as applicable"; source IntegrationConnectionStatus): created/pending authorization / active / expired (refresh-required) / degraded-error / disconnected-revoked / reconnecting. Token-level: valid / refreshable / requires user reauthorization / permanently revoked (sec.18).
- Sync health (DOC sec.42, "where useful"; source SyncStatus): idle/healthy / syncing / degraded / rate-limited / failed / reauthorization-required / resync-required.
- Provider operation outcome (DOC sec.54): succeeded / failed-retryable / failed-terminal / unknown / rate-limited / reauthorization-required / conflict.
- WebhookDeliveryStatus values not listed. Calendar direction (sec.68): export-only / import-only / bidirectional (per-field authority still explicit).
- Scope (INT-003): Account-scoped | Workspace-scoped | resource-scoped; must be an explicit choice in the connect flow, not inferred from where OAuth started.
### Rule IDs with user-visible consequence
- INT-001 — Provider SDK types/brand branching stay out of product UI semantics; provider differences via capability config (show only capabilities the provider supports, sec.110: webhooks, incremental cursor, idempotency key, two-way, delete, attachments, recurrence).
- INT-002 — Connection shows multi-state lifecycle, never a boolean "Connected".
- INT-003 — Choose scope explicitly when connecting.
- INT-004 — Provider OAuth consent does not bypass Governance: user must also be allowed to attach/disconnect/reconfigure at that scope (permission-denied state after OAuth).
- INT-005 — Secrets never shown/echoed (no reveal of tokens/webhook secrets in UI, logs, activity).
- INT-006 — Token refresh failure != connection deleted; revoked grant -> "reauthorization required"/disconnected.
- INT-007 / INT-008 — Webhooks authenticated before business processing; tenant derived from verified connection, not payload (UI implication: rejected/quarantined deliveries are not user "events").
- INT-009 / INT-010 — Inbound processing idempotent; delivery attempts are not repeated product changes (don't show attempt count as repeated edits).
- INT-011 — Sync direction + authority per object/field must be defined ("two-way" alone is not enough): mapping UI shows who is authoritative per field.
- INT-012 — Provider never silently owns Notrelix state; provider changes flow through mapping + target-context ops.
- INT-013 / INT-014 — Cursor is progress, advances only after durable processing; rebuilding cursor may mean resync/backfill (show "resync required").
- INT-015 — Connection status and Sync health are separate indicators (credential valid while sync failing; sync idle while connected).
- INT-016 — Mapping keeps both identities (show provider object link + Notrelix resource).
- INT-017 — Disconnect does NOT auto-delete business resources; copy must state what remains. Disconnect also: disables secret, removes webhooks, stops future sync, decides queued ops (cancel / fail terminal / pause awaiting reauth), mapping retention (sec.97-103; INT-033).
- INT-018 — Provider deletion != Notrelix deletion; outcomes: delete / archive / unlink / mark missing-conflicted / ignore, per mapping.
- INT-019 / INT-020 — Outbound ops have stable ids; UNKNOWN outcome is reconciled (not blindly retried) -> show "outcome unknown / reconciling".
- INT-021 — Rate limiting is a bounded, observable state (backoff), not a permanent connection failure; show "rate-limited, retrying" not "failed".
- INT-022 — Conflict policy explicit: Notrelix wins / provider wins / revision-aware merge / field-specific authority / manual resolution / explicit last-write-wins. Conflict UI must show which integration/resource, which fields, provider state, Notrelix state, resolution options (sec.64).
- INT-023 / INT-024 — Calendar sync maps selected temporal facts; keep date-only vs instant vs local datetime vs time zone vs all-day vs recurrence distinct (no silent date->UTC). Infinite recurrence not expanded into items by default (sec.71).
- INT-025 — External attendee/provider user never auto-creates workspace membership/access.
- INT-026 — Provider payload untrusted even when signed (sanitize rich content).
- INT-027 — Automation retries do not replace integration idempotency (two layers).
- INT-028 — Integrations writes via normal Work Management use cases, not directly.
- INT-029 — Background integration principal is bounded (works only in mapped/configured resources); if product authorization changes, sync may fail / need remap / disable (sec.85) -> show "lost access to <target>" state.
- INT-030 — Downgrade may PAUSE/DISABLE premium sync but keeps connection/mapping/history; don't show as deleted.
- INT-031 / INT-032 — Public events translated; realtime progress recoverable by refetching durable state.
- INT-033 — Disconnect is a lifecycle operation, not just secret deletion.
- INT-034 — Provider-held copies are a separate deletion responsibility (required / best-effort / user-managed / impossible after disconnect) -> disclosure copy on delete/disconnect.
- INT-035 — Least-privilege provider scopes; adding a feature needing new provider scope requires explicit re-consent (show "re-authorization needed", never fail silently, sec.109).
- INT-036 — Capability limitations shown per provider, not special-cased in product domains.
- INT-037 — Provider config typed/versioned.
- INT-038 — Service extraction does not change the product context.
### Who may do what
- Create / disconnect / reconfigure / map / pick target resources = Governance-protected at the connection's Account/Workspace/resource scope (sec.13, sec.83). Account/Governance policy may restrict which providers are allowed (data-residency, sec.106). Enterprise IdP/SCIM is Accounts+Identity, NOT Integrations (sec.87); user-login OAuth is Identity (sec.86). No named roles.
### Failure/retry/idempotency to surface
- Invalid signature / unsupported schema / impossible mapping = poison: quarantined per provider delivery + connection, never blindly retried (sec.29).
- Retry: provider-aware backoff (retry-after, quota window); hot-loop retry forbidden.
- Connection/sync failures may create Collaboration notification/activity for relevant users; delivery attempts are not user Activity (sec.90).
- Reconnect/reauthorize preserves logical connection + mappings when provider account is the same; if provider account identity differs -> explicit confirmation or a NEW connection, never silent relink (sec.99-100). Duplicate/cross-account linking detected via stable provider account id.
### Limits
- Billing may gate: number of connections, premium providers, sync frequency, advanced capabilities (sec.88). On downgrade: pause/disable premium sync, retain connection/mapping/history (INT-030).
### Does NOT own
User login/session/MFA (Identity), enterprise IdP/SCIM (Accounts+Identity), Board/Item/Field (Work Management), Page/Block (Documents), Automation Rule/Execution (Automation), Notification/Activity (Collaboration), resource authorization (Governance), broker/retry transport (Platform). So: automations list lives in Automation screens even when they use Slack; search indexes imported resources via owners; Work Management Calendar view is separate from Calendar Integration.

---------------------------------------------------------------
## 5. Shipped frontend (as of this read; paths under /Users/nqvinh/Documents/projects/todo-app/frontend)
No *.copy.ts / vi i18n strings exist for any of the four areas. Existing *.copy.ts files are only: features/notifications (notification-bell), features/account (profile/appearance/security/notifications/nav), features/workspace (settings/home/shell/spaces/library/teams/members), product/work-management (board-header, kanban-card). i18n foundation: packages/foundation/i18n/src.
- Only vi string touching these areas: packages/features/workspace/src/web/shell/workspace-shell.copy.ts line 33 `billing: "Billing"` / line 191 `billing: "Thanh toán"` (nav label). Route: apps/web/src/router/routes/billing.routes.tsx -> path /billing under workspace; apps/web/src/routes/workspaces/$workspaceId/billing.tsx; nav mapping apps/web/src/shell/workspace/workspace-destinations.ts (section "billing"). No automation/integrations/analytics/reports/dashboard routes or nav.
- BILLING: packages/features/billing/src/{core/types/billing.ts, core/query/keys.ts, web/billing-page.tsx, web/billing-page.stories.tsx, verification/billing-ui-fixtures.ts, web/__tests__/...}; verification/ui-evidence.manifest.json. UI is an English-only placeholder, hard-coded strings (no copy.ts, no vi): title "Billing", "Manage your workspace subscription and billing.", "Current Plan", "Upgrade Plan", plans Free $0 / Pro $12 / Business $29 (per member/month) with feature bullets ("Up to 5 members, 3 boards, 1 GB storage, Basic views"; "Unlimited members/boards, 10 GB, All views, Priority support"; "Everything in Pro, 100 GB, Advanced permissions, Audit log, SAML SSO"); buttons "Current plan" / "Upgrade" / "Downgrade" (all disabled). Types: PlanTier free|pro|business|enterprise; Subscription.status active|canceled|past_due|trialing, workspaceId-scoped; Invoice.status paid|pending|failed; Entitlement{feature,limit,used}. Conflicts with docs: plan-name based, workspace-scoped, no cancel-at-period-end/expired/paused, hard-coded plan names (BIL-002 smell).
- AUTOMATION: packages/product/automation/{core,state,web,mobile,testing}. Web: web/src/components/automations-tab.tsx (+ .stories.tsx, interaction test, verification/ui-evidence.manifest.json surface "automation.rules.tab"). English-only: heading "Automation Rules", "Define trigger-action workflows to reduce repetitive tasks.", rule row (name+description+Switch aria "Toggle automation rule <name>"), dashed button "Create custom automation rule"; demo rules: 'When card status goes to "Done"' (Archive the card and remove all member assignments automatically.), "When card has urgent priority" (Notify the workspace owner instantly via email.). Mobile: mobile/src/components/mobile-automation-screen.tsx (title only, unstyled placeholder). Types (core/src/types/index.ts): AutomationTriggerType card_status_changed|card_priority_changed|card_created|card_deleted|field_updated; AutomationActionType archive_card|remove_members|notify_owner|send_email|move_to_group|set_field_value; AutomationRule.isEnabled boolean only (no draft/archived); AutomationRun/Execution status queued|running|succeeded|failed|cancelled; AutomationExecutionStep; AutomationTemplate; rules are board-scoped, ONE trigger + ONE action; uses legacy "card" vocabulary (docs say BoardItem). State pkg has query keys, rule commands, realtime execution-adapter.
- INTEGRATIONS: packages/features/integrations/src/{core/types/integrations.ts, core/api/integrations.service.ts, core/query/keys.ts, web/query/hooks/use-integrations.ts}. No UI components at all. Types: IntegrationProvider slack|github|jira|linear|figma|custom; ConnectionStatus connected|disconnected|error|pending (the single-boolean-ish status INT-002/INT-015 warn against); Webhook{url,events,isActive,secret?} (carries secret - conflicts with INT-005).
- ANALYTICS / REPORTS / DASHBOARD: none found (no dirs or files in frontend/packages; features/ = account, activity, auth, billing, collaboration, governance, integrations, notifications, search, workspace; product/ = automation, docs, work-management).
