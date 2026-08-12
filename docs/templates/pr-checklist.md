---
document_id: TEMPLATE-PR-CHECKLIST
document_type: template
status: active
owner: documentation-governance
applies_to:
  - pull-requests
  - change-reviews
  - repository
  - backend
  - frontend
  - documentation
  - infrastructure
evidence:
  - docs/delivery/change-classification.md
  - docs/delivery/contract-first-delivery.md
  - docs/delivery/definition-of-done.md
  - docs/delivery/release-and-rollout.md
  - docs/delivery/migration-policy.md
  - docs/delivery/team-ownership.md
  - docs/quality/engineering-quality-standard.md
  - docs/quality/testing-strategy.md
  - docs/quality/security-quality-standard.md
  - docs/quality/accessibility-standard.md
  - docs/quality/performance-and-scalability.md
  - docs/governance/decision-and-exception-policy.md
review_on:
  - pull-request-process-change
  - change-classification-change
  - definition-of-done-change
  - required-ci-change
  - template-change
---

# Pull Request / Change Checklist Template

> **A pull request is a review and evidence boundary for a classified change—not a second specification, architecture document, migration plan, or release tracker.**
>
> The checklist exists to prove that the change follows its canonical semantic owner, preserves protected properties, and claims only evidence that actually ran on the reviewed revision.

Use this template for material pull requests or equivalent repository change reviews.

For trivial changes, the checklist may be shortened proportionally.

For high-risk changes, this checklist routes reviewers to the deeper artifacts that must already exist.

---

# 1. What this template owns

This template owns the **review surface** for one change.

It asks:

```text
What changed?
Who owns it?
What class/risk applies?
Which contracts/data/security/runtime properties changed?
Which evidence proves those properties?
What remains intentionally outside this PR?
```

It does not decide product or architecture semantics.

---

# 2. What this template does not replace

Do not use a PR description as a replacement for:

```text
feature specification
architecture decision record
architecture change artifact
migration plan
incident report
canonical product/context docs
canonical architecture docs
release/runbook
```

Link the proper owner instead.

---

# 3. Review philosophy

The checklist is **risk-proportional**.

A private local refactor does not need migration ceremony.

A cross-tenant authorization/schema/public-contract change cannot hide behind a tiny diff.

The relevant question is:

```text
semantic and operational impact
```

not:

```text
number of changed lines
```

---

# 4. Change classification

Before review, classify the change using:

```text
docs/delivery/change-classification.md
```

The PR inherits the highest/cumulative applicable obligations.

Do not split one risky semantic change across small PRs solely to make each appear lower-risk.

---

# 5. Evidence language

Use only:

```text
verified
not applicable — reason
pending rollout phase
```

Do not write:

```text
should pass
probably safe
all tests passed
```

unless the exact claimed evidence actually ran.

---

# 6. Copy from here

