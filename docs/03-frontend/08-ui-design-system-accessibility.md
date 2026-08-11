---
title: "UI, Design System and Accessibility"
document_class: handbook
normative: true
owner: frontend-architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: frontend
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# UI, Design System and Accessibility

The UI layer standardizes accessible presentation mechanics; it must not become an alternative product/domain layer.

## FE-UI-101 — UI primitives are semantic presentation building blocks

UI packages own tokens, themes, typography, layout primitives, input/control behavior, overlays, feedback patterns and accessible composition. Product-specific nouns or authorization/state decisions remain outside UI.

## FE-UI-102 — Accessibility is a component contract

Interactive primitives MUST preserve keyboard access, focus visibility, programmatic labels/names, correct semantics/roles, focus management for modal/overlay behavior and usable disabled/error states. Accessibility cannot be delegated entirely to individual feature teams after primitive construction.

## FE-UI-103 — Variants are explicit, not CSS escape hatches

Stable visual/behavior variants should be modeled as typed component API. Consumers should not rely on internal DOM structure/class names. Escape hatches are justified only when the primitive cannot reasonably own the use case and they must not bypass accessibility behavior.

## FE-UI-104 — Product state does not live in primitive components

A select/dropdown may expose controlled value/change/loading/error APIs; it does not fetch board fields or workspace members itself. Product adapters bind query/mutation state to primitives.

## Web/mobile relationship

Shared design language does not imply shared rendering implementation. Web UI may depend on DOM/accessibility libraries; mobile UI may use React Native primitives. Shared tokens/contracts may be lower-level when host-neutral. Never import web rendering packages into mobile to obtain visual reuse.

## Required states

Reusable asynchronous UI patterns must support, where applicable: loading/skeleton, empty, error/retry, disabled, read-only, permission-denied, destructive confirmation and progress. A capability owns which state applies; UI owns consistent presentation mechanics.

## Proof

Component tests verify behavior, not snapshots alone. Storybook may document supported states. Web critical primitives should receive automated accessibility checks plus keyboard behavior tests; manual review remains required for focus order and complex interaction. Mobile accessibility labels/roles and screen-reader behavior are validated with native-appropriate tests/review.
