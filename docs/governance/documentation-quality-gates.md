---
document_id: DOC-QUALITY-GATES
document_type: governance
status: active
owner: documentation-governance
applies_to:
  - repository
evidence:
  - Makefile
  - scripts/
  - .github/workflows/
  - backend/backend.slnx
  - backend/**/*.csproj
  - frontend/package.json
  - frontend/pnpm-workspace.yaml
  - frontend/tooling/dependency-rules/src/architecture-manifest.ts
  - frontend/docs/generated/package-boundaries.md
review_on:
  - documentation-governance-change
  - documentation-metadata-change
  - canonical-topic-owner-change
  - generated-document-producer-change
  - backend-project-topology-change
  - frontend-workspace-or-architecture-change
  - ci-documentation-gate-change
---

# Documentation Quality Gates

> **Documentation is a protected engineering subsystem.**
>
> A documentation architecture that is correct only when humans remember to maintain it is not sufficiently governed.
>
> This document defines the executable proof required to keep Notrelix documentation authoritative, navigable, source-aligned, and free of competing canonical generations.

This file owns documentation validation policy.

Authority rules are defined by:

[`documentation-authority.md`](documentation-authority.md)

Lifecycle rules are defined by:

[`documentation-lifecycle.md`](documentation-lifecycle.md)

Topic ownership is defined by:

[`topic-authority-map.md`](topic-authority-map.md)

Decision/exception policy is defined by:

[`decision-and-exception-policy.md`](decision-and-exception-policy.md)

---

# 1. Purpose

Documentation gates protect against failures such as:

```text
broken canonical links
two active owners for one topic
missing required metadata
duplicate rule/ADR/exception IDs
router/topic-map disagreement
forbidden legacy authority returning
backend solution changing while docs remain stale
frontend package architecture changing while docs remain stale
generated docs drifting from producers
required validation silently executing zero work
branch/freeze/final-vN wording becoming authority
```

The objective is not to lint prose style mechanically.

The objective is to protect architectural truth.

---

# 2. Gate philosophy

Documentation checks MUST be:

- deterministic;
- fast enough for required CI;
- explainable;
- fail-closed for protected properties;
- source-aware;
- generator-aware;
- scoped to meaningful architecture/documentation properties.

Do not automate subjective prose quality with brittle heuristics when human review is the correct proof.

Do automate properties that computers can prove exactly.

---

# 3. Documentation gate layers

Target governance has three CI layers:

```text
docs-static
docs-source-alignment
docs-generated
```

They may run in parallel where dependencies allow.

All required layers must pass before documentation governance is green.

---

# 4. `docs-static`

Purpose:

> Prove authored documentation structure and authority are internally coherent.

Expected checks:

```text
links
absolute/local path policy
metadata
document lifecycle values
canonical required paths
topic authority
router coherence
rule IDs
ADR IDs
exception IDs
forbidden authority paths
forbidden authority-generation naming
duplicate canonical ownership
```

Static checks should not require full backend/frontend builds.

---

# 5. `docs-source-alignment`

Purpose:

> Prove source-derived architecture facts exposed by documentation still match their executable producers.

Expected checks include:

```text
backend project inventory
backend project-reference evidence
backend overview source alignment
frontend workspace family alignment
frontend architecture-manifest alignment
frontend architecture documentation alignment
contract producer/consumer evidence where governed
runtime/config inventory where exact docs depend on it
```

This job validates facts whose source changes can make canonical docs materially misleading.

---

# 6. `docs-generated`

Purpose:

> Prove generated documentation is reproducible and committed output matches producers.

Expected generated outputs include:

```text
docs/generated/document-index.md
docs/generated/rule-index.md
backend/docs/generated/project-map.md
frontend/docs/generated/package-boundaries.md
```

The job fails on drift.

It does not silently regenerate and commit.

---

# 7. Developer entry points

Target Makefile commands:

```bash
make docs-check
make docs-generate
make docs-check-generated
```

Meaning:

```text
docs-check
    run all required documentation validation

docs-generate
    intentionally regenerate generated documentation

docs-check-generated
    verify committed generated outputs equal producer output
```

The exact implementation may call multiple scripts.

The command contract should remain stable enough for humans, agents, and CI.

---

# 8. Current transition

The repository currently exposes:

```bash
make docs-check
```

