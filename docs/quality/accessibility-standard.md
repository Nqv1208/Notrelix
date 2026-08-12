---
document_id: QLT-ACCESSIBILITY
document_type: quality-standard
status: active
owner: product-accessibility
applies_to:
  - web
  - mobile
  - marketing
  - authenticated-product
  - ui-components
  - product-workflows
evidence:
  - DESIGN.md
  - docs/product/product-experience.md
  - docs/quality/engineering-quality-standard.md
  - docs/quality/testing-strategy.md
  - frontend/docs/architecture/ui-and-design-system.md
  - frontend/tooling/storybook/
  - .github/workflows/fe-ci.yml
review_on:
  - accessibility-baseline-change
  - design-system-primitive-change
  - navigation-or-focus-model-change
  - complex-interaction-change
  - authentication-experience-change
  - form-system-change
  - mobile-accessibility-change
  - storybook-a11y-gate-change
---

# Accessibility Standard

> **Accessibility is functional correctness for how people perceive, navigate, understand, and operate Notrelix.**
>
> The web target is WCAG 2.2 AA for applicable product and marketing experiences. Native mobile must provide equivalent platform-appropriate accessibility rather than copying web mechanics literally.

This document owns repository-wide accessibility quality requirements.

`DESIGN.md` owns design identity.

`product-experience.md` owns cross-product UX semantics.

Frontend UI documentation owns concrete component implementation.

This standard defines the accessibility contract and required evidence.

---

# 1. Accessibility model

Accessibility must survive:

```text
keyboard-only use
screen reader / semantic navigation
zoom / large text
reduced motion
low vision / contrast needs
touch
alternative pointer operation
dynamic async state
validation/error recovery
authentication
destructive/legal/financial action
mobile assistive technology
```

---

# 2. QLT-A11Y-001 — Accessibility is a release-quality requirement

Accessibility defects in critical flows are functional regressions.

They are not deferred automatically as visual polish.

---

# 3. Conformance target

Applicable web surfaces target:

```text
WCAG 2.2 Level AA
```

Product-specific requirements may be stricter where risk/usage warrants it.

---

# 4. QLT-A11Y-002 — Standards baseline does not replace usability

Passing automated WCAG rules does not prove a complex Board, editor, dialog, drag/drop flow, or mobile workflow is actually operable.

Manual interaction review remains required for high-risk patterns.

---

# 5. Native semantics first

Prefer native platform controls/semantics when they meet product needs.

Custom components inherit the burden of reproducing:

- semantics;
- keyboard;
- focus;
- state;
- assistive-technology behavior.

---

# 6. QLT-A11Y-003 — Custom control has a complete interaction contract

A div styled like a button is not acceptable unless it provides equivalent role, name, state, focus, and operation.

Prefer actual button/link/input primitives.

---

# 7. Accessible name

Every interactive control has a programmatically determinable accessible name.

Icon-only controls require meaningful label.

---

# 8. QLT-A11Y-004 — Visual tooltip is not the only accessible name

Tooltip may supplement.

It must not be the only way an assistive-technology user learns the control purpose.

---

# 9. Role and state

Controls expose role/state such as:

```text
expanded
selected
checked
pressed
disabled
invalid
required
current
```

when semantics require it.

---

# 10. QLT-A11Y-005 — Programmatic state matches visible state

A visually selected/disabled/expanded control must not expose contradictory accessibility state.

---

# 11. Keyboard

All web functionality that is not inherently pointer-specific must be keyboard operable.

Do not require mouse-only interactions.

---

# 12. QLT-A11Y-006 — Keyboard path reaches every primary product action

This includes:

- navigation;
- Board actions;
- menu/dialog operations;
- inline editing;
- document editing controls;
- sharing;
- form submission;
- account/security administration.

---

# 13. Keyboard traps

Focus must not become trapped except intentionally inside an accessible modal/dialog pattern with an exit mechanism.

---

# 14. QLT-A11Y-007 — No accidental keyboard trap

Editors, popovers, tables, grids, code blocks, and nested overlays must provide predictable escape/navigation.

---

# 15. Focus visible

Keyboard focus has a visible indicator.

Design-system tokens/components should provide consistent focus treatment.

---

# 16. QLT-A11Y-008 — Focus indicator remains perceptible

Do not remove outline without an equivalent visible focus style.

Focus visibility must survive themes/state/backgrounds.

---