```markdown
# Change summary

## What changed

<Concise description of behavior/structure changed.>

## Why

<Problem or required outcome.>

## Semantic / mechanism owner

- semantic owner: `<context / none>`
- architecture/mechanism owner: `<owner>`
- affected consumers: `<web/mobile/worker/provider/etc.>`

## Change classification

Primary class(es):

- `<C0..C8>`

Risk modifiers:

- `<none or modifiers>`

## Canonical references

- product/context: `<path or N/A>`
- architecture: `<path or N/A>`
- ADR: `<path or N/A>`
- feature spec: `<path or N/A>`
- migration plan: `<path or N/A>`
- exception: `<approved exception reference or N/A>`

---

# Review checklist

## 1. Semantic ownership

- [ ] Intended behavior is stated.
- [ ] The authoritative product/context owner is identified.
- [ ] The change does not move business semantics into a technical mechanism accidentally.
- [ ] Cross-context writes use the target owner's contract rather than direct foreign persistence.
- [ ] Current source placement was not treated as automatic architectural precedent.
- [ ] Any changed durable product rule is updated in its canonical product/context document.

### Notes

...

---

## 2. Architecture

- [ ] The change conforms to current System/Backend/Frontend architecture.
- [ ] Dependency direction remains valid.
- [ ] New package/project/service/shared abstraction has an explicit owner and real admission reason.
- [ ] No deep/internal import or layer/context bypass was added.
- [ ] Architecture tests/manifests were updated where the architecture itself legitimately changed.
- [ ] A consequential durable architecture decision has an ADR when required.
- [ ] An accepted architecture rule was not silently weakened in source.

### Architecture change

`<N/A / reference>`

### ADR

`<N/A / ID>`

### Notes

...

---

## 3. Exception gate

- [ ] No exception is required.

or:

- [ ] A governed exception exists and identifies:
  - exact violated rule;
  - exact scope;
  - reason the canonical rule remains correct;
  - risk and compensating controls;
  - logical removal owner;
  - concrete expiry/removal condition;
  - validation preventing scope expansion.

Exception reference:

`<N/A / reference>`

A TODO, comment, PR note, or disabled gate is not an architecture exception.

---

## 4. Product behavior and failure states

- [ ] Success behavior is covered.
- [ ] Validation/rejection behavior is covered.
- [ ] Authorization-denied behavior is covered where relevant.
- [ ] Not-found/conflict/concurrency behavior is explicit where relevant.
- [ ] Semantic no-op behavior is explicit where relevant.
- [ ] Pending/unknown external outcome is represented honestly where relevant.
- [ ] Destructive/archive/restore lifecycle behavior remains coherent.
- [ ] User-visible state is not reported as success before authoritative success.

### Notes

...

---

## 5. Tenant isolation and authorization

- [ ] Server-side authorization remains authoritative.
- [ ] Account/Workspace/resource scope is explicit.
- [ ] Tenant IDs/resource IDs from clients are treated as input, not authority.
- [ ] Wrong-tenant / insufficient-permission negative evidence exists where the change touches protected data.
- [ ] RLS remains defense-in-depth where persistence is tenant-scoped.
- [ ] Background work carries explicit tenant/resource scope.
- [ ] Cache/realtime/search/analytics paths do not bypass current authorization.
- [ ] Revocation behavior is considered for session/share/realtime/cache state.

### Negative cases run

- ...

---

## 6. Security and privacy

- [ ] External input remains untrusted.
- [ ] New provider/webhook/file/redirect/outbound-request boundary was threat-reviewed where applicable.
- [ ] No secret was added to source, logs, generated artifacts, frontend bundles, fixtures, container layers, or documentation.
- [ ] Sensitive error details are not exposed to clients.
- [ ] Logging/telemetry is privacy-minimized.
- [ ] Public/share capability does not become transitive authorization accidentally.
- [ ] Security-sensitive config fails safe.
- [ ] Security regression/adversarial proof exists for material C6 changes.

### Security notes

...

---

## 7. Contracts

### REST / OpenAPI

- [ ] No public REST contract change.

or:

- [ ] Request/response/error/timing/idempotency semantics were reviewed.
- [ ] OpenAPI producer was updated intentionally.
- [ ] Generated consumers are synchronized.
- [ ] Old consumer compatibility is understood.

### Events

- [ ] No public/integration event change.

or:

- [ ] Logical event identity/version/producer/consumer were reviewed.
- [ ] Old backlog/replay/DLQ compatibility is handled.
- [ ] Internal Domain event was not exposed directly by accident.

### Realtime

- [ ] No realtime contract change.

or:

- [ ] Duplicate/out-of-order/gap/reconnect behavior is covered.
- [ ] Subscription/resource authorization is preserved.
- [ ] Realtime remains freshness/convergence, not source truth.

### Public package exports

- [ ] No public package export change.

or:

- [ ] Consumers use intended public exports.
- [ ] Breaking export change has migration/compatibility evidence.

### Contract notes

...

---

## 8. Generated artifacts

- [ ] No generated artifacts changed.

or:

- [ ] Generated output was changed through its canonical producer/generator.
- [ ] No generated file was hand-patched to hide producer drift.
- [ ] Deterministic generation/drift check passes.
- [ ] Generated diff was reviewed for semantic impact.

Affected generated artifacts:

- ...

---

## 9. Persistence / schema / migration

- [ ] No persistence migration is required.

or:

- [ ] Schema change has a reviewed migration.
- [ ] Existing production data—not only an empty database—was considered.
- [ ] Index/constraint/RLS changes were reviewed.
- [ ] Mixed-version old/new runtime compatibility is understood.
- [ ] Backfill is bounded, resumable/idempotent, tenant-safe, and observable where required.
- [ ] Invalid legacy data has an explicit policy.
- [ ] Model/schema drift was fixed rather than suppressed.
- [ ] Applied production migration history was not rewritten casually.
- [ ] Destructive contraction waits for objective removal proof.

Migration plan:

`<N/A / reference>`

### Migration phase represented by this PR

`<N/A / expand / compatible writer / backfill / cutover / contraction>`

### Notes

...

---

## 10. Data ownership

- [ ] The source of truth remains explicit.
- [ ] Derived cache/search/analytics/realtime state did not become writable authority.
- [ ] Dual read has deterministic precedence where used.
- [ ] Dual write has one semantic authority where used.
- [ ] Cross-context ownership migration identifies target owner and transition authority.
- [ ] Stable logical identities are preserved or explicitly migrated.

### Notes

...

---

## 11. Concurrency and idempotency

- [ ] Concurrency behavior is unchanged/not relevant.

or:

- [ ] Stale-write/version behavior is defined and tested.
- [ ] Retryable mutation has explicit logical operation identity.
- [ ] Same-key same-request behavior is correct.
- [ ] Same-key different-request behavior is correct.
- [ ] External/provider operation idempotency or reconciliation exists where required.
- [ ] Retry cannot duplicate user-visible side effects.

### Notes

...

---

## 12. Async / messaging / Automation

- [ ] No durable async behavior changed.

or:

- [ ] Source transaction commits before post-commit side effects according to architecture.
- [ ] Outbox/event identity is stable.
- [ ] Consumer identity is stable.
- [ ] Handler success precedes ordered cursor/ack progression.
- [ ] Retry/backoff is bounded.
- [ ] Poison/dead-letter handling is scoped to logical message + consumer.
- [ ] Ordering scope is no broader than the invariant requires.
- [ ] Dedup/idempotency is preserved.
- [ ] Background tenant context is explicit.
- [ ] Automation recursion/fan-out/schedule semantics remain bounded.

### Notes

...

---

## 13. External providers / Integrations

- [ ] No external-provider behavior changed.

or:

- [ ] Provider credential/Connection scope is correct.
- [ ] Timeout/cancellation is bounded.
- [ ] Rate-limit/backoff behavior is defined.
- [ ] Unknown outcomes are reconciled rather than blindly retried.
- [ ] Webhook authenticity/replay protection occurs before product mutation.
- [ ] Provider schema remains behind the owning adapter boundary.
- [ ] Disconnect/revocation behavior is handled.
- [ ] Logs contain no provider secrets/private payload dumps.

### Provider notes

...

---

## 14. Billing / commercial behavior

- [ ] No Billing/entitlement/usage impact.

or:

- [ ] Entitlement remains distinct from authorization.
- [ ] Billable usage remains Billing-owned.
- [ ] Provider/payment idempotency is preserved.
- [ ] Existing subscriptions/entitlements remain valid during provider outage/migration.
- [ ] No fallback invents unlimited entitlement.
- [ ] Financial history/audit is preserved.

### Notes

...

---

## 15. Frontend state and contracts

- [ ] No frontend impact.

or:

- [ ] Generated contract is used instead of duplicate handwritten DTO.
- [ ] Server-authoritative state remains server-authoritative.
- [ ] Query keys include required Account/Workspace/resource scope.
- [ ] Optimistic mutation rolls back/reconciles on rejection/conflict.
- [ ] Cache invalidation is scoped rather than global by default.
- [ ] Workspace/account switch cannot leak prior-scope state.
- [ ] Old browser/mobile consumers remain compatible as required.
- [ ] Mobile dependency graph remains native-safe.
- [ ] Package placement/export follows frontend architecture.

### Notes

...

---

## 16. Frontend UX and accessibility

- [ ] No user-facing UI changed.

or:

- [ ] Loading state is handled.
- [ ] Empty state is handled.
- [ ] Error/permission/read-only/conflict/pending state is handled.
- [ ] Keyboard interaction works for critical actions.
- [ ] Focus semantics are correct for dialogs/routes/composite widgets.
- [ ] Accessible names/roles/states are present.
- [ ] State is not communicated by color alone.
- [ ] Drag-only action has an equivalent accessible operation.
- [ ] Critical forms/errors are associated programmatically.
- [ ] Mobile semantics are platform-appropriate.
- [ ] Automated accessibility evidence was not treated as complete proof for complex interaction.

### Accessibility evidence

- ...

---

## 17. Performance and scalability

- [ ] Change is not performance-sensitive beyond existing bounds.

or:

- [ ] Potentially unbounded collections use pagination/limits.
- [ ] Core list/read path avoids obvious N+1/unbounded round trips.
- [ ] Query shape/index/tenant selectivity was reviewed.
- [ ] Retry/fan-out/load amplification is bounded.
- [ ] Cache is used only for measured/intentional reason.
- [ ] Cache invalidation/security scope is correct.
- [ ] Large frontend collections use appropriate rendering/windowing without breaking accessibility.
- [ ] Provider/background concurrency respects downstream capacity.
- [ ] Performance evidence states workload/cardinality assumptions.
- [ ] No arbitrary universal latency target was invented.

### Evidence

- ...

---

## 18. Configuration and secrets

- [ ] No configuration change.

or:

- [ ] New config has owner/type/requiredness/default/failure behavior.
- [ ] Build-time/startup/runtime binding is understood.
- [ ] Config rename remains compatible with overlapping old/new runtime if needed.
- [ ] Production-sensitive config does not fall back to permissive dev default.
- [ ] Secret delivery is external to source/artifact.
- [ ] Client-visible config contains no secret.
- [ ] Environment-specific provider callbacks/credentials remain isolated.

### Config notes

...

---

## 19. Container / infrastructure impact

- [ ] No container/infrastructure change.

or:

- [ ] Build context is intentional and secret-safe.
- [ ] `.dockerignore` applies to the actual context root.
- [ ] Runtime image contains runtime needs only.
- [ ] Final image user/filesystem/capabilities are appropriate.
- [ ] Local/development topology is not treated as production authority.
- [ ] New dependency has an explicit authority class and failure/recovery model.
- [ ] Runtime identity/network privilege is least-required.
- [ ] Scale-out cannot duplicate logical work.
- [ ] Container build evidence is not mislabeled as runtime smoke.
- [ ] Exact artifact/revision provenance is retained.

### Notes

...

---

## 20. Observability

- [ ] No new operational failure mode.

or:

- [ ] Semantic correlation identifiers exist.
- [ ] Logs are structured and secret-safe.
- [ ] Metrics avoid unbounded cardinality.
- [ ] New durable consumer has backlog age/retry/poison visibility.
- [ ] Eventual-consistency path has lag/freshness evidence where material.
- [ ] Provider failure can be distinguished from Notrelix defect.
- [ ] Rollout/cohort/migration state is observable.
- [ ] New paging alert is actionable and owned.
- [ ] No numerical SLO/alert threshold was invented without approved owner.

### Notes

...

---

## 21. Degradation and recovery

- [ ] No material degradation/recovery behavior changed.

or:

- [ ] Authoritative dependency failure does not fabricate success.
- [ ] Safe reduced mode is defined.
- [ ] Retry/backoff cannot amplify outage.
- [ ] Realtime/cache/provider degradation preserves security and truth.
- [ ] Recovery considers outbox/dedup/order/provider/object state where applicable.
- [ ] Rollback safety is assessed separately for binary/schema/data/events/provider effects.
- [ ] Forward recovery is defined where rollback is unsafe.
- [ ] Recovery verification checks the original product/tenant/data failure.

### Notes

...

---

## 22. Tests and proof

### Focused tests executed

```text
<exact commands / suites>
```

### Architecture / contract / integration gates executed

```text
<exact commands / checks>
```

### Evidence matrix

| Protected property | Primary proof | Result |
|---|---|---|
| ... | ... | verified |

- [ ] Required filters/suites execute non-zero intended tests.
- [ ] Focused tests are not reported as full suite.
- [ ] Mocks/fakes are not cited for properties they do not reproduce.
- [ ] PostgreSQL/RLS behavior uses PostgreSQL-realistic evidence.
- [ ] Production composition/build/E2E evidence is included when required by change class.

---

## 23. CI

### Required CI checks

- ...

### Exact reviewed revision

`<SHA>`

### Result

`<pending / green / failed>`

- [ ] CI evidence belongs to this exact revision.
- [ ] Required gate was not disabled/weakened to get green.
- [ ] Packaging/build success was not used to replace semantic tests.
- [ ] Green empty suite is not counted as proof.

---

## 24. Documentation

- [ ] No durable documentation update is required.

or:

- [ ] Product/context docs updated.
- [ ] System/Backend/Frontend architecture updated.
- [ ] ADR added/superseded where required.
- [ ] Generated docs regenerated through producer.
- [ ] Quality/Operations/Infrastructure docs updated where protected property changed.
- [ ] Temporary migration/change artifacts are not being treated as permanent authority.
- [ ] Old/retired authority references were removed rather than duplicated.

### Documentation notes

...

---

## 25. Rollout

- [ ] Change can deploy as one valid compatible stage.

or:

- [ ] Deployment stages are explicit.
- [ ] Old/new runtime compatibility is understood.
- [ ] Mobile/browser/worker lag is handled.
- [ ] Feature flag has owner/scope/safe default/removal condition.
- [ ] Cohort boundary follows shared-state compatibility.
- [ ] Canary expansion/abort criteria are defined.
- [ ] Migration stage aligns with release stage.
- [ ] Post-deploy verification proves the changed path receives meaningful traffic.

Release/rollout reference:

`<N/A / reference>`

---

## 26. Cleanup / transition

- [ ] No temporary compatibility path was introduced.

or:

- [ ] Temporary path has a logical owner.
- [ ] Removal condition is objective.
- [ ] Old reader/writer/event/flag/adapter usage is observable.
- [ ] Cleanup is tracked as part of the overall change.
- [ ] Overall change is not called globally Done while mandatory transition remains.

### Remaining transition

- ...

---

## 27. Explicitly not changed

List nearby high-risk surfaces reviewers might otherwise assume were changed.

| Surface | Why unchanged |
|---|---|
| ... | ... |

This prevents hidden scope assumptions.

---

## 28. Known limitations

List accepted limitations of this change that are **not** architecture exceptions.

- ...

If a limitation violates a canonical rule, it needs an approved exception.

---

## 29. Review focus

Ask reviewers to focus on concrete high-risk questions.

1. ...
2. ...
3. ...

Avoid:

```text
please review everything
```

when the change contains one subtle migration/security/contract risk.

---

## 30. Evidence report

### Verified

- ...

### Not applicable

- `<item>` — `<reason>`

### Pending rollout phase

- ...

### Not verified in this PR

- ...

Do not hide unverified properties.

---

# Final declaration

- [ ] The PR description states the actual change class and impact.
- [ ] Every applicable protected property has evidence or an explicit pending governed phase.
- [ ] No material product/architecture/migration/security decision is left for the implementing/reviewing agent to invent.
- [ ] No evidence claim exceeds what actually ran.
- [ ] The exact revision intended to merge has passed the required gates.
```