through a single documentation checker.

That existing checker already proves several useful properties, including:

- relative-link existence;
- local `file:///` rejection;
- forbidden legacy paths/references;
- backend/frontend ADR ID duplication checks;
- backend solution production-project presence;
- backend overview coverage;
- frontend workspace-family coverage;
- frontend overview coverage;
- generated frontend package-boundary drift;
- selected branch/freeze/version wording rejection.

The target governance decomposes this responsibility into focused scripts/jobs so failures are easier to understand and extend.

Migration MUST preserve useful existing checks rather than replacing them with weaker validation.

---

# 9. Target scripts

Target tooling location:

```text
scripts/docs/
```

Required target scripts:

```text
check-links.mjs
check-authority.mjs
check-metadata.mjs
check-rule-ids.mjs
check-source-inventory.mjs
check-generated.mjs

generate-document-index.mjs
generate-rule-index.mjs
generate-backend-project-map.mjs
```

Existing frontend package-boundary generation remains owned by the frontend dependency-rules producer.

Do not duplicate that generator under `scripts/docs/`.

---

# 10. Script responsibility rule

Each script should own a clear class of proof.

Avoid another permanent monolithic checker in which unrelated checks are difficult to identify or run independently.

A top-level orchestrator MAY call the focused scripts.

The focused script remains the owner of its check class.

---

# 11. `check-links.mjs`

Must validate authored Markdown links.

At minimum:

- repository-relative links resolve;
- links do not escape repository unexpectedly;
- local workstation/file URLs are forbidden;
- Markdown anchors may be validated where tooling is reliable;
- target canonical paths exist after migration.

Allowed external schemes include only those intentionally supported, such as:

```text
https:
http:        # only if legitimately required; prefer HTTPS
mailto:
```

Do not treat an external web response as required for every local docs CI run unless explicitly designed; external availability makes deterministic CI fragile.

---

# 12. Absolute local paths

The checker MUST reject canonical references such as:

```text
file:///Users/name/project
/home/name/project
C:\Users\name\project
```

Repository documentation cannot depend on one developer workstation.

False positives inside clearly marked explanatory forbidden examples should be handled by checker design rather than forcing governance docs to become vague.

---

# 13. `check-authority.mjs`

Must validate authority topology.

Expected responsibilities:

- required canonical owners exist;
- topic map owner paths exist;
- mapped owners are active authored docs when semantic ownership requires authored docs;
- no forbidden duplicate canonical trees exist;
- no prohibited root/project authority files reappear;
- `CONTEXT-MAP.md` does not route to competing owners;
- project/root indexes do not declare conflicting owners;
- generated files are not mapped as semantic owners where authored owners exist;
- decision registry paths/scopes are correct;
- active exception records reference existing canonical rules/topics where machine-checkable.

---

# 14. Required canonical path set

The authority checker SHOULD enforce the agreed required documentation core.

At repository level:

```text
README.md
PRODUCT.md
DESIGN.md
RULE.md
AGENTS.md
CONTEXT.md
CONTEXT-MAP.md

docs/README.md
docs/governance/*
docs/architecture/*
docs/product/*
docs/quality/*
docs/delivery/*
docs/operations/*
docs/infrastructure/*
docs/decisions/README.md
docs/templates/*
docs/generated/*
```

Backend/frontend required files are enforced from the target manifest/explicit list in governance tooling.

Do not infer required files from sibling symmetry.

---

# 15. Forbidden path policy

The checker MUST maintain an explicit denylist for retired competing authorities.

Typical classes include:

```text
obsolete backend root rule/prompt files
obsolete frontend architecture/rule/tracker files
retired duplicate repository engineering handbooks
retired old backend/frontend documentation trees
versioned/final duplicate canonical files
```

The denylist should be narrow enough to avoid banning legitimate words/paths unrelated to authority.

When a path is retired:

- add to denylist if recurrence risk is meaningful;
- migrate all references;
- delete the old path;
- keep Git as history.

---

# 16. Forbidden-reference policy

Canonical docs MUST NOT route users to retired authority.

The checker should detect references to retired canonical paths.

Historical prose that must mention an old path should be rare and explicitly supported if governance tooling has a safe mechanism.

The target active canonical tree should normally contain no required reading reference to retired authority.

---

# 17. `check-metadata.mjs`

