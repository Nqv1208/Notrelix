---
title: "Product Capability and Feature Ownership"
document_class: handbook
normative: true
owner: frontend-architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: frontend
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Product Capability and Feature Ownership

Notrelix is organized around durable product capabilities, not UI pages. This document prevents capability semantics from drifting into app shells or generic “feature” folders.

## FE-OWN-101 — Product package owns single-capability semantics

If a behavior can be expressed entirely in the vocabulary and lifecycle of one bounded product capability, that capability owns it across core/state/web/mobile slices. Examples: board field configuration belongs to Work Management; document block behavior belongs to Documents; automation trigger/action editing belongs to Automation.

## FE-OWN-102 — Feature package owns cross-capability application behavior

A feature package is justified when it coordinates multiple capability owners or an application-wide workflow and cannot sensibly be reduced to one product owner. It should depend on public capability contracts/adapters rather than deep-importing internal state.

## FE-OWN-103 — UI reuse is not product ownership

A reusable visual primitive moves to UI only when its semantics are presentation-level. A “BoardColumn” remains Work Management even if visually reusable; a generic accessible popover may belong to UI.

## FE-OWN-104 — Duplication is preferable to wrong ownership during discovery

Do not prematurely move similar code into Foundation/UI/Features merely to remove duplication. First establish whether the semantics are identical and stable. Two small capability-local adapters are safer than one generic abstraction that couples unrelated domains.

## Capability public surface

A product capability should make ownership legible:
- `core`: capability contracts/types/pure semantic helpers when justified;
- `state`: authoritative query/mutation adapters and cache ownership;
- host slices: screen/component integration that depends on web/mobile libraries;
- extension/testing slices only when the capability genuinely requires them.

Consumers must depend on public exports. If another capability needs internal data, first determine whether the requirement is a cross-context contract, an application feature, or a read model—not a deep import.

## Review questions

When code placement is disputed, decide in this order: business vocabulary owner → state owner → host neutrality → reuse stability → dependency direction. Team ownership and current ticket location are not architecture criteria.
