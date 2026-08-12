---
document_id: PROD-INTEGRATIONS
document_type: product-context
status: active
owner: integrations
applies_to:
  - integrations
  - provider-connections
  - provider-sync
  - webhooks
  - calendar-integrations
  - provider-mappings
  - external-side-effects
evidence:
  - PRODUCT.md
  - docs/product/product-model.md
  - docs/product/product-experience.md
  - docs/product/contexts/accounts.md
  - docs/product/contexts/identity.md
  - docs/product/contexts/governance.md
  - docs/product/contexts/work-management.md
  - docs/product/contexts/documents.md
  - docs/product/contexts/automation.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/contract-boundaries.md
  - backend/src/Notrelix.Domain/Integrations/
  - backend/tests/
  - frontend/packages/features/integrations/
review_on:
  - integration-connection-model-change
  - provider-adapter-change
  - webhook-contract-change
  - sync-model-change
  - mapping-or-conflict-policy-change
  - calendar-integration-change
  - provider-secret-model-change
  - provider-rate-limit-policy-change
  - integration-deletion-or-retention-change
  - provider-operation-idempotency-change
---

# Integrations Context

> **Integrations owns the product boundary between Notrelix and external providers: provider connection lifecycle, provider-safe credential references, inbound webhook intake, external-to-Notrelix mapping, outbound provider operations, synchronization state, and conflict/reconciliation semantics.**
>
> It translates external systems into stable Notrelix product contracts without leaking provider SDK models or secrets into product domains.

This document is the canonical product owner for Integrations semantics.

Identity may use OAuth for user authentication, but external business-provider connections belong here.

Automation may invoke provider-oriented actions, but provider connection and provider-side effect semantics remain Integrations-owned.

---

# 1. Mission

Integrations allows Notrelix to connect to external systems while preserving product ownership.

The context exists to answer:

```text
Which provider connection exists?
What external identity/resource does it map to?
How is the provider authenticated?
Which Notrelix resource/fact is authoritative?
How does sync progress?
What happens on conflict, duplicate, retry, deletion, or unknown provider outcome?
```

---

# 2. Owns

Integrations owns product semantics for:

```text
IntegrationConnection
provider identity/capability metadata
provider-secret reference
webhook subscription/delivery identity
inbound provider-event dedup
sync cursor/status
provider mapping/fingerprint/revision
outbound provider operation identity
sync direction
provider-specific anti-corruption mapping
disconnect/reconnect lifecycle
integration health
```

Current source contains `Calendar`, `Connections`, `Sync`, and `Webhooks`.

---

# 3. Does not own

```text
user login/session/MFA
→ Identity

Account enterprise IdP/SCIM administration
→ Accounts + Identity

Board/Item/Field
→ Work Management

Page/Block
→ Documents

Automation Rule/Execution
→ Automation

Notification/Activity
→ Collaboration

resource authorization
→ Governance

message broker/retry transport
→ Platform
```

---

# 4. Ubiquitous language

**Integration Connection** — durable product relationship between a Notrelix scope and an external provider account/tenant/capability.

**Provider** — external system type or adapter family.

**Provider Credential Reference** — safe identifier for secret material stored in approved secret infrastructure.

**Webhook Subscription** — configured provider callback relationship where applicable.

**Webhook Delivery** — one inbound provider delivery attempt/identity.

**Sync Cursor** — durable progress marker for incremental provider synchronization.

**Sync Direction** — authoritative direction semantics for a mapping.

**Mapping/Fingerprint** — stable relationship/revision identity between provider object and Notrelix object.

**Provider Operation** — one outbound logical effect attempted against provider.

---

# 5. INT-001 — Provider models are anti-corruption boundaries

External provider types must be mapped into explicit Integration DTOs and Notrelix product commands/facts.

Product Domains MUST NOT:

- import provider SDK types;
- branch on provider brand;
- store raw provider object as business model.

---

# 6. Provider-specific adapters