Must validate canonical metadata where required.

For authored canonical docs:

```yaml
document_id
document_type
status
owner
applies_to
evidence
review_on
```

Validate:

- field presence;
- allowed document type;
- allowed lifecycle status;
- non-empty semantic owner;
- stable ID format;
- evidence/review arrays where required.

---

# 18. Metadata scope

Do not require canonical frontmatter on every Markdown file in the repository.

Examples that may use lighter/no canonical metadata:

- small package README;
- provider compatibility router;
- skill README;
- generated file with its own generated header contract;
- temporary task artifact outside canonical docs.

The checker should use explicit canonical path rules/document classes, not “all `.md` must have frontmatter”.

---

# 19. Lifecycle validation

Allowed authored lifecycle:

```text
draft
active
superseded
```

Generated:

```text
generated
```

The checker MUST reject self-invented lifecycle values on canonical docs.

Examples:

```text
FROZEN
FINAL
CANONICAL
CURRENT
```

Architecture maturity may appear in prose or a separately governed engineering artifact, not as documentation lifecycle.

---

# 20. Active-owner status

A canonical topic map MUST NOT point to:

```text
draft
superseded
```

documents.

If a new owner is being migrated, activation and topic-map switch must be coordinated.

Generated evidence may be mapped as exact evidence producer/output, not as semantic owner when an authored owner exists.

---

# 21. Document ID uniqueness

All canonical `document_id` values MUST be unique repository-wide.

A duplicated ID is a blocker even if paths differ.

IDs are stable references.

---

# 22. `check-rule-ids.mjs`

Must validate stable normative rule IDs.

Target prefixes include:

```text
NRX
DOC
SYS
PROD
BE-DOM
BE-APP
BE-INF
BE-PLT
BE-API
BE-SEC
BE-TST
FE-ARCH
FE-DEP
FE-STATE
FE-RT
FE-UI
FE-TST
QLT
DEL
OPS
INFRA
```

The checker should validate:

- uniqueness;
- expected ID shape;
- no duplicate active definitions;
- rule owner document is active;
- generated rule index aligns.

Do not require an ID on every normative sentence.

---

# 23. ADR ID validation

ADR IDs MUST be unique within their registry namespace:

```text
SYS-ADR-*
ADR-*
FE-ADR-*
```

Checks belong in authority/metadata validation or a shared identifier helper.

Requirements:

- filename ID matches ADR body ID for new normalized ADRs;
- registry index has no duplicate ID;
- supersession targets exist;
- no reused retired ID.

Do not rename historical IDs merely for prettier ordering.

---

# 24. Exception ID validation

Exception IDs MUST be repository-unique.

Prefixes:

```text
EX-SYS-*
EX-PROD-*
EX-BE-*
EX-FE-*
EX-DOC-*
```

Machine-checkable exception blocks should verify:

- ID unique;
- Active exception has owner;
- Active exception has review/expiry trigger;
- Active exception has removal condition;
- violated rule/topic resolves where parseable;
- expired exception is not treated as active permission.

---

# 25. Topic-map validation

`topic-authority-map.md` should become machine-checkable for structural properties.

Validate:

- Topic IDs unique;
- canonical owner path exists;
- owner path matches required authority plane;
- owner lifecycle is active;
- no forbidden legacy owner;
- no duplicate mapping for exact Topic ID;
- referenced decision registry exists.

Semantic correctness of topic boundaries still requires architecture review.

CI cannot decide whether Work Management and Documents should merge.

---

# 26. Router coherence

`CONTEXT-MAP.md` is task-oriented.

The checker SHOULD validate deterministic path references against the topic map where machine-readable mapping exists.

At minimum:

- every canonical route target exists;
- no target is forbidden/superseded;
- no task route points to a known competing canonical owner.

Do not attempt brittle natural-language semantic inference.

---

# 27. `check-source-inventory.mjs`

Must validate source-derived facts that documentation depends on.

This script is not a generic code architecture tester.

It checks documentation/source coherence.

---

# 28. Backend project inventory

Validate production project set from:

```text
backend/backend.slnx
```

and project files.

Expected target generated output:

```text
backend/docs/generated/project-map.md
```

The backend overview must describe the production project roles.

Do not manually hard-code package/reference detail in multiple docs.

---

