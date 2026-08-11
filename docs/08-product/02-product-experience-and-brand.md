---
title: "Product Experience and Brand Principles"
document_class: constitution
normative: true
owner: product-design
maturity: FROZEN
conformance: CANONICAL
applies_to: product-experience
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Product Experience and Brand Principles

Notrelix has one product identity but two presentation registers: authenticated product surfaces optimize for sustained focused work; public marketing surfaces can be more expressive and conversion-oriented. Shared brand does not mean identical density, motion or visual hierarchy.

## PROD-UX-101 — The work is the visual priority

Authenticated app chrome should recede behind Boards, Items, Documents, schedules, status and user content. Navigation/control surfaces remain discoverable but should not compete continuously for attention. Avoid decorative containers, borders, gradients and status color when they do not communicate information or interaction.

## PROD-UX-102 — Calm density, not low information

Enterprise work management requires dense information. The target is readable hierarchy and efficient scanning—not oversized cards, excessive whitespace or hidden data. Use spacing, typography, alignment, grouping and progressive disclosure before adding panels/borders/modal layers.

Power users should be able to move quickly with keyboard and predictable interaction; occasional collaborators should still understand labels/state without memorizing hidden gestures.

## PROD-UX-103 — Coherence across capabilities

Board, document, automation and collaboration surfaces use the same design tokens, interaction grammar, feedback patterns and language style where semantics match. Coherence does not justify forcing one component across web/mobile when accessibility/runtime behavior differs, or making unrelated product concepts share a generic model.

## PROD-UX-104 — Language is plain and precise

Authenticated UI copy states what happened, what is required and what the user can do next. Prefer product nouns and direct verbs over framework/engineering jargon. Error messages distinguish validation, access, concurrency and recoverable connectivity when recovery differs. Marketing may be more expressive but should not promise capabilities/security/performance the product contract cannot support.

## PROD-UX-105 — Product and marketing have different volume

**Product register:** quiet, focused, durable for long sessions; restrained motion; content carries most color; primary action remains clear without turning every screen into a conversion funnel.

**Marketing register:** bolder composition, stronger storytelling, polished product-led visual moments and conversion hierarchy are appropriate. It may use richer graphics/gradients/motion, but still reflects the same calm/confident personality and accessibility baseline.

Do not copy the app's density directly into landing pages or import marketing hero aesthetics into every product screen.

## Anti-references

### Generic SaaS template

Avoid defaulting to identical icon-card grids, repeated tiny uppercase “eyebrows”, arbitrary metric bands, decorative gradient blobs and generic hero scaffolding when they do not express Notrelix's actual product.

### Cluttered enterprise

Avoid dense nested toolbars, modal-on-modal flows, permanent secondary panels and visual noise that bury the current work. Enterprise capability is not measured by visible controls per pixel.

### Toy-like motion and color

Friendly does not mean bounce-heavy animation, rainbow palettes or decorative interaction on every state change. Motion explains continuity, hierarchy or result; respect reduced-motion preferences.

### Flat/no-hierarchy interfaces

Calm is not lifeless. Clear focus, selection, hover/pressed/disabled/error states and hierarchy are required. Important state cannot disappear into undifferentiated gray surfaces.

## PROD-UX-106 — Accessibility is product quality

New work targets WCAG 2.2 AA. Web interactions remain keyboard-operable with visible focus and semantic controls; forms have labels/error association; status/meaning is not color-only; motion has reduced alternatives; document/work surfaces remain understandable at zoom/large text. Mobile provides appropriate native accessibility labels/roles/actions and touch targets.

AAA contrast/readability can be pursued for critical long-reading text where it does not harm comprehension or interaction, but the formal baseline remains AA unless an approved product requirement raises it.

## PROD-UX-107 — Async and permission states are designed, not incidental

Every substantial screen considers loading, empty, recoverable error, permission denied/read-only, stale/reconnecting and destructive confirmation states where relevant. Avoid flashing inaccessible data during authorization/bootstrap or showing empty-state creation actions to users who cannot create.

## Proof and ownership

UI primitives encode recurring accessibility/interaction behavior; product packages own capability-specific states; apps own host composition. Storybook/component accessibility tests and critical host/e2e flows provide evidence, supplemented by manual keyboard/screen-reader/design review for complex interactions. Changes to global design personality or accessibility baseline are product decisions and update this document rather than being embedded only in a component library.