Provider-specific differences belong in Integration adapters/capability configuration.

Examples:

```text
Google Calendar recurrence/revision
Slack channel/message constraints
GitHub issue identity
provider rate-limit semantics
```

must not distort core Work Management/Documents semantics.

---

# 7. Integration Connection

Current source includes `IntegrationConnection`, `IntegrationConnectionStatus`, and `IntegrationProvider`.

A Connection is a durable product object representing an approved external integration relationship.

---

# 8. INT-002 — Connection lifecycle is explicit

Connection lifecycle must distinguish as applicable:

```text
created/pending authorization
active
expired/refresh-required
degraded/error
disconnected/revoked
reconnecting
```

Do not reduce every provider state to one boolean `IsConnected`.

---

# 9. Logical connection identity

Connection identity remains stable across:

- token refresh;
- secret rotation;
- transient provider error;
- display-name change.

Reauthorization should not accidentally create duplicate logical connection when the provider relationship is the same.

---

# 10. Provider account identity

A Connection should retain the stable external provider tenant/account/subject identity needed to detect accidental duplicate/cross-account linking.

Email/display name alone is insufficient where provider exposes stable IDs.

---

# 11. INT-003 — Connection scope is explicit

A Connection may be:

```text
Account-scoped
Workspace-scoped
resource-scoped
```

according to product semantics.

Scope must be explicit and cannot be inferred from whichever screen initiated OAuth.

---

# 12. Account versus Workspace connection

Some provider connections are organization-wide.

Some apply only to one Workspace/resource.

Do not force all integrations into Workspace scope or Account scope by implementation convenience.

---

# 13. Authorization to connect

Creating/disconnecting/reconfiguring a Connection requires Governance authorization appropriate to its Account/Workspace/resource scope.

Provider consent alone is not Notrelix authorization.

---

# 14. INT-004 — Provider consent does not bypass Governance

A valid provider OAuth callback may prove provider authorization.

It does not prove the current Notrelix principal may attach that provider to an Account/Workspace/resource.

---

# 15. Secret material

External credentials may include:

```text
OAuth access/refresh tokens
API keys
webhook secrets
provider signing secrets
service-account secrets
```

These are security-sensitive.

---

# 16. INT-005 — Provider secrets are isolated

Reusable secret material is encrypted/stored through approved secret/provider infrastructure and referenced by safe connection/secret identity.

Secrets MUST NOT appear in:

- Domain events;
- frontend contracts/state;
- ordinary config payloads;
- logs;
- Activity;
- Analytics.

---

# 17. Secret rotation

Rotation updates secret material while preserving logical Connection identity where appropriate.

Rotation must not create two independent active connections accidentally.

---

# 18. Token expiry/refresh

Provider refresh is infrastructure/application behavior.

Product Connection state should reflect when authorization is:

- valid;
- refreshable;
- requires user reauthorization;
- permanently revoked.

---

# 19. INT-006 — Secret validity and Connection product state are related but distinct

A transient token refresh failure does not automatically mean the logical Connection is deleted.

A revoked provider grant may require transition to reauthorization/disconnected state.

---

# 20. Webhooks

Current source has first-class:

```text
WebhookSubscription
WebhookDelivery
WebhookDeliveryStatus
WebhookSecretHash
```

Webhook intake is a security and idempotency boundary before product mutation.

---

# 21. INT-007 — Webhook is authenticated before business processing

Canonical intake:

```text
raw HTTP request
→ size/content bounds
→ signature/secret/timestamp/replay verification
→ resolve trusted Connection
→ derive trusted Account/Workspace scope
→ persist/deduplicate provider delivery identity
→ async translation/business processing
```

---

# 22. Raw request requirement

Provider signature verification may require raw body bytes/headers.

Do not deserialize and mutate business state before authenticity is verified.

---

# 23. Tenant routing

Provider payload may contain Workspace/account-like values.

Those values are untrusted until resolved through the verified Connection/mapping.

---