# 29. Backend project-reference alignment

Where backend architecture declares project dependency direction, compare against actual `.csproj` project references.

Architecture tests remain the primary enforcement for illegal code dependencies.

Documentation source-alignment should fail if the documented top-level project graph is materially stale.

---

# 30. Backend target/toolchain facts

If canonical onboarding/current context declares:

- target framework family;
- SDK policy;

the source-inventory check MAY compare against:

```text
backend/global.json
backend/Directory.Build.props
```

Avoid copying every NuGet package/version into authored docs.

Exact dependency versions belong to package manifests.

---

# 31. Frontend workspace family alignment

Validate:

```text
frontend/pnpm-workspace.yaml
```

against documented package-family architecture.

Required workspace families should be explicitly configured, not inferred from random directories.

---

# 32. Frontend architecture-manifest alignment

The executable package-boundary producer is:

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

Documentation governance MUST invoke the frontend-owned architecture docs drift check.

Do not reimplement its full closed-world package logic in repository docs tooling.

Reuse the producer's checker.

---

# 33. Frontend generated package-boundary check

The current frontend package-boundary output remains:

```text
frontend/docs/generated/package-boundaries.md
```

The exact generation/drift command is frontend tooling authority.

Repository `docs-check` orchestrates it.

---

# 34. Contract/source alignment

As cross-stack contract governance matures, source-inventory checks MAY verify:

- required contract artifact producer exists;
- generated consumer artifact is current;
- canonical contract docs reference the correct producer;
- deprecated compatibility paths are not silently removed.

Do not parse every API endpoint into Markdown.

Generated OpenAPI/contracts are better exact evidence.

---

# 35. Runtime source alignment

Only authored facts intended as current stable onboarding/runtime facts should be checked.

Examples:

- environment file convention;
- primary Compose files;
- gateway/service role;
- major local service set.

Do not create CI churn for every port/env value if prose does not claim exact ownership.

Use generated/current-context evidence where appropriate.

---

# 36. `check-generated.mjs`

Must verify generated outputs match their producers.

Target generated outputs:

```text
docs/generated/document-index.md
docs/generated/rule-index.md
backend/docs/generated/project-map.md
frontend/docs/generated/package-boundaries.md
```

The checker should invoke each producer or producer-specific drift check.

---

# 37. Generated drift behavior

On drift:

```text
FAIL
```

The check MUST NOT silently overwrite the committed files and return green.

Developer workflow:

```text
make docs-generate
review diff
commit generated change
make docs-check-generated
```

---

# 38. Generated files are committed evidence

Where repository policy commits generated docs:

- generated output should be deterministic;
- generated output should be reviewable;
- CI should compare producer/output;
- source of truth remains producer.

If future policy stops committing a generated artifact, update governance explicitly.

---

# 39. Document index generator

Target producer:

```text
scripts/docs/generate-document-index.mjs
```

Target output:

```text
docs/generated/document-index.md
```

Expected fields:

```text
Document ID
Type
Status
Owner
Path
Applies To
```

The generator reads canonical metadata.

It must not guess semantic ownership from directory names.

---

# 40. Rule index generator

Target producer:

```text
scripts/docs/generate-rule-index.mjs
```

Target output:

```text
docs/generated/rule-index.md
```

Expected fields:

```text
Rule ID
Title
Owner document
Owner/topic
```

Only current active normative rules should appear as active rules.

Draft/superseded behavior must follow lifecycle governance.

---

# 41. Backend project map generator

Target producer:

```text
scripts/docs/generate-backend-project-map.mjs
```

Inputs:

```text
backend/backend.slnx
backend/**/*.csproj
```

Output:

```text
backend/docs/generated/project-map.md
```

Expected data:

- project path;
- project type/classification;
- project references.

Do not encode business bounded-context inventory into the project map unless it is machine-derived and explicitly useful.

---

# 42. Generator determinism

Generators MUST produce stable ordering.

Do not include volatile values such as:

```text
current timestamp
current branch
current commit SHA
developer path
```

unless that value is an explicit required property of the generated artifact.

Volatile output creates meaningless drift.

---

# 43. Generated-file header

Generated Markdown SHOULD include:

```text
GENERATED — DO NOT EDIT

Producer:
...

Command:
...

Drift check:
...
```

This header must remain concise.

---

