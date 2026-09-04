---
document_id: FE-ARCH-TESTING-QUALITY-GATES
document_type: architecture
status: active
owner: frontend-platform
applies_to:
  - frontend-testing
  - frontend-quality-gates
  - frontend-ci
  - frontend-architecture-gates
  - frontend-codegen-gates
  - frontend-mobile-verification
  - frontend-ui-verification
  - frontend-e2e
evidence:
  - frontend/package.json
  - frontend/scripts/
  - frontend/tooling/testing/
  - frontend/tooling/dependency-rules/
  - frontend/playwright.config.ts
  - frontend/playwright.mock.config.ts
  - frontend/playwright.storybook.config.ts
  - .github/workflows/fe-ci.yml
review_on:
  - frontend-test-taxonomy-change
  - frontend-ci-gate-change
  - guarded-test-model-change
  - architecture-gate-change
  - codegen-gate-change
  - ui-test-model-change
  - e2e-model-change
  - host-build-gate-change
---

# Testing and Quality Gates

> **Frontend evidence proves properties, not file existence or command names.**
>
> A green test command is valid evidence only for the property it actually executed. Critical suites must prove non-zero work. Architecture, generated-contract, mobile-purity, UI-accessibility, host-build, and production-E2E properties have separate gates because no single test layer proves all of them.

Full-app mock-browser certification is a separate frontend integration property. It runs the Vite development server with `VITE_MOCK_API=true` and the backend intentionally absent, and proves protected-route bootstrap plus zero backend HTTP, auth-refresh, and WebSocket traffic. Its executable entrypoint is `pnpm e2e:mock`; it does not replace production-mode E2E or real-backend integration evidence.

This document is the canonical frontend owner for:

- frontend test taxonomy;
- node/web/integration/mobile/tooling test responsibility;
- non-zero test execution;
- mobile category coverage;
- architecture gates;
- generated architecture-doc gates;
- codegen drift;
- typecheck/lint/format;
- UI accessibility and visual gates;
- web/mobile/marketing build evidence;
- production web E2E;
- CI dependency topology;
- local `validate:fast` and `validate`;
- evidence scope and reporting;
- exact-revision certification;
- gate change rules and stop conditions.

It does not define:

- backend test architecture;
- product acceptance criteria for each feature;
- exact visual design;
- exact package dependency policy;
- deployment/runtime health outside the executed frontend gates.

---

# 1. Testing architecture objective

The frontend should prove a change using the cheapest reliable seam while preserving broader cross-boundary evidence where required.

Conceptually:

```text
pure logic
→ node/unit

web component behavior
→ web/component

cross-package behavior
→ integration

native behavior
→ mobile

generator/checker behavior
→ tooling/generator

UI foundation behavior
→ Storybook + Playwright + axe + visual

host packaging
→ web/mobile/marketing build

production browser flow
→ production E2E

package architecture
→ architecture checker

generated contract freshness
→ codegen drift check
```

No layer should pretend to prove another layer's property.

---

# 2. FE-TST-001 — Test the protected property at the cheapest reliable seam

Do not use E2E for every pure calculation.

Do not use a unit test to claim real router/build/backend integration.

Choose the lowest-cost seam that can actually prove the property.

---

# 3. Current root commands

Current root `frontend/package.json` exposes:

```text
test:node
test:web
test:integration
test:mobile
test:generators

test:node:guarded
test:web:guarded
test:integration:guarded
test:mobile:guarded
test:generators:guarded

test:ui:a11y
test:ui:visual
test:ui:freeze

e2e

codegen:check
check:architecture
check:architecture-docs
check:test-taxonomy

typecheck
lint
format:check

validate:fast
validate
```

These are current executable evidence routes.

---

# 4. FE-TST-002 — Command name is not the architecture

The exact script body MAY evolve.

The protected properties MUST remain explicit.

If a script is renamed, update docs/CI without weakening the property.

---

# 5. Test taxonomy

Current taxonomy requires Vitest test files under governed app/package/tooling roots to use explicit suffixes such as:

```text
.unit.test.ts[x]
.component.test.ts[x]
.integration.test.ts[x]
.mobile.test.ts[x]
```

with generator tooling handled by its dedicated taxonomy path/config.

---

# 6. FE-TST-003 — Every governed Vitest file belongs to one explicit suite class

A test file MUST NOT depend on accidental config glob overlap.

Its filename/taxonomy should communicate the intended execution class.

---

# 7. Taxonomy purpose

Explicit classification prevents:

```text
mobile test accidentally run only in web
integration test silently omitted
component test selected by wrong config
```

and makes non-zero/category guards meaningful.

---

# 8. FE-TST-004 — Test taxonomy is executable governance

Do not rename a test to avoid the intended suite.

If taxonomy no longer represents the protected property, change taxonomy deliberately and update checker/config/CI together.

---

# 9. Node/unit tests

Node tests cover framework-neutral logic.

Typical examples:

```text
pure product calculations
query-key helpers
protocol parsers
cache transformation
generator pure logic
```

where no DOM/native host is required.

---

# 10. FE-TST-005 — Node tests do not prove browser/native integration

Passing a pure test does not prove:

```text
React rendering
DOM focus
React Native behavior
Vite/Expo packaging
```

Claim only the executed property.

---

# 11. Web/component tests

Web tests cover React/web component and browser-like behavior using the configured web test environment.

---

# 12. FE-TST-006 — Web tests prove web behavior only

A web component test cannot certify native-mobile safety.

Do not treat shared React code as mobile-proven merely because it passed jsdom/web tests.

---

# 13. Integration tests

Integration tests cover cross-package/cross-owner frontend workflows that need more than one isolated unit/component.

Examples can include:

```text
runtime + auth
query + API adapter
realtime + cache adapter
package composition contract
```

---

# 14. FE-TST-007 — Integration test has an explicit integration boundary

A test named integration SHOULD state which real/fake components participate.

Do not call a test “integration” merely because it imports two files.

---

# 15. Mobile tests

Mobile tests cover native-safe runtime/UI/product adapters.

Current mobile guarded execution additionally checks required category participation.

---

# 16. FE-TST-008 — Mobile verification is a first-class suite

Mobile behavior is not a secondary web variant.

Changes to native runtime/UI/product adapters require mobile-specific proof.

---

# 17. Current mobile category guard

Current guarded mobile suite requires test execution from categories representing:

```text
app-mobile
runtime-mobile
ui-mobile
work-management-mobile
docs-mobile
automation-mobile
```

This is current evidence coverage.

The exact category list can evolve when the architecture changes.

---

# 18. FE-TST-009 — Mobile category guard evolves with mobile architecture

If a required mobile architecture unit is added/removed/renamed:

```text
update category guard
update tests
update CI evidence
```

Do not leave the guard stale or delete it because topology changed.

---

# 19. Tooling/generator tests

Generator/checker code protects architecture/contracts.

Current dedicated suite:

```text
test:generators
```

and guarded variant execute tooling/generator tests.

---

# 20. FE-TST-010 — Governance tooling is critical code

A dependency checker or generator defect can bless invalid production architecture.

Changes to:

```text
manifest parser
architecture checker
package generator
docs generator
contract generator
test guards
```

require dedicated tooling tests.

---

# 21. Golden-path generators

Generator tests should prove representative output and failure behavior.

---

# 22. FE-TST-011 — Generator tests cover failure paths, not only happy output

Examples:

```text
unknown package
duplicate path
stale generated file
invalid source input
missing required artifact
```

should fail where the generator/checker contract requires.

---

# 23. Non-zero execution

Current guarded Vitest scripts write JSON result files and run a zero-test guard.

The guard fails if:

```text
numTotalTests == 0
```

or if the report cannot be read.

---

# 24. FE-TST-012 — Zero discovered tests is failure for critical guarded suites

A command that exits successfully after selecting zero required tests is not proof.

Critical suites MUST fail non-zero selection.

---

# 25. Test count versus coverage

Non-zero count proves:

```text
something ran
```

not:

```text
all required behavior is covered
```

Additional category/property guards remain necessary.

---

# 26. FE-TST-013 — Test-count guard is necessary but not sufficient

Do not cite:

```text
100 tests executed
```

as proof that a particular architecture/security property ran.

Name the relevant suite/test/category.

---

# 27. Architecture checker

Current `check:architecture` routes through `@notrelix/dependency-rules`.

Current checker aggregates:

```text
package-manifest checks
dependency architecture checks
folder-boundary checks
generated architecture-doc checks
```

and fails on violations.

---

# 28. FE-TST-014 — Machine-detectable architecture rules are executable

Critical structural properties such as:

```text
closed-world package inventory
allowed internal imports
folder/runtime boundaries
generated boundary drift
```

SHOULD be protected by code, not reviewer memory alone.

---

# 29. Manifest integrity

Architecture checks must fail malformed manifest data before trusting it.

---