# 17. Focus order

Sequential focus order preserves meaning and operation.

DOM order and visual order should not diverge in ways that confuse keyboard/assistive users.

---

# 18. QLT-A11Y-009 — Focus order follows meaningful interaction order

CSS visual rearrangement must not create a nonsensical focus sequence.

---

# 19. Focus not obscured

Sticky headers, panels, dialogs, banners, or virtualized surfaces must not entirely hide the focused control.

---

# 20. QLT-A11Y-010 — Focus is not hidden by authored overlays

When focus moves, the user can perceive the focused control without manually guessing/scrolling around persistent overlays.

---

# 21. Dialog focus

Opening a modal/dialog:

- moves focus appropriately;
- traps only when semantics require;
- restores focus to logical trigger/fallback after close.

---

# 22. QLT-A11Y-011 — Overlay lifecycle manages focus explicitly

Dialog/menu/popover behavior must not dump focus to document body or stale removed elements.

---

# 23. Route/page transition focus

Single-page navigation should establish meaningful focus/context after transition where needed.

Do not leave screen-reader users unaware that the main content changed.

---

# 24. Bypass repeated content

Web application/marketing surfaces should provide a practical way to bypass repeated navigation when appropriate.

---

# 25. Headings and landmarks

Headings/regions reflect content structure.

Do not choose heading levels solely for visual size.

---

# 26. QLT-A11Y-012 — Visual hierarchy has semantic structure

Typography styling cannot replace heading/landmark semantics for substantial pages.

---

# 27. Page title

Browser/page titles identify the current topic/resource sufficiently for navigation and assistive context.

---

# 28. Links

Link purpose is understandable from text/context.

Avoid repeated ambiguous links such as:

```text
click here
more
open
```

without useful programmatic context.

---

# 29. QLT-A11Y-013 — Same function is identified consistently

A repeated action should not have unrelated accessible names/icons across the product without reason.

---

# 30. Color

Color is not the only means of conveying:

- error;
- status;
- selection;
- required;
- ownership;
- permission;
- chart series meaning.

---

# 31. QLT-A11Y-014 — State has non-color cue

Use text, icon/shape, programmatic state, pattern, or another meaningful cue.

---

# 32. Text contrast

Applicable normal text targets at least:

```text
4.5:1
```

and large-scale text at least:

```text
3:1
```

subject to WCAG exceptions.

---

# 33. QLT-A11Y-015 — Design tokens do not excuse contrast failure

Theme/token combinations must be evaluated in actual component states:

- default;
- hover;
- disabled;
- placeholder;
- focus;
- error;
- selected.

---

# 34. Non-text contrast

Controls, focus indicators, and meaningful graphics require adequate perceptibility according to applicable WCAG criteria.

---

# 35. Images of text

Use real text when technologies can achieve the intended presentation, except where the visual itself is essential.

---

# 36. Zoom

Text can be resized to 200% without losing content/functionality, subject to applicable standard exceptions.

---

# 37. QLT-A11Y-016 — Zoom does not remove primary actions

At increased text/zoom:

- controls remain reachable;
- content does not become clipped beyond recovery;
- dialogs remain operable;
- horizontal layout adapts as required.

---

# 38. Reflow

Narrow viewport/zoom layouts should avoid unnecessary two-dimensional scrolling except where content such as large data grids genuinely requires it.

---

# 39. Enterprise data grids

Tables/boards may require horizontal scrolling.

Accessibility still requires:

- clear focus;
- row/column context;
- keyboard navigation or alternative;
- reachable actions.

---

# 40. QLT-A11Y-017 — Dense work surface remains operable, not merely visible

Enterprise density is allowed.

It does not waive keyboard, zoom, focus, naming, or state requirements.

---

# 41. Pointer target size

Applicable web pointer targets should satisfy WCAG 2.2 AA target-size requirements or documented exceptions/equivalent control paths.

---

# 42. QLT-A11Y-018 — Tiny icon targets are not default interaction design

When a control must be visually small, spacing/equivalent interaction still protects operability.

Native mobile uses platform-appropriate touch-target guidance.

---

# 43. Dragging

Drag/drop interactions require a non-drag single-pointer method unless dragging is essential.

---

# 44. QLT-A11Y-019 — Dragging has an accessible alternative

Examples:

```text
Move up/down
Move to group
Choose destination
Reorder menu
keyboard move commands
```