# 44. Documentation CI workflow

Target workflow:

```text
.github/workflows/docs-governance.yml
```

It should be dedicated to documentation governance rather than hiding docs failures inside backend/frontend jobs.

It may reuse setup actions/patterns from existing CI.

---

# 45. Workflow triggers

The workflow SHOULD run when changes can affect documentation authority or its producers.

At minimum consider:

```text
*.md canonical root files
docs/**
backend/docs/**
frontend/docs/**
scripts/docs/**
Makefile
backend/backend.slnx
backend/**/*.csproj
frontend/package.json
frontend/pnpm-workspace.yaml
frontend/tooling/dependency-rules/**
frontend generated doc producers
.github/workflows/docs-governance.yml
```

A simpler repository-wide PR trigger is acceptable if path filtering becomes too fragile.

Correctness outranks micro-optimizing CI trigger scope.

---

# 46. `docs-static` job

Expected responsibilities:

```text
checkout
Node setup
run:
    check-links
    check-metadata
    check-authority
    check-rule-ids
```

It should not need backend build or frontend application build.

---

# 47. `docs-source-alignment` job

Expected responsibilities:

```text
checkout
Node setup
pnpm setup when frontend producer check requires it
frozen frontend dependency install as required by producer tooling
run:
    check-source-inventory
    frontend architecture/docs source-alignment check
```

Do not claim frontend package-boundary alignment if the required producer tooling was skipped.

---

# 48. `docs-generated` job

Expected responsibilities:

```text
checkout
tool setup required by generators
regenerate/check generated docs
fail if committed output differs
```

It should report which generated artifact drifted.

---

# 49. CI fail-closed rule

If a required checker cannot run because:

- dependency missing;
- producer command missing;
- parser error;
- expected manifest missing;
- generator crashes;

the required job MUST fail.

Do not convert infrastructure/tooling failure into a successful skip.

---

# 50. Non-zero work

Documentation gates also follow `NRX-016`.

Examples of invalid “green”:

```text
checker scanned 0 canonical documents
rule checker found 0 rule-bearing files because glob is broken
ADR checker looked in nonexistent directory and silently returned
generated check skipped frontend because pnpm unavailable
source inventory parsed 0 projects
```

Checkers SHOULD report useful counts.

---

# 51. Suggested check summaries

Successful scripts SHOULD print concise evidence such as:

```text
links:
    84 authored docs checked
    312 relative links checked

metadata:
    63 canonical documents checked

rules:
    142 active rule IDs checked

source inventory:
    5 backend production projects checked
    N frontend registered packages checked

generated:
    4 generated outputs verified
```

Counts are evidence that protected scope was actually examined.

Exact numbers are runtime output, not hard-coded documentation truth.

---

# 52. Empty scope failure

For a required inventory:

```text
expected > 0
actual = 0
```

must fail.

For explicit required sets, compare exact expected classes/producer outputs rather than only checking non-zero.

---

# 53. Link checker exclusions

Exclude generated/build/vendor areas deliberately.

Possible examples:

```text
node_modules
.git
tool caches
build output
provider cache directories
```

Do not exclude a directory merely because its docs currently fail.

Exclusions must have a semantic tooling reason.

---

# 54. Skills/provider documentation

Skills/provider-specific docs may be excluded from canonical metadata/authority checks where they are procedural.

They still should be included in basic link/path safety where practical.

Do not let a tooling exclusion make them a hidden competing architecture authority.

Authority checker may inspect them for forbidden canonical claims/references if needed.

---

# 55. Markdown examples and false positives

Governance docs must sometimes show forbidden examples.

Checker implementation should distinguish code examples/explicit denylist explanation where practical.

Avoid simplistic regexes that make it impossible to document a rule safely.

However, do not use “example” formatting to hide an active forbidden reference.

---

# 56. Branch/freeze/version wording

The target checker should reject branch/version wording only when used as active canonical authority.

It should not blindly reject every occurrence of words like:

```text
branch
freeze
version
```

because canonical docs may legitimately explain why such authority patterns are forbidden.

Prefer semantic patterns:

- versioned canonical filenames;
- frontmatter maturity masquerading as lifecycle;
- mandatory reading paths tied to one branch;
- fixed transient package counts presented as permanent architecture.

---

# 57. No stale SHA freshness theater

