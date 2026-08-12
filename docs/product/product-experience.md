---
document_id: PROD-EXPERIENCE
document_type: product-experience
status: active
owner: product-design
applies_to:
  - authenticated-product
  - marketing
  - web
  - mobile
  - product-copy
  - accessibility
evidence:
  - DESIGN.md
  - PRODUCT.md
  - docs/product/product-model.md
  - frontend/docs/architecture/ui-and-design-system.md
  - frontend/apps/
  - frontend/packages/ui/
  - frontend/packages/product/
  - frontend/packages/features/
  - frontend/tests/
review_on:
  - design-constitution-change
  - product-experience-principle-change
  - accessibility-baseline-change
  - product-vs-marketing-register-change
  - async-state-language-change
  - permission-state-language-change
  - multi-host-experience-change
---

# Product Experience

> **Notrelix should feel calm, focused, and confident while remaining powerful enough for sustained enterprise work.**
>
> Product experience is the way product semantics, system state, permissions, uncertainty, density, accessibility, and user intent are communicated.

This document owns cross-capability product-experience semantics. `DESIGN.md` remains the design constitution. Frontend UI/design documentation owns concrete tokens, component APIs, responsive implementation, primitive ownership, and host-specific mechanics.

# 1. Experience identity

Notrelix has one product identity with two presentation registers:

```text
Authenticated product
    quiet
    focused
    information-dense where useful
    durable for long sessions

Marketing
    more expressive
    narrative
    conversion-oriented
    visually broader
```

Shared brand does not require identical density, motion, hierarchy, or composition.

# 2. PROD-UX-001 — The work is the visual priority

Authenticated product surfaces make the user's work visually dominant. Boards, Items, Documents, schedules, conversations, automation definitions, integrations, and reports should carry more attention than shell chrome.

Navigation remains discoverable, but it must not continuously compete with the current work.

# 3. Chrome recedes

Global navigation, sidebars, utility bars, and shell controls support orientation and action. They should not become the visual product itself.

# 4. Decorative surfaces

Use borders, elevation, panels, gradients, and color when they communicate hierarchy, grouping, focus, interaction, or state. Avoid decoration whose only purpose is to make an enterprise UI look busy or “designed”.

# 5. PROD-UX-002 — Calm density, not low information

Enterprise work management often requires substantial information to remain visible.

Target:

```text
high information value
+
clear hierarchy
+
fast scanning
```

Avoid oversized cards, unnecessary whitespace, hidden primary information, and excessive container nesting.

# 6. Density hierarchy

Prefer typography, alignment, spacing, grouping, progressive disclosure, and stable column structure before adding more containers.

# 7. Power users and occasional collaborators

Power users should move quickly through predictable keyboard-friendly interactions. Occasional collaborators should still understand labels, state, and actions without memorizing hidden gestures.

# 8. PROD-UX-003 — Coherence follows semantics

Boards, Documents, Automation, Collaboration, Integrations, Billing, and Governance should use common interaction grammar where the meaning matches: loading, error, destructive confirmation, read-only, selection, saving, conflict, and retry.

Consistency is semantic, not mechanical.

# 9. Cross-host coherence

Web and mobile do not require the same component implementation. Native behavior, accessibility, density, and interaction may differ while preserving the same product action and meaning.

# 10. PROD-UX-004 — Language is plain and precise

Authenticated product copy should tell users:

```text
what happened
what is required
what state the system is in
what they can do next
```

Prefer product nouns and direct verbs. Avoid framework, transport, persistence, and internal architecture jargon.

# 11. Product vocabulary

Use the language owned by each context: Account, Workspace, Board, Item, Page, Comment, Automation, Connection, Subscription. Do not expose `aggregate`, `DTO`, `handler`, `queue`, or `cache invalidation` as primary user language.

# 12. Error language

Different recovery paths deserve different error semantics: validation, permission denied, concurrency conflict, temporary connectivity, provider uncertainty, and commercial limits must not collapse into one vague message.

# 13. PROD-UX-005 — Product and marketing have different volume

Authenticated product:

```text
quiet
stable
purposeful
content-led
```

Marketing may be more expressive, narrative, visual, and conversion-aware while remaining truthful and accessible.

# 14. Product register

Favor restrained motion, stable spatial relationships, content over decoration, and predictable actions. Not every authenticated surface needs a hero or conversion pattern.

# 15. Marketing register

Marketing may use richer imagery, gradients, motion, storytelling, and product-led visual moments. It must not promise capabilities, performance, integrations, or security guarantees that the product cannot support.

# 16. Anti-pattern — generic SaaS template

Avoid repeated icon-card grids, arbitrary metric strips, generic gradient blobs, tiny uppercase eyebrow labels everywhere, and boilerplate hero layouts when they do not express actual product value.

# 17. Anti-pattern — cluttered enterprise

Avoid permanent nested toolbars, modal-on-modal flows, always-open secondary panels, and visible controls that bury the current work. Enterprise capability is not measured by controls per pixel.

# 18. Anti-pattern — toy-like interaction

Friendly does not mean bounce-heavy animation, rainbow status systems, or celebratory motion on routine actions. Motion should explain continuity, hierarchy, state, or result.

# 19. Anti-pattern — flat/no hierarchy

Calm is not visually indifferent. Focus, selection, hover, pressed, disabled, read-only, error, warning, success, and pending state must remain clear.

# 20. PROD-UX-006 — Accessibility is product quality

Applicable web work targets WCAG 2.2 AA. Native mobile provides equivalent platform-appropriate accessibility.

Accessibility is a release-quality property rather than cleanup work.

# 21. Keyboard

Critical web workflows should be usable by keyboard where the interaction model supports it. Pointer-only hidden gestures must not be the only path to important actions.

# 22. Focus

Focus is visible, ordered meaningfully, and restored correctly after dialogs, menus, editors, route changes, or dynamic content.

# 23. Semantic controls

Use appropriate control semantics. Visual containers must not replace buttons, links, or inputs without equivalent semantics and keyboard behavior.

# 24. Forms

Forms need explicit labels, descriptions where useful, associated validation, keyboard access, and predictable submission state. Placeholder text is not a label substitute.

# 25. Color

Meaning cannot depend on color alone. Status, selection, validation, and permission need additional semantic cues.

# 26. Motion

Reduced-motion preferences must be respected. Motion must not be required to understand the product state.

# 27. Zoom and large text

Work surfaces and documents remain understandable under zoom and large text. Layout may adapt; critical meaning and actions remain available.

# 28. Mobile accessibility

Native UI uses appropriate labels, roles, actions, touch targets, and navigation order rather than copied web assumptions.

# 29. PROD-UX-007 — System state is designed explicitly

Substantial surfaces consider the states that matter to the capability:

```text
initial loading
background refresh
empty
ready
saving
pending
error
read-only
permission denied
stale
reconnecting
conflict
destructive confirmation
```

Relevant states must not be accidental.

# 30. Loading

Differentiate “no usable state yet” from “existing content is refreshing”. Preserve useful stable content during background refresh when safe.

# 31. Empty state

Explain what is empty, whether it is expected, and which action is available. Do not show create actions to users who cannot create.

# 32. Permission denied

Permission denial is distinct from empty/not-found. Reveal only what policy allows and provide an appropriate next step where possible.

# 33. Read-only

Read-only is an intentional product state. Users should understand what can be inspected and why mutation is unavailable where explaining it is safe and useful.

# 34. PROD-UX-008 — Permission state must not flash unauthorized data

During bootstrap, tenant switch, route transition, or authorization resolution, protected content must not render briefly and then disappear.

# 35. Saving

Saving feedback should match the actual contract. A locally committed mutation may need subtle feedback; long-running provider or background work needs explicit pending semantics.

# 36. PROD-UX-009 — Pending is not completed

If work continues after immediate request success, use truthful state where material:

```text
pending
queued
syncing
processing
awaiting provider
```

Do not display final completion prematurely.

# 37. Provider uncertainty