depending on the interaction.

---

# 45. Work Management drag/drop

Kanban/group/item reorder must not be drag-only.

The alternative must perform the same product mutation semantics.

---

# 46. Documents block reorder

Block movement/reordering must have keyboard/non-drag path where applicable.

---

# 47. Motion

Non-essential motion respects reduced-motion preference.

Motion is not required to understand state.

---

# 48. QLT-A11Y-020 — Reduced motion preserves information and operation

Turning motion down/off cannot remove the only indication of completion, navigation, or relationship.

---

# 49. Auto-updating content

Auto-refreshing/realtime changes should avoid disorienting focus/reading order.

Update announcements are used selectively.

---

# 50. QLT-A11Y-021 — Dynamic update is announced only when useful

Do not make every realtime event an assertive live-region announcement.

Prioritize meaningful state changes requiring attention.

---

# 51. Loading

Loading states expose meaningful accessible status when users need to wait for an operation.

Skeleton-only visual motion is not sufficient semantics.

---

# 52. Errors

Automatically detected input errors identify the field/problem in text or accessible description.

---

# 53. QLT-A11Y-022 — Form error is programmatically associated with its input

Users can navigate from summary/field to correction and understand the message without color alone.

---

# 54. Labels

Inputs have persistent programmatic labels.

Placeholder is not a label substitute.

---

# 55. Instructions

Required format/constraints are provided before or when needed, not only after repeated failure.

---

# 56. QLT-A11Y-023 — Required/invalid semantics are exposed programmatically

Visual asterisk/red border alone is insufficient.

---

# 57. Error suggestion

When a known correction can be suggested safely, provide useful guidance.

Security-sensitive flows may intentionally avoid hints that would weaken security.

---

# 58. Error prevention

Legal, financial, or destructive data operations should support applicable:

- review;
- confirmation;
- reversibility;
- correction

consistent with product semantics.

---

# 59. QLT-A11Y-024 — High-impact action has accessible review/confirmation

Account deletion, Billing changes, security administration, destructive bulk operations, and similar flows must be operable and understandable with assistive technology.

---

# 60. Redundant entry

Where WCAG applies, avoid forcing users to re-enter previously supplied information in the same process when it can be auto-populated or selected, subject to exceptions.

---

# 61. Authentication

Authentication must not unnecessarily require cognitive-function tests without an accessible alternative/mechanism under WCAG 2.2 AA.

---

# 62. QLT-A11Y-025 — Authentication supports password managers and paste

Do not block paste into password/OTP fields merely as a security superstition.

Credential managers and accessible authentication mechanisms should remain usable.

---

# 63. CAPTCHAs

If abuse protection introduces a cognitive/visual challenge, provide an accessible alternative or choose a less exclusionary control consistent with security requirements.

---

# 64. MFA

MFA flows should provide clear labels, time/expiry state, recovery, resend behavior, and alternatives according to Identity/security policy.

---

# 65. Time limits

Where a user-facing time limit exists, provide applicable warning/extension/control unless the timing is essential or covered by a standard exception.

---

# 66. QLT-A11Y-026 — Session expiry does not cause silent data loss without warning where avoidable

Long editing/admin workflows should handle expiring session/re-authentication accessibly.

---

# 67. Notifications/toasts

Transient messages that contain critical recovery information need a persistent accessible path.

---

# 68. QLT-A11Y-027 — Toast is not sole container for critical information

Use inline/persistent state for errors or actions the user must revisit.

---

# 69. Status messages

Where appropriate, status messages are programmatically determinable without unexpectedly moving focus.

---

# 70. Charts/analytics

Visualizations require meaningful alternative information.

Depending on chart:

- accessible title/summary;
- data table;
- values;
- labels;
- keyboard-accessible detail.

---

# 71. QLT-A11Y-028 — Chart meaning is not encoded only in color or hover

Critical values/comparison remain available without color discrimination, pointer hover, or vision.

---

# 72. Data tables

Semantic tables expose headers/relationships where tabular data is truly a table.

Interactive grids use a deliberate accessible grid/table pattern.

---

# 73. Virtualized lists/grids

Virtualization must preserve enough semantics/focus behavior for assistive access.

Do not lose focus because an off-screen row is recycled unexpectedly.

---

# 74. QLT-A11Y-029 — Virtualization cannot make focused content disappear unpredictably

Focus/selection and accessible row context must remain coherent through scroll/re-render.