Canonical metadata does not require:

```text
last_verified_sha
```

The checker SHOULD NOT force commit-SHA churn on living docs.

Point-in-time audit/certification artifacts may record SHA separately.

---

# 58. Decision registry checks

Documentation governance SHOULD validate:

- decision indexes exist;
- ADR IDs unique;
- new normalized ADR filename/body ID match;
- status uses allowed values;
- supersession target exists;
- Accepted ADR does not point to a missing current canonical owner where explicitly declared.

Do not require rewriting all historical ADR bodies before governance can be introduced; support a controlled normalization transition.

---

# 59. Exception checks

Where structured exception blocks exist, checks SHOULD validate:

- unique ID;
- Active status fields complete;
- owner;
- review/expiry;
- removal condition;
- New Usage policy;
- verification reference.

Architecture-specific tests enforce actual bounded violation scope.

Documentation checker verifies the governance record.

---

# 60. Evidence traceability

For consequential rules, target traceability is:

```text
canonical rule/topic
→ implementation owner/evidence
→ proof test/gate
→ CI
→ contract/migration artifact where relevant
```

Not every line needs a matrix entry.

Prioritize:

- architecture;
- security/tenant;
- public contracts;
- reliability;
- generated architecture;
- data lifecycle.

---

# 61. Evidence quality

Use the closest deterministic proof.

Examples:

```text
RLS
→ PostgreSQL integration path

frontend package boundary
→ architecture manifest/checker

OpenAPI drift
→ producer/generator diff

Domain failure atomicity
→ Domain behavior tests
```

A mocked test does not prove an external/runtime property it never executes.

---

# 62. Manual-review properties

Some properties remain primarily review-enforced:

- prose clarity;
- correct semantic abstraction boundary;
- whether a product concept deserves a new context;
- whether an ADR alternative was credible;
- whether a document is too broad conceptually.

Do not create fake automatic proof for subjective architecture judgment.

Automation should support review by proving objective structure/evidence.

---

# 63. Gate ownership

Each gate must have an owner.

Suggested:

```text
documentation-governance
```

for repository docs scripts/workflow.

Frontend-owned generated package-boundary checks remain owned by frontend dependency-rules tooling even when orchestrated by docs CI.

---

# 64. Gate changes

Weakening/removing a protected check is a governance change.

A PR that removes a failing rule/check MUST explain:

- why the property is no longer required;
- what replaces it;
- whether architecture changed;
- whether an ADR is required.

Do not delete the checker to make a conflicting source change green.

---

# 65. Gate exceptions

Do not create generic “ignore docs check” switches.

If a temporary architecture exception requires a bounded checker allowance:

- reference the exception ID;
- scope allowance narrowly;
- ensure new usage cannot spread;
- define removal condition.

For a documentation tooling migration, use an explicit temporary migration plan rather than permanently weakening the checker.

---

# 66. Required path migration

When introducing the target docs tree, avoid this state:

```text
checker requires target path
but target owner is not yet migrated
```

and also avoid:

```text
old and new canonical owners both accepted
```

Migration sequencing should:

```text
prepare target
→ migrate knowledge
→ switch authority/checker
→ remove old
→ certify
```

The governance gate should remain truthful at each intentionally merged stage or migration should be performed atomically.

---

# 67. Documentation checks and repository checkout state

CI runs against a committed tree.

Local tooling may run with unrelated working-tree changes.

Checkers MUST NOT reset, clean, or rewrite user files.

Generators only write their defined outputs when explicitly invoked through generation commands.

Check commands should be read-only.

---

# 68. `docs-check` must be read-only

`make docs-check` MUST NOT modify canonical/generated files.

If validation requires generation, generate into memory/temp output or compare deterministic output without leaving a modified tree.

`make docs-generate` is the explicit mutating command.

---

# 69. `docs-generate` behavior

`make docs-generate` MAY update only registered generated documentation outputs.

It MUST NOT:

- rewrite authored docs;
- normalize prose;
- delete canonical docs;
- edit source manifests.

Generated diff remains subject to review.

---

# 70. `docs-check-generated` behavior

Must verify committed generated artifacts.

Possible strategies:

- generate to temporary files and compare;
- regenerate then assert no Git diff, provided the command restores/does not leave changes in check mode;
- producer-specific check command.