# 30. FE-TST-015 — Architecture policy defects fail the gate

Examples:

```text
duplicate package name/path
unknown allowed import
self import
duplicate allow-list edge
```

are gate failures, not warnings.

---

# 31. Generated architecture docs

Current package-boundary docs are deterministic output from the architecture manifest.

`check:architecture-docs` uses the generator's `--check` behavior.

---

# 32. FE-TST-016 — Generated architecture evidence must match its producer exactly

If committed generated docs differ from current generation:

```text
CI fails
```

Do not manually reconcile the table.

Regenerate from the manifest.

---

# 33. Contract codegen drift

Current `codegen:check`:

```text
runs codegen
then
git diff --exit-code on generated contract output
```

according to root scripts.

---

# 34. FE-TST-017 — Producer/generated contract drift is a quality failure

If backend/system producer contract changed, frontend generated contracts must be regenerated and reviewed.

Do not merge stale generated client contracts.

---

# 35. Generated diff review

A generated diff can be large and still semantically important.

---

# 36. FE-TST-018 — Generated output is reviewed, not auto-blessed

Review:

```text
removed/renamed operations
required fields
new enum/union values
error types
realtime events
```

and affected client behavior.

Codegen green only proves freshness.

---

# 37. Typecheck

Typecheck proves TypeScript type consistency for participating tasks.

---

# 38. FE-TST-019 — Typecheck is not runtime proof

Typecheck does not prove:

```text
HTTP header spelling
CSS contrast
socket gap recovery
route navigation
browser startup
```

Use runtime/property-specific tests.

---

# 39. Lint

Lint protects configured static rules.

Current root also exposes a lint-coverage checker in `validate:fast`.

---

# 40. FE-TST-020 — Lint coverage must include intended source

A green linter that silently skips a production package is weak evidence.

Coverage/inventory checks SHOULD detect omitted governed source where current tooling supports it.

---

# 41. Formatting

`format:check` protects repository formatting consistency.

---

# 42. FE-TST-021 — Formatting is hygiene, not semantic correctness

Do not block architecture reasoning at:

```text
Prettier passed
```

and call the feature verified.

---

# 43. UI accessibility

Current root command:

```bash
pnpm test:ui:a11y
```

uses the Storybook Playwright configuration and accessibility tests.

---

# 44. FE-TST-022 — Accessibility is a required UI quality property

Reusable interactive UI changes SHOULD execute accessibility evidence appropriate to the changed component.

Do not treat a11y as optional polish.

---

# 45. Automated a11y limitation

Automated axe-based checks cannot prove every keyboard/screen-reader/workflow property.

---

# 46. FE-TST-023 — Automated a11y plus reasoned interaction review

For high-risk components also verify:

```text
focus order
keyboard workflow
overlay focus return
meaningful labels
reduced motion
touch ergonomics
```

as applicable.

---

# 47. Visual regression

Current Storybook Playwright config:

```text
disables animations for screenshot comparison
uses screenshot diff thresholds
runs against built Storybook
```

as current evidence.

---

# 48. FE-TST-024 — Snapshot changes require intent review

Do not update baseline because CI is red.

Determine:

```text
intended design change
token/theme change
real regression
font/browser instability
```

then approve/update deliberately.

---

# 49. UI freeze suite

Current:

```bash
pnpm test:ui:freeze
```

runs the Storybook UI Playwright suite.

CI `ui-foundation` uses this command.

---

# 50. FE-TST-025 — UI freeze means protected contract stability, not permanent visuals

An intentional design-system change can update snapshots/tests.

The process must review and migrate consumers.

---

# 51. Web build

Current CI builds:

```text
@notrelix/app-web
```

with explicit public runtime env placeholders and uploads the exact `dist` artifact for production E2E.

---

# 52. FE-TST-026 — Web build proves packaging for the exact source revision

It proves:

```text
Vite bundle/build integration
```

for that revision.

It does not prove runtime user-flow correctness.

---

# 53. Exact web artifact reuse

Current `e2e-production` downloads the exact `web-build` artifact produced by the web build job.

It does not rebuild web independently before E2E.

---

# 54. FE-TST-027 — Production E2E executes the exact CI web build artifact

This prevents:

```text
build A passed
E2E rebuilt B
```

from creating ambiguous evidence.

If the artifact changes, E2E evidence changes.

---

# 55. Marketing build

Current CI separately builds:

```text
@notrelix/app-marketing
```

---

# 56. FE-TST-028 — Marketing has independent packaging evidence

Web application build does not prove Next.js marketing build.