# 24. INT-008 — Webhook tenant routing never trusts provider JSON alone

The verified Connection is the primary bridge from provider identity to Notrelix scope.

A spoofed payload field cannot route mutations to another tenant.

---

# 25. Webhook replay

Providers often retry.

A malicious actor may replay.

Webhook processing requires a stable inbound identity such as:

```text
provider event ID
delivery ID
provider object revision + event kind
or safe derived identity
```

scoped by Connection/provider.

---

# 26. INT-009 — Inbound provider processing is idempotent

Duplicate delivery of one logical provider event MUST NOT duplicate:

- Item creation;
- Comment creation;
- Calendar event;
- Notification;
- Automation trigger;
- mapping.

---

# 27. Webhook Delivery

WebhookDelivery represents technical/provider delivery identity/status.

It is not itself the Notrelix product event.

After verification/translation, Integrations emits/executes stable Notrelix semantics.

---

# 28. INT-010 — Provider delivery identity and product fact identity are distinct

Several provider deliveries may resolve to one logical Notrelix fact under retry.

Do not expose technical attempt count as repeated product changes.

---

# 29. Poison/invalid webhook

Invalid signature, unsupported schema, impossible mapping, or permanently incompatible version must not be retried blindly.

Quarantine by concrete provider delivery + consumer/connection identity.

---

# 30. Provider webhook schema evolution

Adapters must tolerate supported provider versions and fail clearly on unsupported ones.

Provider version change must not silently reinterpret Notrelix product facts.

---

# 31. Sync

Current source has `IntegrationSyncCursor` and `SyncStatus`.

Sync represents durable progress/reconciliation between Notrelix and provider.

---

# 32. INT-011 — Sync has explicit direction and authority

For each synchronized object/field, define:

```text
Notrelix authoritative
provider authoritative
two-way with explicit conflict rule
derived/read-only provider projection
```

“Two-way sync” is not a complete product contract.

---

# 33. Sync direction

Direction may be:

```text
Notrelix → Provider
Provider → Notrelix
Bidirectional
```

but field-level exceptions can still exist.

A one-word direction enum cannot hide contradictory per-field semantics.

---

# 34. Source of truth

Examples:

```text
Work Management Item title
→ Notrelix authoritative for a one-way provider mirror

external calendar invite attendance
→ provider authoritative where product says so
```

Ownership is defined per mapping semantics.

---

# 35. INT-012 — External provider never silently becomes owner of Notrelix Domain state

A provider change mutates Notrelix only through an approved Integration mapping + target-context operation.

Provider SDK objects are not persisted as target aggregate state blindly.

---

# 36. Sync Cursor

Cursor tracks incremental progress such as:

- provider page token;
- timestamp/revision boundary;
- sequence/token.

It is Integrations sync state, not product resource identity.

---

# 37. INT-013 — Sync cursor is progress state, not business truth

Losing/rebuilding cursor may require replay/backfill.

It must not redefine the authoritative provider/Notrelix object state.

---

# 38. Cursor advancement

Advance only after the corresponding provider batch/items are durably processed according to sync contract.

---

# 39. INT-014 — Sync progress advances after successful durable processing

Do not move cursor before target mutations/mappings commit.

Otherwise failed provider records can be skipped permanently.

---

# 40. Backfill

Initial/full sync may need:

```text
pagination
checkpointing
rate limiting
dedup
partial failure
restart
progress
```

It must be restart-safe.

---

# 41. Incremental sync

Incremental sync uses provider revision/cursor semantics and must handle:

- duplicate;
- reordered update;
- missing old history;
- token expiration requiring full resync.

---

# 42. Sync status

Product-visible sync health can distinguish:

```text
idle/healthy
syncing
degraded
rate-limited
failed
reauthorization-required
resync-required
```

where useful.

---

# 43. INT-015 — Connection state and Sync health are not one status

A provider credential can be valid while sync is failing.

A sync can be idle while connection is active.

Do not overload one `ConnectionStatus` for every operational fact.