An external timeout may mean the outcome is unknown. The UI may need `pending confirmation`, `reconciling`, or `status unknown` rather than a false binary success/failure.

# 38. Stale and reconnecting

Stale data or reconnecting realtime should be represented at the level users need. Do not expose transport jargon, but do not present uncertain state as fully current.

# 39. Conflict

Concurrency/product conflicts need a recoverable path such as refresh, compare, merge, retry, or explicit choice. Never silently overwrite if product semantics are fail-closed.

# 40. Destructive confirmation

Destructive actions must communicate target, scope, consequence, downstream impact, and reversibility to the degree appropriate to risk.

# 41. Archive versus delete

Use the correct product verb: archive, delete, disconnect, revoke, cancel, remove. Generic “Delete” must not obscure different lifecycle semantics.

# 42. PROD-UX-010 — User-visible consistency matches system consistency

If downstream work is eventual, the product must not imply universal instant finality.

Example:

```text
Board change committed
Automation pending
Provider sync pending
Analytics may lag
```

# 43. Realtime convergence

Realtime improves freshness but must have a query/refetch recovery path when events are missed, duplicated, reordered, or the connection is interrupted.

# 44. Optimistic interaction

Optimistic UI is appropriate only when likely success, rollback/reconciliation, conflict behavior, and scope are understood.

# 45. PROD-UX-011 — Optimism is provisional

Optimistic state is temporary. Authoritative product/server state wins on reconciliation.

# 46. Workspace transitions

Switching Workspace must make new scope clear, prevent old late responses from contaminating new scope, and re-establish navigation/subscriptions/cache coherently.

# 47. Account transitions

Account administration is distinct from Workspace product work. Account-wide SSO, SCIM, region, billing, ownership, and closure must not look like arbitrary Workspace settings.

# 48. PROD-UX-012 — Scope is visible when mistakes could be costly

High-impact Account, Workspace, security, billing, region, provisioning, and destructive actions should clearly communicate current scope.

# 49. Navigation

Navigation follows user product concepts rather than technical module names. Users should understand where they are, which object they are working on, and which scope contains it.

# 50. Work Management experience

Different views reinforce:

```text
same item
same field values
different presentation
```

Table, Kanban, Calendar, Timeline, Form, and Dashboard do not imply separate authoritative records.

# 51. Table

Table emphasizes dense scanning, field editing, sorting/filtering, and comparison while preserving product schema semantics.

# 52. Kanban

Kanban emphasizes grouping, flow, and spatial state. Dragging must map to the configured product field/grouping semantics; a column is not automatically a universal status or BoardGroup.

# 53. Calendar and Timeline

Temporal views represent underlying work temporal fields. They do not create duplicate calendar/timeline records.

# 54. Form

Form simplifies structured input through Work Management rules while hiding unrelated board complexity.

# 55. Dashboard

Dashboard communicates derived insight and should distinguish source data, metric meaning, freshness, and actions that route back to source owners.

# 56. Documents experience

Documents prioritize reading/writing, long-form content, hierarchy, and focus. Product chrome should recede during deep work.

# 57. Collaboration experience

Comments, mentions, activity, and notifications remain attached to the work. Notifications preserve target, reason, actor, and next action.

# 58. Automation experience

Users should understand when an automation runs, what conditions apply, what action occurs, and what happened during execution. Avoid opaque “magic”.

# 59. Integrations experience

Distinguish connection configured, authentication valid, sync enabled, sync healthy, sync pending, and sync failed. One toggle cannot safely represent every integration state.

# 60. Billing experience

Distinguish plan, entitlement, usage, payment/commercial state, and product consequence. Payment failure must not imply automatic destructive deletion unless product policy says so.

# 61. Governance experience

Permission/security interfaces should help users understand `who can do what to which scope/resource` without exposing unnecessary internal identifiers.

# 62. Account administration experience

Organization-wide identity providers, domains, SCIM, region/data placement, membership, and Account closure require especially clear scope and consequence.

# 63. Analytics experience

Analytics communicates metric definition, scope, period, and freshness. Visually precise numbers must not hide semantically vague metrics.