Host-specific framework boundaries need host-specific build evidence.

---

# 57. Mobile build/export

Current CI separately runs the mobile package build, currently an Expo export/build command.

---

# 58. FE-TST-029 — Mobile build is separate from mobile unit tests

Mobile tests prove behavior.

Mobile build/export proves native host bundling/configuration.

Both can be required.

---

# 59. Build versus smoke

A build command that only produces artifacts is packaging proof.

It is not runtime health proof unless the built application is started/probed.

---

# 60. FE-TST-030 — Evidence names match executed behavior

Do not call:

```text
docker build
bundle build
expo export
```

a runtime smoke test unless startup/health behavior actually executes.

Evidence must be truthfully named.

---

# 61. Production E2E

Current Playwright production config:

```text
testDir = e2e/production
testMatch = *.e2e.spec.ts
webServer = app-web preview
baseURL = 127.0.0.1:4173
VITE_MOCK_API = false
```

and CI runs Chromium against the uploaded build artifact.

---

# 62. FE-TST-031 — E2E is cross-boundary user-flow evidence

Use production E2E for flows where:

```text
router
runtime
built bundle
host provider
network/client behavior
```

must work together.

Do not duplicate every pure invariant through E2E.

---

# 63. Mock API flag

Current production E2E sets:

```text
VITE_MOCK_API=false
```

This indicates the production-like client path should not use the frontend mock API mode.

---

# 64. FE-TST-032 — E2E environment declaration is part of evidence scope

If an E2E suite uses:

```text
mock backend
fake auth
fixture proxy
```

report that substitution.

Do not claim real backend compatibility if it was not exercised.

---

# 65. Browser scope

Current production E2E uses Chromium/Desktop Chrome.

That is current evidence scope.

---

# 66. FE-TST-033 — One browser project does not prove every browser

If cross-browser compatibility becomes release-critical:

```text
add explicit projects/gates
```

Do not imply Safari/Firefox proof from Chromium-only execution.

---

# 67. Retry

Current CI Playwright uses retries.

A flaky-first-pass can still ultimately pass.

---

# 68. FE-TST-034 — Retry does not normalize flaky behavior

Repeated intermittent failures require investigation.

Retry is resilience/evidence collection support, not permission to ignore instability.

---

# 69. Traces/reports

Current Playwright config captures trace on first retry and uploads reports on CI failure.

---

# 70. FE-TST-035 — Failure artifacts support diagnosis, not pass/fail substitution

A report upload is useful evidence of failure.

It does not make a failed required gate acceptable.

---

# 71. Current CI topology

Current `fe-ci.yml` has required jobs:

```text
quality
test-core
test-mobile
test-tooling
ui-foundation
build-web
build-marketing
build-mobile
e2e-production
frontend-gate
```

The final gate depends on the first nine execution jobs and requires every result to equal `success`.

---

# 72. FE-TST-036 — Final frontend gate is an AND gate

The final gate MUST fail if any required upstream frontend job:

```text
fails
is cancelled
is skipped unexpectedly
```

rather than allowing partial green status.

---

# 73. Quality job

Current `quality` executes:

```text
codegen:check
check:architecture
check:architecture-docs
check:test-taxonomy
typecheck
lint
format:check
```

---

# 74. FE-TST-037 — Static/generated architecture quality runs before broader suites

Current CI makes major test/build jobs depend on `quality`.

This fails fast on structural/generated defects before spending broader CI resources.

---

# 75. Core tests job

Current core job executes guarded:

```text
node
web
integration
```

suites.

---

# 76. FE-TST-038 — Core job remains multi-seam evidence

Do not collapse all frontend correctness into one giant Vitest config if doing so hides taxonomy/coverage responsibility.

Separate suite meaning should remain observable.

---

# 77. Mobile job

Current mobile job runs guarded mobile tests plus category coverage.

---

# 78. FE-TST-039 — Mobile required categories cannot silently disappear

If mobile architecture still contains a required category, the guarded suite should fail when that category selects no tests.

---

# 79. Tooling job

Current tooling job runs guarded generator tests.

---

# 80. FE-TST-040 — Generator tooling failure blocks frontend certification

Generated architecture/contracts are part of the frontend foundation.

Tooling is not “developer convenience only.”

---

# 81. UI foundation job

Current UI job installs Chromium and runs:

```text
test:ui:freeze
```

then uploads report on failure.

---

# 82. FE-TST-041 — UI foundation evidence is independent from web component tests

Vitest component tests do not replace:

```text
Storybook accessibility
visual regression
```

for design-system foundation.

---

# 83. Build jobs

Web, marketing, mobile builds run separately after quality.

---

# 84. FE-TST-042 — Host builds are independent gates

A green web build cannot make a failed mobile/marketing build acceptable.

Each supported host owns its packaging evidence.

---

# 85. E2E dependency

Current E2E depends specifically on `build-web`.

The final gate separately depends on all other required jobs.

---

# 86. FE-TST-043 — E2E dependency graph and final certification graph are distinct

A job need not depend on every other test to run.

The final gate is responsible for requiring all certification properties.

This allows CI parallelism without weakening final result.

---

# 87. Path triggers

Current frontend CI is triggered by frontend paths and shared backend/realtime contract inputs.

Examples include:

```text
frontend/**
backend/contracts/openapi/**
artifacts/contracts/**
```

---

# 88. FE-TST-044 — Producer contract changes are frontend-relevant CI changes

A backend OpenAPI/realtime contract diff can break frontend without any frontend source edit.

CI path filters MUST continue to include producer inputs that frontend codegen consumes.

---

# 89. CI concurrency

Current workflow cancels in-progress PR runs for the same PR/ref group.

This is execution optimization.

---

# 90. FE-TST-045 — Cancelled superseded PR run is not certification

Use the latest completed required run for the exact commit/revision.

Do not cite a cancelled/older run after code changed.

---

# 91. `validate:fast`

Current `validate:fast` executes:

```text
codegen drift
architecture
architecture docs
test taxonomy
lint coverage
typecheck
lint
format
guarded node
guarded web
```

according to current root script.

---

# 92. FE-TST-046 — `validate:fast` is fast feedback, not full frontend CI

It currently does not include every:

```text
integration
mobile
generator
UI
host build
E2E
```

gate.

Do not report full frontend verification from `validate:fast`.

---

# 93. `validate`

Current `validate` extends `validate:fast` with:

```text
integration guarded
mobile guarded
generator guarded
```

---

# 94. FE-TST-047 — `validate` still does not automatically equal current full CI

Current CI additionally executes:

```text
UI foundation
web build
marketing build
mobile build
production E2E
```

Therefore report exact commands, not shorthand assumptions.

---

# 95. Focused development tests

During implementation, run narrow affected tests first.

This shortens feedback.

---

# 96. FE-TST-048 — Focused green is intermediate evidence

Before completion, add all broader gates required by the change class.

Do not stop at one local test if architecture/host/UI/mobile boundaries changed.

---

# 97. Change-to-proof mapping

Examples:

```text
pure product calculation
→ unit + affected type/lint

new package edge
→ architecture + architecture docs + affected tests

OpenAPI change
→ codegen + adapters/state tests

mobile adapter change
→ mobile tests + architecture + mobile build as required

UI primitive change
→ component + a11y + visual + build as required

host/root provider change
→ integration/web + build + E2E as required
```

---

# 98. FE-TST-049 — Validation obligations accumulate

A change touching:

```text
contract
+
architecture
+
UI
```

requires evidence from all three relevant property classes.

Do not choose one “highest” test and discard the rest.

---

# 99. Negative proof

Security/scope/runtime boundaries often require tests proving forbidden behavior.

---

# 100. FE-TST-050 — Boundary changes require negative tests

Examples:

```text
mobile does not import web
Workspace A key != Workspace B
old Workspace event does not mutate B
forbidden import fails checker
zero-test suite fails
```

Positive happy-path proof alone is insufficient.

---

# 101. Regression test

A bug fix should capture the failure at the cheapest reliable seam.

---

# 102. FE-TST-051 — Bug fix adds proof against recurrence where feasible

Do not rely only on manual reproduction after fixing a deterministic defect.

The new test should fail on the broken behavior and pass on the repair.

---

# 103. Architecture regression

If a dependency/source debt escaped current gates, repair the gate where reliable.

---

# 104. FE-TST-052 — Repeated architecture defect becomes executable rule when detectable

Do not document the same reviewer warning indefinitely if AST/manifest/source checking can enforce it reliably.

---

# 105. Flakiness

Flaky tests weaken trust.

---

# 106. FE-TST-053 — Flaky required tests are quality debt

Do not:

```text
increase retries indefinitely
skip randomly
remove assertions
```

without fixing root nondeterminism or narrowing the claim.

---

# 107. Time/randomness

Inject clock/randomness where behavior depends on them.

Current realtime/API tooling already uses explicit factories in several places.