---

# 44. Mapping

Integrations maintains stable mapping between external resource identity and Notrelix target identity.

A mapping may include:

```text
Connection
provider object ID
Notrelix resource ID
provider revision/etag
sync direction
fingerprint
mapping version
```

---

# 45. INT-016 — Mapping preserves both identities

Never replace Notrelix stable ID with provider object ID or vice versa.

Each system keeps its own authoritative identity.

---

# 46. Mapping uniqueness

One provider object should not map to multiple Notrelix resources accidentally under the same logical mapping unless product explicitly supports fan-out.

---

# 47. Cross-Connection mapping

Provider object identity must be scoped by Connection/provider tenant.

`event-123` or `calendar-1` is not globally unique by assumption.

---

# 48. Mapping deletion

If mapping is removed:

- Notrelix resource may remain;
- provider resource may remain;
- future sync may stop.

Mapping lifecycle is separate from source resource lifecycle.

---

# 49. INT-017 — Disconnect does not automatically delete business resources

Disconnect/revoke primarily:

- disables secret access;
- stops future sync/webhook work;
- removes/invalidates subscriptions;
- transitions mapping behavior.

Whether Notrelix-created/provider-created data remains is explicit per integration.

---

# 50. External deletion

Provider deletion may mean:

```text
delete mapped Notrelix resource
archive it
unlink only
mark missing/conflicted
ignore
```

depending on mapping authority.

---

# 51. INT-018 — Provider deletion does not automatically equal Notrelix deletion

Deletion mapping is explicit per resource/provider direction.

Do not mirror destructive operations blindly.

---

# 52. Outbound provider operation

Outbound sync creates a logical provider effect.

Examples:

```text
create/update/delete provider task/event
send message
create issue
update calendar event
```

---

# 53. INT-019 — Outbound operations have stable logical identity

Retryable external creates/updates need:

```text
local operation ID
Connection
target provider capability
provider correlation/idempotency key where supported
current attempt state
```

---

# 54. Provider result taxonomy

Outcome can be:

```text
succeeded
failed-retryable
failed-terminal
unknown
rate-limited
reauthorization-required
conflict
```

Do not reduce every non-2xx to one generic failure.

---

# 55. Unknown outcome

Timeout/network disconnect may happen after provider committed.

Unknown is first-class.

---

# 56. INT-020 — Unknown provider outcome is reconciled, not blindly retried

Use:

- provider idempotency key;
- correlation lookup;
- fetch/reconcile;
- mapping fingerprint/revision

where available.

---

# 57. Retry

Retry respects provider semantics and target operation idempotency.

Hot-loop retry against a provider is forbidden.

---

# 58. Rate limits

Provider-aware backoff may depend on:

- retry-after;
- quota window;
- per-tenant limits;
- endpoint class.

Rate limit is not a permanent Connection failure.

---

# 59. INT-021 — Provider rate limiting is a bounded operational state

Retry/backoff is observable.

The system does not spin aggressively or mark every rate-limited operation as terminal.

---

# 60. Conflict

Two-way sync conflict occurs when Notrelix and provider changed overlapping facts without a safe ordering/authority resolution.

---

# 61. INT-022 — Sync conflict policy is explicit

Allowed policies may include:

```text
Notrelix wins
provider wins
revision-aware merge
field-specific authority
manual resolution
explicitly accepted last-write-wins
```

Iteration order/time received is not an implicit conflict policy.

---

# 62. Provider revision

Use provider revision/etag/update token where available to detect stale updates.

Wall-clock timestamp alone may be insufficient.

---

# 63. Last-write-wins

Allowed only when explicitly acceptable for that mapped fact and clocks/order assumptions are understood.

Never use it as automatic default for business-critical data.

---

# 64. Conflict visibility

If users need action, conflict state must explain:

- which integration/resource;
- which fields/facts;
- current provider state;
- current Notrelix state;
- resolution options.

---

# 65. Calendar integration