---

# 7. Checklist reduction rules

For a low-risk change, it is acceptable to keep only relevant sections.

Example C0 private refactor:

```text
Summary
Classification
Ownership
Architecture
Tests
CI
Docs
```

and mark material omitted categories as clearly not applicable if necessary.

Do not paste 30 empty headings into every PR.

---

# 8. Checklist expansion rules

A high-risk PR should link deeper artifacts instead of pasting their entire contents.

Example:

```text
C4 + C6 migration

PR checklist
→ summary/review/evidence

migration-plan-template instance
→ phase/backfill/cutover detail

ADR
→ decision rationale

canonical security/product docs
→ normative rules
```

This keeps the PR reviewable without losing authority.

---

# 9. PR scope quality

A PR should be one coherent review/deployment unit when practical.

It may contain multiple files/layers because a vertical capability crosses layers.

Do not split solely by:

```text
Domain PR
Application PR
Infrastructure PR
Frontend PR
```

if each intermediate state is invalid or forces reviewers to infer the complete semantic change.

Conversely, do not create one giant PR when compatible stages are safer.

---

# 10. PR title quality

A title should describe the logical change.

Prefer:

```text
Add Workspace ownership transfer invariant
Migrate Item status contract to v2
Harden webhook replay protection
```

over:

```text
Update files
Refactor code
Fix stuff
```