The chosen implementation must be deterministic and safe locally/CI.

---

# 71. Performance target

Documentation static checks should remain fast enough for normal PR feedback.

Do not start full backend integration/database containers solely to verify link/metadata authority.

Source-alignment should parse manifests where possible.

Use full project test suites in their owning CI jobs, not duplicate them in docs CI.

---

# 72. No duplicate CI proof

Docs CI should not rebuild the entire backend/frontend just to duplicate existing CI.

Instead:

```text
docs gate
    source/document alignment

backend/frontend CI
    implementation behavior/architecture
```

When one property requires the project-owned checker, invoke that focused checker.

---

# 73. Backend architecture tests relationship

Backend Architecture.Tests remain the executable owner for illegal backend dependency rules.

Docs source-alignment may check:

- project inventory;
- project-reference documentation;
- required architecture docs.

It should not reimplement every architecture-test rule in JavaScript.

---

# 74. Frontend architecture checker relationship

Frontend dependency-rules tooling remains the executable owner for package architecture.

Docs CI invokes/reuses it.

Repository docs tooling should not maintain a second package allow-list.

---

# 75. OpenAPI/codegen relationship

OpenAPI/codegen drift remains producer/project-owned.

Documentation gates may orchestrate or verify that canonical contract documentation points to the correct producer.

Do not move API generation logic into docs scripts.

---

# 76. Documentation gate local workflow

Typical after authored docs edit:

```bash
make docs-check
```

After producer/metadata/rule changes affecting generated docs:

```bash
make docs-generate
make docs-check
```

Before commit, generated diffs should be reviewed.

---

# 77. Failure messages

Failures must tell the developer:

```text
what failed
which file/topic
why it violates governance
what owner/producer should be changed
```

Bad:

```text
docs failed
```

Good:

```text
DOC-AUTHORITY:
topic FE-STATE points to missing/non-active owner
frontend/docs/architecture/state-query-mutations.md
```

---

# 78. Exit codes

Any required violation:

```text
exit non-zero
```

Warnings may be used only for properties not required for merge.

Do not downgrade architecture/security/authority drift to warnings to reduce CI friction.

---

# 79. Deterministic ordering

Checker output should sort findings.

Generated outputs must sort stable registries.

Nondeterministic output makes CI harder to trust and diffs harder to review.

---

# 80. Parser robustness

If frontmatter/registry syntax cannot be parsed:

```text
fail with file + parse reason
```

Do not silently skip the file.

A malformed canonical document is not ungoverned.

---

# 81. Canonical document discovery

Prefer an explicit deterministic discovery rule, such as:

- root canonical file allow-list;
- target canonical directories;
- metadata class.

Do not decide canonicality solely because a file contains the word `canonical`.

---

# 82. Documentation index as evidence

Once generated document index exists, governance may use it as a convenience for visibility.

The generator/input metadata remains source.

The index must not become the only place an owner is declared.

---

# 83. Rule index as evidence

Rule index helps reviewers/agents find rules.

The normative definition remains in the owner document.

Changing a rule requires changing its owner, then regenerating the index.

---

# 84. Required generated producer availability

CI MUST ensure required generators/checkers are available.

If a target generated output exists without its producer:

```text
blocker
```

A committed generated file with no reproducible producer is stale-risk evidence, not governed evidence.

---

# 85. Documentation source-alignment scope review

When a new canonical doc asserts an exact source fact, ask:

```text
Should this fact be generated?
Should source-inventory check validate it?
Or should the doc avoid exact mutable detail?
```

Do not grow source-alignment scripts to compensate for unnecessarily duplicated inventories.

---

# 86. Quality-gate admission test

Before adding a new docs gate:

```text
What objective property does it prove?
What failure does it prevent?
What is the authoritative input?
Is the check deterministic?
Can the same property be generated instead?
Does another project checker already own it?
Will zero-work be detectable?
```

Do not add checks merely because they are easy to regex.

---

# 87. Quality-gate removal test

Before removing a check:

```text
Is the protected property no longer required?
Did authority change?
Is there a stronger replacement?
Are all consumers updated?
Will duplicate/stale authority become possible?
```

If the answer is “the check is annoying”, removal is not justified.

---

# 88. Current monolithic-checker migration

Migration from the current single checker should proceed by preserving behavior first.