Current source has first-class:

```text
CalendarIntegration
CalendarEvent
CalendarProvider
CalendarSyncDirection
CalendarSyncFingerprint
```

This is concrete evidence for a specialized provider capability.

---

# 66. INT-023 — Calendar integration is provider mapping, not Work Management calendar truth

Work Management Calendar is a View over Work Management data.

External Calendar Integration maps selected Notrelix temporal facts to/from provider events.

They must not be merged into one data model.

---

# 67. Calendar event identity

Provider Calendar Event and Notrelix Item remain separate identities unless a dedicated Integration-owned CalendarEvent represents mapping/projection semantics.

The relationship must be explicit.

---

# 68. Calendar sync direction

A calendar connection may be:

- export-only;
- import-only;
- bidirectional.

Per-field authority for title, time, attendees, status, recurrence must still be explicit.

---

# 69. INT-024 — Calendar date/time semantics preserve product meaning

Differentiate:

```text
date-only
instant
local date-time
time zone
all-day event
recurrence
```

Do not silently convert date-only Work Management meaning into UTC instant.

---

# 70. Calendar fingerprint

Current source has `CalendarSyncFingerprint`.

Fingerprint can support duplicate/change detection.

It is derived integration state, not source business value.

---

# 71. Recurrence

Recurring provider calendar events require explicit mapping semantics.

Do not expand infinite recurrence into uncontrolled Work Management Items by default.

---

# 72. Provider attendee/user mapping

External provider users/attendees are not automatically Notrelix Identities/Workspace Members.

Mapping must validate stable provider identity and scope.

---

# 73. INT-025 — Provider user identity does not create Notrelix membership automatically

An external attendee email cannot grant Workspace access.

Identity/Workspaces/Governance remain authoritative.

---

# 74. Provider-created content

Imported task/calendar/document content must pass target-context validation and authorization/service-principal policy.

External data is untrusted input.

---

# 75. INT-026 — Provider payload is untrusted business input

Even after webhook signature verification, payload values must be:

```text
schema-validated
normalized
scope-validated
mapped
target-validated
```

Authenticity does not imply semantic validity.

---

# 76. Provider HTML/rich content

Sanitize/migrate rich provider content before storing in Documents/Collaboration.

Do not trust signed provider payload to be safe for rendering.

---

# 77. Automation relation

Automation can:

- trigger from stable Integration facts;
- invoke Integration/provider Actions.

Automation owns Rule/Execution.

Integrations owns Connection/provider operation.

---

# 78. INT-027 — Automation retries do not replace Integration idempotency

If Automation retries a provider Action, Integrations still owns provider operation identity/reconciliation.

Two reliability layers must cooperate rather than assume the other solved duplication.

---

# 79. Work Management relation

Integrations maps provider records to Work Management commands/queries.

Work Management owns Item/Field validation and concurrency.

---

# 80. INT-028 — Integrations never writes Work Management persistence directly

Provider sync invokes ordinary Work Management use cases/ports with explicit service/connection principal and idempotency.

---

# 81. Documents relation

External document providers map through validated Documents import/update contracts.

Provider content schema stays outside Documents Domain.

---

# 82. Collaboration relation

External comments/messages may map into Collaboration through approved operations.

Provider identity/content must be translated and scoped.

---

# 83. Governance relation

Connecting, disconnecting, mapping, and selecting target resources are protected operations.

Background sync uses a bounded service/connection principal.

---

# 84. INT-029 — Background Integration principal is bounded

A Connection does not receive unrestricted system authority.

It can operate only within configured/mapped resources/capabilities and current product policy.

---

# 85. Permission revocation

If current product authorization changes, existing Connection may:

- lose access to target;
- fail sync;
- require remapping/disable.

Integration cannot keep mutating because it was once authorized.

---

# 86. Identity relation

User-authentication OAuth belongs to Identity.

Provider business connection OAuth belongs to Integrations.

---

# 87. Accounts relation

