---
title: "Source Evidence and Traceability"
document_class: handbook
normative: true
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Source Evidence and Traceability

## Evidence classes

- **structural:** csproj/package manifests, solution/workspace files, dependency manifests;
- **behavioral:** unit/integration/E2E tests and runtime behavior;
- **contractual:** OpenAPI/realtime/event artifacts, generated clients/types;
- **data:** EF model/migrations/schema/RLS/indexes;
- **operational:** CI workflow, build artifacts, deployment/runtime config.

## Traceability chain

For consequential rules target:

```text
canonical rule ID
→ implementation owner/path
→ proof test/gate
→ CI job
→ contract/migration artifact when relevant
```

Not every line of code needs a matrix entry. Trace architecture/security/tenant/contract/reliability invariants that benefit from durable proof.

## Source audit

When documentation is generated from a baseline, record:

- branch/SHA/date;
- files/manifests inspected;
- known stale docs ignored;
- known unverified assumptions;
- external decisions intentionally left unresolved.

The exact SHA records audit history; it does not make living docs automatically invalid after the next commit.


## Evidence quality

Evidence must exercise the production-relevant path of the property it claims. A mocked handler test cannot prove PostgreSQL RLS, a TypeScript compile cannot prove package dependency policy, and a generated file existing in git cannot prove codegen drift is zero. Prefer the closest deterministic proof and keep broader composition evidence for boundaries that can fail only when integrated.

When a rule is review-enforced because automation is impractical, the change record names the reviewer decision/ADR rather than pretending a missing test exists.