---

# 75. Menus

Use menu semantics only for true application/menu behavior.

Ordinary navigation lists should not be converted into menu widgets unnecessarily.

---

# 76. Tabs

Tabs expose:

- selected state;
- tablist/tab/tabpanel relation;
- keyboard behavior;
- focus order

according to the chosen pattern.

---

# 77. Combobox/select

Search/select/autocomplete needs:

- label;
- expanded state;
- active option;
- keyboard navigation;
- clear selection;
- loading/empty/error semantics.

---

# 78. QLT-A11Y-030 — Complex composite follows one consistent keyboard model

Do not invent different arrow/Enter/Escape semantics for every feature.

Prefer tested design-system primitives.

---

# 79. Inline editing

Inline cells/fields need predictable:

- entry;
- save/cancel;
- error;
- focus return;
- read state.

---

# 80. QLT-A11Y-031 — Inline edit failure preserves user context

Validation/concurrency failure must not throw focus away or erase entered content unnecessarily.

---

# 81. Editors

Rich/block editors require dedicated accessibility review.

Consider:

- semantic structure;
- keyboard navigation;
- block controls;
- selection;
- formatting;
- announcements;
- drag alternatives.

---

# 82. QLT-A11Y-032 — Rich editor accessibility is a feature contract

A generic `contenteditable` implementation is not enough evidence.

---

# 83. Mobile semantics

Native mobile controls expose appropriate:

```text
label
role
state
hint where useful
action
touch target
navigation order
```

---

# 84. QLT-A11Y-033 — Mobile parity is semantic, not DOM parity

Do not copy ARIA/web DOM patterns into native components blindly.

Use platform accessibility APIs.

---

# 85. Mobile gestures

Gesture-only functionality needs an accessible action path when the platform pattern requires it.

---

# 86. Orientation

Mobile/web content should not force one orientation unless orientation is essential to function.

---

# 87. Screen-reader navigation

Critical mobile/web screens should have meaningful headings/regions/grouping and avoid excessive unlabeled controls.

---

# 88. Media

If Notrelix product/marketing includes synchronized media, applicable captions/audio-description requirements follow WCAG level target.

---

# 89. QLT-A11Y-034 — Marketing accessibility has the same release-quality status

A conversion/landing page is not exempt from accessibility because it is not the authenticated app.

---

# 90. Localization

Language of page/parts should be programmatically identifiable where applicable.

Localized copy must preserve accessible labels/instructions.

---

# 91. QLT-A11Y-035 — Localization cannot remove semantic labels

An icon/button must not become unlabeled because one locale string is missing.

Fail visibly/safely during development/testing.

---

# 92. Content order

Responsive rearrangement preserves meaningful reading order.

Do not rely entirely on visual position.

---

# 93. Permission state

While permission is unresolved:

- avoid flashing unauthorized content;
- avoid announcing controls that disappear;
- present safe loading/read-only state.

Accessibility and security align here.

---

# 94. QLT-A11Y-036 — Permission transitions preserve focus/context

When permission revokes a visible control/panel, move focus to a logical safe location and communicate the state change where necessary.

---

# 95. Realtime state

Realtime updates should not steal focus.

If the user's edited resource changes remotely, conflict/status information should be accessible.

---

# 96. Workspace switch

Scope transition should provide new-page/resource context without trapping focus in old removed DOM.

---

# 97. Empty states

Empty state text/action should be accessible and respect permission.

Do not show an inaccessible creation path to a user who cannot create.

---

# 98. Read-only state

Disabled controls are not always enough to explain read-only state.

Provide useful explanation where product policy permits.

---

# 99. QLT-A11Y-037 — Disabled state remains understandable

A user should know what action is unavailable and, when appropriate, why/how to resolve it.

---

# 100. Automated testing

Automated accessibility tools catch common defects such as:

- missing accessible names;
- role/state issues;
- some contrast;
- duplicate IDs/markup issues.

They cannot fully test usability.

---

# 101. Current CI evidence

Current frontend CI runs a dedicated `ui-foundation` job with:

```text
Storybook a11y and visual gates
```

through `pnpm test:ui:freeze`.

This is important reusable-primitives evidence.

---

# 102. QLT-A11Y-038 — Primitive accessibility is tested centrally

Design-system primitives carry baseline accessibility tests so feature teams do not independently reimplement buttons/dialogs/menus/forms with inconsistent behavior.