Enterprise IdP/SCIM configuration can belong to Accounts + Identity rather than generic Integrations when its product purpose is authentication/provisioning.

Do not classify by protocol only.

---

# 88. Billing relation

Billing may gate:

- number of Connections;
- premium providers;
- sync frequency;
- advanced capabilities.

Integrations owns Connection/sync lifecycle under those entitlement facts.

---

# 89. INT-030 — Billing entitlement does not own Connection identity

A downgrade may pause/disable premium sync while retaining Connection/mapping/history according to policy.

Do not destroy provider state automatically unless product contract says so.

---

# 90. Notifications/activity

Connection/sync failures may create Collaboration notification/activity for relevant users.

Provider delivery attempts themselves are not user Activity.

---

# 91. Analytics

Analytics may derive:

- active connections;
- sync success/error;
- provider usage;
- latency.

Analytics remains derived and must not expose raw secrets/provider personal data unnecessarily.

---

# 92. Search

Search indexes imported target resources through target owners.

Integrations does not become search source-of-truth.

---

# 93. Events/facts

Potential stable Integration facts include:

```text
IntegrationConnectionCreated/Activated/Expired/Disconnected
IntegrationReauthorizationRequired
WebhookAccepted/Rejected where externally useful
ProviderResourceMapped/Unmapped
SyncStarted/Completed/Failed
SyncConflictDetected/Resolved
ProviderOperationSucceeded/Failed/Unknown
CalendarIntegrationChanged
```

Only expose facts with stable consumers.

---

# 94. INT-031 — Integration public events expose translated product meaning

Do not publish raw provider webhook payload or secret/provider SDK object as Notrelix integration event.

---

# 95. Realtime

Realtime may surface:

- connection state;
- sync progress;
- conflict;
- operation result.

Durable Integration query/state remains authoritative.

---

# 96. INT-032 — Integration progress realtime is recoverable

Missing realtime must not lose Connection/sync/error state permanently.

Clients refetch durable current state.

---

# 97. Disconnect

Disconnect defines:

```text
secret revocation/disable
webhook subscription cleanup
future sync cancellation
queued operation behavior
mapping retention
user-visible state
```

---

# 98. INT-033 — Disconnect is a product lifecycle operation, not secret deletion only

A secret may be invalidated but product Connection/mapping/history state still needs explicit transition/retention.

---

# 99. Reconnect/Reauthorize

Reauthorize should preserve logical Connection and mappings where safe.

If provider account identity changes, product must decide whether this is a new Connection instead.

---

# 100. Provider account change

Reconnecting one Connection to a different external organization/account can be dangerous.

Require explicit confirmation/migration rather than silent relink.

---

# 101. Webhook cleanup

Disconnect should remove/disable provider webhook subscriptions where possible and reject later stale deliveries.

---

# 102. Queued work after disconnect

Pending outbound operations may:

```text
cancel
fail terminal
pause awaiting reauth
```

according to operation/product policy.

They must not execute with revoked secret.

---

# 103. Mapping retention after disconnect

Mappings may be retained for history/reconnect/dedup or deleted under privacy policy.

Retention is explicit.

---

# 104. Provider-held data

Deleting Notrelix data does not automatically delete copies already held by external provider.

Privacy/delete flows must consider provider-side cleanup separately.

---

# 105. INT-034 — Provider-held copies are separate deletion responsibility

If Notrelix created/synchronized external provider data, deletion/retention policy must say whether provider cleanup is:

- required;
- best-effort;
- user-managed;
- impossible after disconnect.

---

# 106. Data residency/privacy

External providers may process/store data outside Notrelix region.

Enabling an Integration can have privacy/data-processing implications.

Account/Governance policy may restrict providers.

---

# 107. Threat model

Every provider adapter should consider:

```text
credential theft
webhook spoofing/replay
tenant confusion
provider compromise
over-broad scopes
malicious payload
SSRF/URL issues
rate-limit abuse
data exfiltration
```

---

