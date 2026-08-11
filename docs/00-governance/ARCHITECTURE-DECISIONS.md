---
title: "Architecture Decision Index"
document_class: context
normative: true
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Architecture Decision Index

This index records durable accepted decisions. Detailed ADRs live under `docs/engineering/adr/`.

## Accepted direction

- **AD-001 Modular monolith first.** Backend remains a modular monolith with bounded-context seams; microservices are extracted only from operational/business evidence, not folder symmetry.
- **AD-002 Five backend production projects.** Domain/Application/Infrastructure/Platform/API remain the foundation; ordinary feature work does not create per-context projects.
- **AD-003 Frontend multi-host monorepo.** Web, mobile and marketing are composition roots over package classes enforced by dependency rules.
- **AD-004 Contract-artifact boundary.** BE↔FE REST/realtime contracts are versioned/generated through `artifacts/contracts`/codegen rather than copied DTOs.
- **AD-005 Vertical capability ownership.** Product teams/capabilities own delivery through layers/packages; architectural layers are responsibility boundaries, not permanent handoff teams.
- **AD-006 Documentation authority model.** RULE = constitution, canonical docs = topic truth, AGENTS = execution, CONTEXT/maps = current evidence; nested triple-file bureaucracy is rejected.

New consequential decisions get an ADR and an index entry.