---

# 103. Feature-level evidence

Feature teams still test product-specific composition:

- correct accessible name;
- error association;
- focus after mutation;
- permission state;
- drag alternative;
- keyboard interaction.

Primitive correctness is necessary but not sufficient.

---

# 104. QLT-A11Y-039 — Automated scan does not replace keyboard review

Critical workflows require manual keyboard operation review when interaction complexity warrants it.

---

# 105. Screen-reader review

Use manual screen-reader testing for selected high-risk workflows such as:

- authentication;
- Workspace navigation/switch;
- Board grid/Kanban;
- Documents editor;
- dialogs/menus;
- Billing/security forms.

Exact device/reader matrix may evolve.

---

# 106. QLT-A11Y-040 — Screen-reader evidence targets critical semantics, not exhaustive browser multiplication

Choose representative supported platform/assistive combinations based on actual product support and risk.

---

# 107. Accessibility regression tests

When a defect is fixed, add automated regression where deterministic and valuable.

Do not rely only on institutional memory.

---

# 108. Visual regression

Visual tests can protect focus rings, overflow, zoom-sensitive layout, state labels, and contrast-affecting token changes.

They supplement semantic tests.

---

# 109. QLT-A11Y-041 — Screenshot pass does not certify accessibility

Pixels cannot prove accessible name, role, reading order, keyboard behavior, or live-region semantics.

---

# 110. E2E accessibility

Critical E2E can assert:

- keyboard route;
- focus destination;
- no inaccessible modal trap;
- accessible navigation;
- key form/error behavior.

Do not attempt to replace component-level checks entirely with E2E.

---

# 111. Manual review triggers

Manual accessibility review is especially required for changes to:

```text
new custom composite
drag/drop
rich editor
virtualized grid
complex chart
modal/focus manager
keyboard shortcuts
authentication
destructive financial/data flow
mobile gesture
```

---

# 112. Accessibility evidence matrix

| Property | Preferred evidence |
|---|---|
| Name/role/state | component automated test |
| Keyboard behavior | component/integration + manual |
| Focus lifecycle | integration/E2E + manual |
| Contrast/token | automated/design review |
| Zoom/reflow | browser/manual + visual where useful |
| Drag alternative | interaction test + manual |
| Form error association | component/integration |
| Live/dynamic status | integration + screen-reader review |
| Rich editor | dedicated integration/manual |
| Mobile semantics | native component/manual |
| Critical journey | selected E2E/manual |

---

# 113. QLT-A11Y-042 — Evidence matches the interaction property

Do not claim keyboard correctness from an axe scan or screen-reader correctness from screenshot comparison.

---

# 114. Accessibility bug severity

Severity considers:

```text
blocked task
criticality of workflow
absence of alternative
number of users/surfaces affected
security/financial/destructive impact
primitive/systemic scope
```

---

# 115. QLT-A11Y-043 — Blocked critical workflow is release-blocking by default

Examples:

- cannot login;
- cannot submit required form;
- cannot close modal;
- cannot perform required Board action;
- destructive action cannot be reviewed/cancelled;
- payment/security control has no accessible operation.

Explicit exception requires governance.

---

# 116. Design-system change

Changing primitive keyboard/focus/ARIA/token behavior has broad blast radius.

Review dependent hosts/features.

---

# 117. QLT-A11Y-044 — Primitive regression has system-wide impact

A Button/Dialog/Select/Grid primitive change requires central tests plus representative composition evidence when risk warrants it.

---

# 118. Vendor/generated components

Third-party/shadcn-derived code is not exempt.

If it becomes owned product source, Notrelix owns accessibility of the shipped result.

---

# 119. QLT-A11Y-045 — Upstream component pedigree is not accessibility evidence

Run Notrelix's own tests/review under its composition, theme, and interaction changes.

---

# 120. New component admission checklist

```text
[ ] native semantic available?
[ ] accessible name
[ ] role/state
[ ] keyboard
[ ] focus visible/order
[ ] disabled/read-only
[ ] error/loading
[ ] pointer/touch
[ ] reduced motion
[ ] zoom/reflow
[ ] screen-reader behavior
[ ] mobile equivalent if shared capability
```

---

# 121. Form checklist