---

# 108. FE-TST-054 — Deterministic time/identity seams are preferred

Avoid long real timers/sleeps in unit tests for:

```text
retry
heartbeat
dedup TTL
optimistic timing
```

when a fake clock/scheduler can prove the property.

---

# 109. Network fakes

A fake network layer is valid for client behavior.

It does not prove real server compatibility.

---

# 110. FE-TST-055 — Test double scope is disclosed in evidence

Report:

```text
mocked fetch
fake socket
fixture server
real preview bundle
real backend
```

as applicable.

Do not blur the boundary.

---

# 111. Production graph

A test may instantiate the production composition graph with selected external substitutions.

---

# 112. FE-TST-056 — Production-graph claim names substitutions

If:

```text
real providers
real router
fake backend
```

are used, say so.

“Production graph” does not mean every external system is real.

---

# 113. Test isolation

Tests should not depend on execution order or residual global state.

---

# 114. FE-TST-057 — Tests clean up global/runtime state

Dispose:

```text
query clients where relevant
sockets
timers
DOM listeners
storage fixtures
```

to prevent false pass/fail ordering.

---

# 115. Snapshot scope

Snapshot tests are suitable for stable serialized/rendered contracts.

Do not snapshot huge arbitrary state when specific assertions are clearer.

---

# 116. FE-TST-058 — Snapshot is not assertion outsourcing

A reviewer should understand what behavior a snapshot protects.

Use focused semantic assertions for critical state/permission logic.

---

# 117. Test data

Fixtures/builders should produce valid contract/product states.

---

# 118. FE-TST-059 — Fixture convenience cannot create impossible production states silently

If a test intentionally uses invalid data:

```text
name it
explain the negative case
```

Otherwise use canonical builder/generated contract types.

---

# 119. Accessibility fixture

UI stories/tests should include realistic long labels/errors/disabled states where relevant.

---

# 120. FE-TST-060 — High-risk UI states are represented in verification fixtures

Do not test only the default Button/empty form and claim the full primitive frozen.

---

# 121. Test removal

Removing a test can be valid when the protected property moved/retired.

---

# 122. FE-TST-061 — Removing a critical test requires replacement/retirement rationale

Do not delete a failing test solely because implementation changed.

State:

```text
property still exists → replacement evidence
or
property retired → architecture/product decision
```

---

# 123. Gate removal

A CI gate can be retired if its property is no longer required or is protected more reliably elsewhere.

---

# 124. FE-TST-062 — Required gate removal is architecture/governance change

Review:

```text
what property disappears
where it is now proven
migration
CI final gate
docs
```

Do not remove a job only to reduce CI duration.

---

# 125. Gate consolidation

Multiple checks may be combined operationally.

Their property-level evidence must remain visible.

---

# 126. FE-TST-063 — CI optimization preserves property observability

Combining commands into one job MUST NOT make it impossible to know whether:

```text
codegen
architecture
mobile
UI
```

actually executed.

---

# 127. Timeouts

CI timeouts are operational controls.

A timeout failure is a failed required gate.

---

# 128. FE-TST-064 — Timeout is not a pass with infrastructure excuse

Diagnose:

```text
hang
resource exhaustion
flakiness
runner outage
```

and rerun/fix.

Do not certify a timed-out required job.

---

# 129. Resource constraints

Local Docker/build resource failures can differ from CI.

Evidence is environment-specific.

---

# 130. FE-TST-065 — Local failure/success scope is explicit

A local host build can prove local behavior.

CI remains required where repository merge policy depends on CI.

Do not dismiss reproducible local OOM/hang without classification.

---

# 131. Exact revision

Evidence belongs to source revision.

---

# 132. FE-TST-066 — Green evidence is SHA-specific

After code changes:

```text
previous green CI
≠ certification for new SHA
```

Re-run required gates.

---

# 133. Branch naming

Architecture quality is not tied to a permanent “freeze branch” name.

CI can run on current protected branches.

---

# 134. FE-TST-067 — Branch label is not architecture evidence

Use:

```text
exact source SHA
required gate result
```

not:

```text
"this came from freeze branch"
```

as certification.

---

# 135. Freeze evidence

A freeze certificate/audit can summarize a verified SHA.

It is point-in-time evidence.

---

# 136. FE-TST-068 — Freeze artifact is not current architecture authority

After source changes, re-evaluate evidence.

Do not use old freeze JSON/markdown as permission to bypass current tests.

---

# 137. Required evidence reporting