# 64. PROD-UX-013 — Feedback matches action weight

Routine low-risk actions should not interrupt flow unnecessarily. High-impact actions require stronger confirmation, explanation, and durable feedback.

# 65. Toasts and persistent state

Toasts can acknowledge routine outcomes. Critical recovery instructions or persistent failures cannot live only in short-lived transient messages.

# 66. Modals and workflow surfaces

Use dialogs for focused interruption. Avoid modal nesting. Complex stateful workflows may deserve dedicated surfaces.

# 67. Progressive disclosure

Hide secondary complexity until relevant, but do not hide primary state/actions users need repeatedly.

# 68. Defaults

Defaults should be safe, understandable, and reversible where practical. They must not conceal impactful policy.

# 69. Enterprise administration

Group enterprise controls by product meaning rather than backend module structure: identity/provisioning, members, Workspace routing, security/governance, billing, data region, integrations.

# 70. PROD-UX-014 — Scope and consequence precede enterprise action

For organization-wide administration show enough context to understand which Account, which users/Workspaces, what changes, when it applies, and whether it can be undone.

# 71. Product-copy tone

Copy should be clear, brief, specific, calm, and respectful. Avoid cute errors during serious failure, unnecessary exclamation, blame, vague “Oops”, and engineering jargon.

# 72. Warnings and confirmation

Warnings explain risk, scope, and next action without creating warning fatigue. Confirmations should describe consequences rather than asking only “Are you sure?”.

# 73. Accessibility evidence

Evidence may include component accessibility tests, axe, keyboard tests, E2E, manual screen-reader review, zoom/large-text review, and native accessibility review.

# 74. PROD-UX-015 — Experience regressions are product regressions

A change can be technically correct and still regress the product if it hides important state, breaks keyboard access, misrepresents pending work, makes scope ambiguous, or obscures destructive consequences.

# 75. Multi-host parity

Web and mobile require semantic parity for shared capabilities, not pixel parity. Host-specific affordances are allowed when product meaning is preserved.

# 76. Marketing truthfulness

Marketing may simplify stories but cannot invent feature completeness, unsupported integrations, security guarantees, or unrealistic performance.

# 77. Change classification

Update `DESIGN.md` for constitution-level design identity. Update this document for cross-capability experience semantics. Update frontend docs/source for implementation-specific UI mechanics.

# 78. Experience review checklist

```text
[ ] work remains visually primary
[ ] hierarchy and density are purposeful
[ ] product vocabulary is used
[ ] loading/empty/error/read-only states are intentional
[ ] permission state is safe
[ ] pending work is truthful
[ ] conflicts have recovery
[ ] scope is clear where needed
[ ] keyboard/focus/accessibility reviewed
[ ] web/mobile semantics are coherent
[ ] destructive consequences are accurate
[ ] technical/provider jargon does not leak unnecessarily
```

# 79. Stop conditions

Stop rather than guess when:

- the UI promises state the backend/product cannot guarantee;
- Account/Workspace scope is ambiguous for high-impact action;
- frontend copy invents permission semantics;
- provider outcome is unknown but UX claims certainty;
- accessibility of a critical interaction is unresolved;
- one Work Management view is becoming a separate model;
- marketing claims exceed the approved product contract.

# 80. Related canonical owners

```text
DESIGN.md
PRODUCT.md
docs/product/product-model.md
docs/product/contexts/*.md
frontend/docs/architecture/ui-and-design-system.md
frontend/docs/architecture/state-query-mutations.md
frontend/docs/architecture/realtime.md
frontend/docs/architecture/hosts-composition-routing.md
docs/quality/accessibility-standard.md
```

# 81. Final experience rule

For every substantial product surface, answer:

```text
What work is primary?
What state is the system in?
What scope is active?
What can/cannot the user do?
What is committed versus pending?
How does stale/conflicting state recover?
Is the experience accessible?
Does language use product semantics?
Would the experience still make sense if implementation changed?
```

The target is:

> **a calm, focused enterprise product that communicates truth clearly without making complexity visually or semantically chaotic.**