Repository-specific conventional commit/PR naming can be added separately if explicitly adopted.

---

# 11. Generated diff quality

Generated output can dominate a PR.

Review sequence should be:

```text
producer/source change
→ generation config
→ generated diff
→ consumer behavior
```

Do not review thousands of generated lines without first validating the source contract.

---

# 12. Migration PR quality

For staged migrations, PR scope should state which phase it implements.

Example:

```text
Phase 1: expand schema only
```

Then reviewers know:

```text
old writer remains authority
new column not yet mandatory
no contraction allowed
```

This is clearer than calling each stage “migration complete”.

---

# 13. Security PR quality

Security-sensitive change review should focus on the **negative path**:

```text
wrong tenant
revoked permission
forged ID
replay
invalid signature
stale cache
misconfiguration
```

not only the authorized happy path.

---

# 14. Frontend PR quality

For UI/state changes, reviewer should ask:

```text
Which server state is authoritative?
How do optimistic updates recover?
What happens on Workspace switch?
What does realtime do after reconnect/gap?
What does denied/read-only/conflict look like?
Can keyboard/mobile/assistive users complete the workflow?
```

A screenshot alone is insufficient evidence.

---

# 15. Infrastructure PR quality

Infrastructure review should distinguish:

```text
build artifact
startup
runtime health
composition
production semantics
```