Completion reports should name:

```text
command/gate
result
scope
important substitutions
revision if CI
```

---

# 138. FE-TST-069 — Evidence claim cannot exceed execution

Allowed:

```text
"mobile guarded suite passed"
```

Not allowed:

```text
"all frontend passed"
```

if UI/build/E2E were not run.

---

# 139. Partial completion

A blocked external dependency can leave some gates unrun.

Report that honestly.

---

# 140. FE-TST-070 — Unrun required proof remains unresolved

Do not infer a likely pass.

Mark:

```text
not run
blocked
out of scope
```

with reason.

---

# 141. Security-sensitive frontend changes

Auth/session/CSRF/token storage/HTML/telemetry changes need failure/negative evidence.

---

# 142. FE-TST-071 — Security-sensitive changes prove reject/failure paths

Examples:

```text
missing CSRF rejected
invalid redirect sanitized
secret not emitted
logout clears protected state
```

as applicable.

---

# 143. Tenant/scope changes

Query/realtime/route scope changes need cross-scope negative evidence.

---

# 144. FE-TST-072 — Tenant isolation is not proven by two successful loads

Prove:

```text
A data cannot bleed into B
old A event cannot mutate B
old principal cache is cleared/not selected
```

as applicable.

---

# 145. Generated architecture count

Current generated package count can change.

Tests should compare generation, not freeze a manually copied number in docs.

---

# 146. FE-TST-073 — Generated inventory is checked by equality, not stale constants

Use deterministic generator output/manifests.

Do not maintain a second hard-coded package count guard unless it protects a separate intentional invariant.

---

# 147. CI path filters

If a gate's source/producer input moves, path filters must move with it.

---

# 148. FE-TST-074 — CI trigger coverage is part of gate correctness

A perfect job that never runs for the relevant file change is not effective governance.

Review path filters when adding/moving:

```text
contract producer
generator
architecture manifest
shared setup
```

---

# 149. CI shared setup

Current frontend jobs use:

```text
.github/actions/setup-frontend
```

for consistent environment setup.

---

# 150. FE-TST-075 — Shared setup changes affect all frontend evidence

Toolchain/cache/install changes in shared setup require broad CI awareness.

Do not treat setup action as unrelated plumbing.

---

# 151. Lockfile/install

CI setup should use repository package-manager/lockfile policy.

---

# 152. FE-TST-076 — CI must not silently mutate dependency resolution

Use frozen/locked installation for certification.

If lockfile is stale, fix source lockfile rather than disabling the guard permanently.

---

# 153. Test output artifacts

Failure reports are short-lived diagnostic artifacts.

They are not canonical architecture docs.

---

# 154. FE-TST-077 — Diagnostic artifacts do not become authority

Do not route future architecture decisions to an old Playwright report.

Extract durable learning into source/test/docs.

---

# 155. Testing architecture change

Changes to:

```text
suite taxonomy
required job set
zero-test guard
mobile category guard
architecture checker
codegen drift model
UI freeze model
production E2E artifact model
```

can be architecture-significant.

---

# 156. FE-TST-078 — Test-foundation changes follow architecture-change policy

Update:

```text
scripts/config
CI
canonical docs
tests for tooling
generated evidence if applicable
```

and ADR if the durable engineering foundation changes consequentially.

---

# 157. New feature proof checklist

```text
[ ] protected behavior identified
[ ] cheapest focused test
[ ] backend contract test impact
[ ] query/realtime state impact
[ ] web/mobile host impact
[ ] accessibility impact
[ ] architecture/codegen gates
[ ] affected build
[ ] E2E if cross-boundary critical flow
```

---

# 158. New package proof checklist

```text
[ ] package generator/checker tests
[ ] architecture manifest
[ ] generated package docs
[ ] package typecheck/lint
[ ] owner unit/component tests
[ ] mobile/web boundary test
[ ] host consumer build if applicable
```

---

# 159. UI primitive proof checklist

```text
[ ] component behavior
[ ] keyboard/focus
[ ] a11y automation
[ ] light/dark
[ ] relevant accent/density variants
[ ] visual regression
[ ] consumer/build impact
```

---

# 160. Contract change proof checklist

```text
[ ] producer diff
[ ] codegen:check
[ ] generated diff reviewed
[ ] adapter/state tests
[ ] web/mobile compatibility
[ ] E2E/integration where contract-critical
```

---

# 161. Realtime proof checklist