# 108. INT-035 — Provider permission scopes are least-privilege

Request only provider scopes needed for approved features.

Do not ask for full provider-account access merely for implementation convenience.

---

# 109. Provider scope upgrade

Adding a feature that needs new provider permission may require explicit re-consent/reauthorization.

Do not fail silently.

---

# 110. Adapter capability model

A Provider may not support every Notrelix integration feature.

Capability must be explicit.

Examples:

```text
webhooks supported?
incremental cursor?
idempotency key?
two-way sync?
delete?
attachments?
calendar recurrence?
```

---

# 111. INT-036 — Provider capability limitation stays in Integration boundary

Do not branch core Work Management/Documents Domain logic on `if Provider == Google`.

Integrations maps capability differences.

---

# 112. Generic Integration config

Flexible provider config requires:

```text
provider discriminator
config schema/version
validation
secret references
migration
```

---

# 113. INT-037 — Generic integration table cannot be schema-less JSON dumping ground

Provider-specific config remains typed/versioned even if persisted flexibly.

---

# 114. Provider migration

Provider API/version changes can require:

- config migration;
- webhook migration;
- secret reauth;
- mapping migration;
- backfill;
- contract changes.

Do not silently rewrite stored provider identifiers.

---

# 115. Adapter replacement

Changing SDK/provider implementation should preserve Integration product semantics and logical provider identities where possible.

---

# 116. Service extraction

Integrations is a strong potential operational extraction candidate because provider workloads can have:

- different scaling;
- rate limits;
- secret isolation;
- failure patterns.

Semantic boundary must be hardened before service extraction.

---

# 117. INT-038 — Provider isolation can justify service extraction without changing product context

An independently deployed provider worker/service is still part of Integrations semantics unless product ownership actually changes.

---

# 118. Current source alignment

Current Integrations Domain contains:

```text
Calendar
Connections
Rules
Sync
Webhooks
```

Current source includes:

```text
IntegrationConnection
IntegrationConnectionStatus
IntegrationProvider
IntegrationSyncCursor
SyncStatus
WebhookDelivery
WebhookDeliveryStatus
WebhookSecretHash
WebhookSubscription
CalendarEvent
CalendarIntegration
CalendarProvider
CalendarSyncDirection
CalendarSyncFingerprint
```

This supports explicit Connection, webhook, sync-progress, and provider-specific Calendar mapping semantics.

---

# 119. Current ambiguity watch

Do not normalize:

```text
CalendarEvent
→ same as Work Management Calendar Item

ConnectionStatus
→ all sync/provider health

WebhookDelivery
→ business product event

SyncCursor
→ authoritative resource revision

provider email
→ Notrelix Identity/membership

OAuth protocol
→ always Identity or always Integrations

provider delete
→ Notrelix delete

provider SDK DTO
→ Domain model
```

---

# 120. Change impact — Connection

Review:

```text
scope
Governance
secrets
provider account identity
webhooks
sync
Automation
Billing
frontend
retention
```

---

# 121. Change impact — Webhook

Review:

```text
signature
raw body
replay
tenant routing
dedup identity
payload/version
async delivery
target mapping
threat model
```

---

# 122. Change impact — Sync

Review:

```text
authority/direction
cursor
mapping
conflict
rate limits
retry
target context
provider revision
full resync
```

---

# 123. Change impact — Provider adapter

Review:

```text
provider scopes
secret/config
capabilities
mapping
webhook
idempotency
rate limits
privacy
runbook/observability
frontend
```

---

# 124. Change impact — Calendar

Review:

```text
date/time/time-zone
recurrence
direction
fingerprint
Work Management temporal fields
provider attendee mapping
deletion/conflict
```

---

# 125. Connection checklist

```text
[ ] provider
[ ] stable logical Connection ID
[ ] Account/Workspace/resource scope
[ ] provider account identity
[ ] authorization
[ ] secret reference
[ ] lifecycle
[ ] capability set
[ ] webhook state
[ ] sync state
[ ] disconnect/reconnect policy
```