```text
[ ] label
[ ] required semantics
[ ] instructions
[ ] autocomplete/input purpose where appropriate
[ ] error association
[ ] correction guidance
[ ] focus on submit failure
[ ] review/confirm for high-impact action
[ ] no color-only error
[ ] authentication/password-manager compatibility
```

---

# 122. Dialog checklist

```text
[ ] semantic dialog role/name
[ ] logical initial focus
[ ] keyboard containment if modal
[ ] Escape/close behavior where allowed
[ ] background interaction blocked if modal
[ ] focus restored
[ ] content scroll/zoom remains usable
[ ] no obscured focus
```

---

# 123. Drag/reorder checklist

```text
[ ] drag is not sole path
[ ] keyboard/single-pointer alternative
[ ] destination/state communicated
[ ] focus retained/restored
[ ] operation result announced where useful
[ ] same product semantics as drag
```

---

# 124. Dynamic/realtime checklist

```text
[ ] update does not steal focus
[ ] critical status programmatically available
[ ] live region not noisy
[ ] reconnect/conflict state understandable
[ ] optimistic rejection recoverable
[ ] permission revocation preserves safe focus
```

---

# 125. Mobile checklist

```text
[ ] native label/role/state
[ ] touch target
[ ] navigation order
[ ] gesture alternative
[ ] dynamic type/large text
[ ] orientation
[ ] screen-reader action
[ ] focus after navigation/modal
```

---

# 126. Change impact — primitive

Review:

```text
all hosts using primitive
Storybook a11y/visual
keyboard/focus
theme/contrast
mobile/web split
```

---

# 127. Change impact — complex Work Management interaction

Review:

```text
keyboard
drag alternative
grid semantics
focus
selection
inline edit
zoom
realtime conflict
screen reader
```

---

# 128. Change impact — Documents editor

Review:

```text
block semantics
keyboard navigation
formatting controls
block move alternative
selection/focus
screen reader
zoom/mobile
```

---

# 129. Change impact — authentication

Review:

```text
accessible authentication
password manager/paste
MFA
error identification
session timeout
focus
screen reader
```

---

# 130. Change impact — destructive/Billing

Review:

```text
error prevention
confirmation/review
labels
keyboard
focus
readable consequences
screen reader
```

---

# 131. Current source alignment

Current frontend UI architecture explicitly requires:

```text
keyboard, pointer and touch match platform expectations
loading/error/empty/permission/conflict are first-class
accessibility is part of component contract
```

Current frontend CI includes a dedicated Storybook accessibility/visual gate.

Those are existing executable foundations; this standard defines the broader product quality contract.

---

# 132. Stop conditions

Stop rather than merge if:

- primary action is pointer/drag only;
- a custom control lacks role/name/keyboard/state;
- focus disappears or becomes trapped;
- sticky/overlay UI entirely obscures focus;
- form error is only red border;
- placeholder is the only label;
- chart meaning is only color/hover;
- zoom/large text removes critical functionality;
- authentication blocks password managers/paste without justified accessible alternative;
- critical toast disappears with no persistent recovery path;
- automated a11y pass is the only evidence for a new complex grid/editor/drag flow;
- mobile copies web ARIA instead of native accessibility APIs;
- primitive change breaks accessibility but is accepted because vendor/shadcn supplied it.

---

# 133. Related canonical owners

```text
DESIGN.md
docs/product/product-experience.md
docs/quality/engineering-quality-standard.md
docs/quality/testing-strategy.md
docs/quality/security-quality-standard.md
docs/quality/performance-and-scalability.md
frontend/docs/architecture/ui-and-design-system.md
frontend/docs/architecture/hosts-composition-routing.md
frontend/docs/architecture/state-query-mutations.md
frontend/docs/architecture/realtime.md
```

---

# 134. Final accessibility rule

For every user-facing change, answer:

```text
Can it be perceived without relying on color/motion alone?
Can it be operated without a mouse or drag-only gesture where required?
Is focus visible, ordered, and restored correctly?
Are name/role/state/error semantics programmatic?
Does zoom/large text preserve the task?
Does dynamic/realtime state remain understandable?
Does high-impact form/authentication behavior satisfy error-prevention and accessible-auth needs?
Does mobile use native accessibility semantics?
What automated evidence exists?
What manual keyboard/screen-reader review is still required?
```

The target is:

> **an enterprise product whose dense, dynamic, multi-host interactions remain perceivable, operable, understandable, and robust for people using keyboard, assistive technology, zoom, touch, and alternative interaction methods.**
