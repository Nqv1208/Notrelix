---
title: "Accessibility Quality Contract"
document_class: handbook
normative: true
owner: engineering-quality
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Accessibility Quality Contract

Accessibility is a functional requirement of interactive product behavior, not a post-release visual polish task.

## QLT-A11Y-101 — Keyboard and assistive semantics are preserved

Web interactive controls are keyboard reachable/operable, have visible focus and accessible names, use native semantics where possible, and manage focus correctly for dialogs/menus/overlays. Mobile controls expose appropriate labels/roles/actions.

## QLT-A11Y-102 — State is not conveyed only by color or motion

Error, selection, status and required fields have semantic/textual cues. Respect reduced-motion preferences where animation is non-essential. Contrast/touch target requirements are handled by design-system primitives and reviewed in product composition.

## QLT-A11Y-103 — Dynamic application changes are understandable

Async loading, validation failures, toasts/live updates and drag/drop alternatives provide an accessible path. Complex Work Management interactions need keyboard/non-pointer operation or an equivalent accessible control path.

## Evidence

Automated checks catch common defects; they do not replace keyboard/screen-reader review of critical workflows. Component primitives carry baseline tests so feature teams do not re-solve basic accessibility independently.