```text
[ ] transport unit
[ ] duplicate/stale/gap
[ ] recovery continuation
[ ] adapter outcome
[ ] Workspace negative isolation
[ ] session transition
[ ] integration with backend if claiming protocol compatibility
```

---

# 162. Stop conditions

Stop completion/certification if:

- a guarded suite selected zero tests;
- mobile category guard is missing a still-required architecture category;
- codegen output is stale;
- package-boundary docs are stale;
- architecture checker was weakened solely for a feature import;
- a snapshot was updated without design review;
- a failed required CI job is being ignored because “other jobs passed”;
- `validate:fast` is being reported as full CI;
- web tests are being reported as mobile proof;
- a build-only command is being called runtime smoke;
- E2E rebuilt a different artifact from the one being certified;
- a test was deleted only because source failed it;
- an old green SHA is being cited after source changed;
- test doubles are hidden while claiming real backend compatibility;
- CI path filters omit a new relevant producer/config path;
- a required property has no executable or explicit manual proof.

---

# 163. FE-TST-082 — Fixture, application mock, shell, and real E2E prove different properties

- Typed fixtures and scenarios render isolated components and exercise deterministic local behavior; they do not intercept transport or prove backend compatibility.
- `@notrelix/dev-mock-backend` is the canonical closed-world application mock. It proves application flows against modeled API semantics and must never fall through to real HTTP.
- Production-shell E2E proves the built client starts and fails safely with mock mode disabled; unreachable placeholder endpoints do not make it a backend integration test.
- Real-backend E2E must orchestrate a reachable canonical backend, deterministic reset/seed, real authentication, persistence reread, and tenant/permission negatives. Its claim must remain separate from shell and mock lanes.

Critical product UI evidence is selected from owner-local schema-version-1 manifests and collected Storybook tags. Manifest rows bind `surfaceId`, `pureEntry`, required story states, checks, owner-local `interactionTests[]`, and explicit N/A rationale. A11y/visual/network runners select the registered stories generically from the built Storybook index; interaction entries require an existing focused component test file and guarded web-test reporting must prove that each declared file executed non-zero. Missing, duplicate, stale, zero-collected, skipped, or provider-backed bindings fail closed.

Pure UI component tests use `renderPureUi`. That harness does not create a QueryClient, Router, auth/session provider, runtime provider, application service, API client, or mock backend. Pure UI tests and stories install a fail-closed network guard for `fetch`, `XMLHttpRequest`, and `WebSocket`; an API interception or fake auth/session provider is integration evidence, not pure UI evidence.

# 164. Executable evidence

Current primary evidence:

```text
frontend/package.json
frontend/scripts/assert-vitest-count.mjs
frontend/scripts/assert-mobile-vitest-coverage.mjs
frontend/scripts/check-test-taxonomy.mjs
frontend/tooling/testing/
frontend/tooling/dependency-rules/
frontend/playwright.config.ts
frontend/playwright.storybook.config.ts
.github/workflows/fe-ci.yml
```

Current CI explicitly protects:

```text
quality
core tests
mobile tests
tooling/generator tests
UI foundation
web build
marketing build
mobile build
production E2E
final AND gate
```

---

# 164. Related frontend architecture

Read:

```text
frontend-overview.md
dependency-boundaries.md
hosts-composition-routing.md
api-and-contracts.md
state-query-mutations.md
realtime.md
ui-and-design-system.md
architecture-change-policy.md
```

---

# 165. Repository quality authority

Also read:

```text
docs/quality/engineering-quality-standard.md
docs/quality/testing-strategy.md
docs/quality/security-quality-standard.md
docs/quality/accessibility-standard.md
docs/quality/performance-and-scalability.md
```

This document specializes those standards for frontend execution.

---

# 166. Explicit non-responsibilities

This document does not define:

```text
backend test suites
GitHub branch protection UI settings
product-specific acceptance criteria
exact runner machine specification
deployment health monitoring
```

It defines frontend engineering evidence and required gate semantics.

---

# 167. Final testing model

The frontend proof model is:

```text
SOURCE / CONTRACT
        ↓
static + generated correctness
        ↓
focused behavior tests
        ↓
architecture/mobile/UI boundaries
        ↓
host packaging
        ↓
production E2E where required
        ↓
final CI AND gate
        ↓
exact-revision evidence
```

with one governing rule:

```text
the evidence claim MUST NOT exceed what actually executed.
```

The testing architecture succeeds when a green frontend result means every required property genuinely ran, generated/architecture drift cannot hide, mobile/UI/tooling are first-class, and teams can still use focused cheap tests during development.
