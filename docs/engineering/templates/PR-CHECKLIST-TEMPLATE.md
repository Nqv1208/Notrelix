---
title: "Pull Request / Change Checklist Template"
document_class: template
normative: false
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Pull Request / Change Checklist Template

- [ ] Semantic owner and intended behavior are stated.
- [ ] Tenant/authorization implications reviewed.
- [ ] Public REST/realtime/event/export impact identified.
- [ ] Schema/RLS/index/migration impact identified.
- [ ] Concurrency/idempotency/failure behavior covered where relevant.
- [ ] Frontend cache/realtime/mobile/accessibility impact covered where relevant.
- [ ] Generated artifacts/lockfiles/migrations updated through canonical tooling.
- [ ] Focused tests and required architecture/contract/integration gates executed.
- [ ] Canonical docs/ADR/exception updated if the decision changed.
- [ ] Rollout/recovery/feature flag explained for non-trivial deployment risk.
- [ ] Completion report claims only evidence actually executed.