Recommended sequence:

```text
1 inventory existing checks
2 create focused scripts with equivalent coverage
3 add metadata/authority/rule/generated checks required by new model
4 make top-level docs-check call focused scripts
5 verify equivalent or stronger failures
6 add dedicated docs-governance CI
7 remove old monolithic implementation
```

Do not remove the current checker before replacement coverage exists.

---

# 89. Dedicated workflow admission

A dedicated docs workflow is justified because documentation governance now has:

- independent canonical authority;
- generated artifacts;
- source-alignment checks;
- merge-blocking failure modes.

Do not hide this responsibility under only backend or frontend CI.

---

# 90. CI job dependency

The three documentation jobs can usually run independently:

```text
docs-static
docs-source-alignment
docs-generated
```

A final summary job is optional.

Docker/build jobs elsewhere should depend on docs governance only if repository branch-protection/release policy intentionally makes docs a release prerequisite.

The docs workflow itself should not manufacture unnecessary serial dependencies.

---

# 91. Branch protection

Once stable, documentation governance should be a required PR check where repository policy supports it.

A required check must have stable naming.

Do not rename required CI jobs casually.

---

# 92. Documentation-only PR

A documentation-only PR changing normative architecture/product contracts is not automatically low risk.

Required docs governance must run.

Affected backend/frontend architecture/test gates may also be required if the docs claim an implementation contract changed.

Do not use “docs only” to bypass architecture review.

---

# 93. Source-only PR

A source-only PR can make docs stale.

Path-based docs CI triggers therefore need to include relevant producers/manifests.

Examples:

```text
backend.slnx/csproj
frontend workspace/architecture manifest
contract producer
generator source
```

If path filtering cannot reliably capture all producers, run docs governance on all PRs.

---

# 94. Generated-only diff

Generated-only changes should be traceable to a producer change.

If generated output changes with no producer/input change:

- investigate nondeterminism;
- do not accept unexplained drift.

---

# 95. Quality-gate severity

## Blocker

- broken canonical link;
- missing canonical owner;
- duplicate active owner;
- duplicate stable ID;
- forbidden retired authority exists;
- source inventory contradicts canonical topology;
- generated drift;
- required checker executes zero protected scope;
- Active exception lacks owner/removal/verification.

## Major

- stale supporting evidence;
- router ambiguity not yet causing wrong canonical owner;
- missing non-critical generated metadata;
- incomplete ADR normalization.

## Minor

- non-critical warning/help text;
- count/report formatting;
- optional evidence link refinement.

---

# 96. Gate result reporting

A docs-governance CI summary SHOULD report:

```text
static:
    pass/fail + counts

source alignment:
    pass/fail + key inventories

generated:
    pass/fail + outputs
```

Do not dump thousands of success lines.

On failure, show actionable findings.

---

# 97. Documentation certification

Documentation governance is certified for a commit when:

```text
docs-static = green
docs-source-alignment = green
docs-generated = green
```

and no required check was skipped.

This is a point-in-time CI fact.

It does not make documents permanently fresh.

---

# 98. Governance test cases

Docs tooling itself SHOULD have focused tests/fixtures for critical parser/checker behavior.

Examples:

- duplicate document ID rejected;
- missing owner rejected;
- draft owner rejected from topic map;
- broken link rejected;
- generated drift rejected;
- zero canonical files rejected;
- expired Active exception rejected;
- forbidden authority path rejected;
- code-block example does not trigger a false positive where intended.

Do not rely only on testing against the live repository.

---

# 99. Tooling code quality

Documentation tooling is production engineering tooling.

It should have:

- clear modules;
- deterministic behavior;
- tests for critical parsing;
- stable error messages where CI depends on them;
- no destructive behavior in check mode;
- dependency minimization.

Do not treat `scripts/docs` as disposable shell glue once it protects architecture.

---

# 100. Final quality-gate rule

The documentation system is governed only when the repository can prove:

```text
the intended owner exists
the path resolves
the metadata is valid
the topic maps to one owner
the source-derived facts still align
the generated evidence matches producers
the required checks actually executed
```

Human review remains responsible for whether the architecture decision itself is good.

CI is responsible for making it hard for documentation authority to silently drift.

The target is:

> **one authoritative documentation model, continuously checked against its own structure and the executable repository facts that matter.**