---

# 126. Webhook checklist

```text
[ ] raw-request verification
[ ] payload bounds
[ ] signature/timestamp
[ ] replay policy
[ ] verified Connection
[ ] trusted tenant resolution
[ ] provider delivery identity
[ ] dedup
[ ] schema/version
[ ] async translation
[ ] poison handling
[ ] redacted observability
```

---

# 127. Sync checklist

```text
[ ] source of truth per fact
[ ] direction
[ ] stable mapping identities
[ ] provider revision/fingerprint
[ ] cursor/checkpoint
[ ] duplicate handling
[ ] conflict rule
[ ] rate limits
[ ] retry/unknown outcome
[ ] delete behavior
[ ] resync/backfill
[ ] target-context validation
```

---

# 128. Outbound-operation checklist

```text
[ ] Connection
[ ] logical operation ID
[ ] provider target
[ ] bounded principal
[ ] idempotency/correlation
[ ] retry classification
[ ] unknown reconciliation
[ ] provider result
[ ] mapping update
[ ] user-visible status where needed
```

---

# 129. Testing/evidence

Critical evidence should cover:

```text
Connection lifecycle
provider account identity/reconnect
secret non-exposure/rotation
webhook signature/timestamp/replay
tenant resolution
duplicate webhook
webhook poison/schema
sync cursor advancement
initial/incremental resync
mapping uniqueness
provider revision/conflict
outbound create retry
unknown outcome reconciliation
rate limit/backoff
disconnect/reauthorization
provider deletion mapping
Calendar direction/fingerprint/time-zone
target authorization
cross-tenant rejection
privacy/deletion
```

---

# 130. Stop conditions

Stop rather than guess if:

- provider SDK type enters product Domain;
- provider token/secret appears in client/event/log;
- webhook mutates business state before authenticity check;
- tenant routing trusts webhook JSON alone;
- duplicate webhook can duplicate target mutation;
- cursor advances before target processing commit;
- two-way sync has no per-field authority/conflict rule;
- provider delete blindly deletes Notrelix data;
- external create retry has no idempotency/reconciliation;
- provider email automatically creates Workspace membership;
- Integration writes Work Management/Documents persistence directly;
- Connection status is overloaded to hide sync/operation states;
- Calendar integration is merged with Work Management Calendar view model;
- generic integration config becomes unversioned arbitrary JSON.

# 131. Related canonical owners

```text
PRODUCT.md
docs/product/product-model.md
docs/product/product-experience.md
docs/product/contexts/accounts.md
docs/product/contexts/identity.md
docs/product/contexts/governance.md
docs/product/contexts/work-management.md
docs/product/contexts/documents.md
docs/product/contexts/collaboration.md
docs/product/contexts/automation.md
docs/product/contexts/billing.md
docs/product/contexts/analytics.md
docs/architecture/events-realtime-and-delivery-boundary.md
docs/architecture/data-ownership-and-consistency.md
docs/architecture/contract-boundaries.md
backend/docs/architecture/security-tenancy-authorization.md
backend/docs/architecture/platform-and-messaging.md
```

# 132. Final Integrations rule

For every Integration capability, answer:

```text
Which provider and logical Connection is involved?
What Notrelix Account/Workspace/resource scope owns the connection?
Where are provider secrets kept?
How is inbound authenticity/replay verified?
What provider delivery identity prevents duplicate processing?
Which Notrelix context owns the target state?
What is source-of-truth per synchronized fact?
What mapping/revision/cursor preserves progress?
How are conflict, deletion, rate limit, retry, and unknown outcome handled?
Which bounded principal authorizes provider-driven mutations?
What happens on disconnect/reconnect?
What provider-held data remains after Notrelix deletion?
```

The target is:

> **a secure anti-corruption boundary that makes external systems useful without making provider identity, schema, transport, retry behavior, or data ownership leak into Notrelix product domains.**