A successful Docker image build is not evidence for all later layers.

---

# 16. Exception decision

The legacy repository had a separate architecture-exception template with fields for:

```text
exact rule
exact scope
reason
risk/compensating controls
owner
expiry/removal
validation
```

Those fields are now owned normatively by:

```text
docs/governance/decision-and-exception-policy.md
```

Therefore the target documentation set does **not** require a second authored:

```text
docs/templates/architecture-exception-template.md
```

unless implementation of the governance workflow later proves a machine/review form is genuinely useful.

If such a form is introduced later, it MUST be a non-normative projection of the policy, not another owner of exception semantics.

---

# 17. No checkbox theater

A checked box is not proof.

Good:

```text
[x] Cross-tenant denial
Evidence: Notrelix.IntegrationTests/...::WrongWorkspaceCannotRead
CI: backend-integration / SHA ...
```

Weak:

```text
[x] Security reviewed
```

without evidence for a material security change.

---

# 18. Reviewer stop conditions

A reviewer should stop approval if:

- semantic owner is unresolved;
- change classification understates real impact;
- a required ADR/migration plan is absent;
- a canonical rule is violated with no governed exception;
- auth/tenant behavior is left for “later”;
- generated contract was edited directly;
- migration only proves empty DB;
- old mobile/worker/backlog compatibility is unknown;
- retry may duplicate external side effects;
- a test suite selected zero intended tests;
- CI evidence refers to another SHA;
- required rollout stage is impossible to operate safely;
- PR description claims evidence not actually executed.

---

# 19. Final PR-template quality test

A reviewer unfamiliar with the implementation should be able to answer:

```text
What semantic change is this?
Who owns it?
How risky is it?
Which architecture/contracts/data/security surfaces changed?
Which consumers and deployed versions are affected?
What evidence proves each protected property?
Which migration/release phase is represented?
What remains intentionally pending?
Is any exception explicit and temporary?
Is the exact reviewed revision actually certified?
```

If those answers require architecture/product invention, the PR is not ready for approval.
