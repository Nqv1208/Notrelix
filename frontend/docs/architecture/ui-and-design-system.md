---
document_id: FE-ARCH-UI-DESIGN-SYSTEM
document_type: architecture
status: active
owner: frontend-platform
applies_to:
  - frontend-ui
  - design-tokens
  - web-ui
  - mobile-ui
  - frontend-theme
  - frontend-accessibility
  - frontend-motion
  - frontend-storybook
evidence:
  - frontend/packages/ui/tokens/
  - frontend/packages/ui/web/
  - frontend/packages/ui/mobile/
  - frontend/packages/ui/icons/
  - frontend/tooling/storybook/web/
  - frontend/e2e/ui/
  - frontend/playwright.storybook.config.ts
  - frontend/package.json
review_on:
  - token-model-change
  - theme-model-change
  - ui-platform-boundary-change
  - component-public-contract-change
  - accessibility-foundation-change
  - motion-foundation-change
  - visual-regression-model-change
  - marketing-design-system-change
---

# UI and Design System

> **The Notrelix design system separates semantic design tokens from platform rendering implementations.**
>
> `ui-tokens` owns framework-neutral design semantics. `ui-web` and `ui-mobile` own platform primitives. Product/feature packages compose those primitives into product UI. Accessibility is a release property. Light/dark mode and accent/color theme are separate axes. Components consume semantic tokens rather than creating competing brand palettes.

This document is the canonical frontend owner for:

- design-token architecture;
- primitive versus semantic tokens;
- color/brand/surface roles;
- light/dark/system appearance;
- accent/color themes;
- theme persistence/application;
- web/mobile UI separation;
- reusable component ownership;
- product component ownership;
- accessibility foundation;
- focus/keyboard/touch behavior;
- motion/reduced motion;
- responsive/density principles;
- Storybook;
- visual regression;
- UI testing;
- marketing reuse.

It does not define:

- exact marketing page visual design;
- individual product screen specification;
- backend product semantics;
- package dependency allow-lists;
- exact CI job topology.

---

# 1. Design-system objective

The design system should provide:

```text
coherent brand
semantic hierarchy
light/dark parity
accessible interaction
web/mobile consistency
controlled motion
predictable component APIs
reviewable visual change
```

without forcing identical rendering implementation across platforms.

---

# 2. FE-UI-001 — Design semantics are centralized before component styling

Reusable visual meaning SHOULD originate from:

```text
tokens
theme semantics
component variants
```

rather than repeated arbitrary literals in feature screens.

One-off layout values are allowed where no semantic token is warranted.

---

# 3. UI package split

Current package family:

```text
@notrelix/ui-tokens
@notrelix/ui-web
@notrelix/ui-mobile
@notrelix/ui-icons
```

The split separates semantic design data from platform rendering.

---

# 4. FE-UI-002 — Tokens are framework-neutral

`ui-tokens` MUST NOT depend on:

```text
React
React DOM
React Native rendering
Next.js
Vite
Expo
```

for its design-value model.

Current source explicitly documents framework-neutral token files.

---

# 5. Current token categories

Current token exports include:

```text
primitive colors
brand
semantic state colors
surface colors
gradients
badge colors
typography
spacing/layout/grid
radius
shadows
motion
semantic surfaces
focus ring
light theme
dark theme
```

as current implementation evidence.

---

# 6. FE-UI-003 — Primitive and semantic token roles are distinct

Primitive:

```text
violet
paper
slate
```

answers:

```text
what value?
```

Semantic:

```text
primary
background
border
danger
focus
card
```

answers:

```text
what role?
```

Components SHOULD prefer semantic roles when styling product UI.

---

# 7. Primitive palette

Primitive values are low-level inputs.

They are useful to build brand/semantic themes.

---

# 8. FE-UI-004 — Components do not default to primitive color literals for semantic meaning

Avoid:

```text
button background = brand.violet literal
error border = #f64932
```

inside many components when:

```text
primary
destructive
border
```

semantic tokens exist.

This preserves theme evolution.

---

# 9. Brand palette

Current token source defines brand colors including violet/indigo/purple/ocean/sky/frost and gradients.

Brand palette is a design input.

---

# 10. FE-UI-005 — Brand palette has one token authority

Do not introduce competing hard-coded “Notrelix primary” definitions in:

```text
marketing CSS
feature component
app shell
Storybook
```

without routing them through the token/theme authority.

A design refresh changes token authority first.

---

# 11. Current primary-source alignment risk

Current source contains several representations of primary/brand intent:

```text
brand.violet
light/dark --primary
COLOR_THEMES.default primaryColor
theme-* accent behavior
```

Different representations can be valid if they map to semantic roles.

They become source debt if they independently define competing visual authority.

---

# 12. FE-UI-006 — Multiple color representations must map through one semantic theme contract

The system MUST define:

```text
which token drives interactive primary
which value is only a preview swatch
which values are primitives
which values are theme variables
```

Do not let every representation independently style components.

---

# 13. Semantic states

Current token source defines:

```text
success
warning
danger
info
```

Semantic state color is not product status meaning by itself.

---

# 14. FE-UI-007 — Product status maps to semantic presentation explicitly

Example:

```text
Work item "stuck"
→ may map to danger-like presentation
```

but the UI token does not own the product lifecycle/status semantics.

Product owner chooses mapping.

---

# 15. Surfaces

Current semantic surfaces define roles such as:

```text
underlay
canvas
raised
card
overlay
toast
```

for light/dark.

---

# 16. FE-UI-008 — Surface role follows elevation/context

Prefer:

```text
canvas
card
overlay
```

over arbitrary background shades per component.

This keeps light/dark hierarchy coherent.

---

# 17. Light/dark themes

Current token source exports explicit:

```text
lightTheme
darkTheme
```

CSS-variable mappings.

Current web `ThemeProvider` supports:

```text
light
dark
system
```

appearance.

---

# 18. FE-UI-009 — Appearance mode is one axis

Appearance mode answers:

```text
light?
dark?
system?
```

It is separate from:

```text
accent/color theme
```

Do not combine both concepts into one giant enum such as:

```text
dark-ocean
light-sage
...
```

unless a future theme architecture deliberately changes.

---

# 19. Color/accent themes

Current `useColorTheme()` exposes themes such as:

```text
default
editorial
sage
ocean
sunset
midnight
rose
aurora
```

and applies a `theme-*` class.

This is a second theme axis.

---

# 20. FE-UI-010 — Accent theme modifies semantic accent roles, not arbitrary component palettes

A color theme SHOULD influence approved semantic variables.

Components should not contain:

```text
if ocean → blue
if sage → green
```

branching across the codebase.

---

# 21. Theme persistence

Current web theme provider accepts injected storage and uses keys such as:

```text
notrelix-ui-theme
notrelix-color-theme
```

for appearance/accent persistence.

---

# 22. FE-UI-011 — Theme persistence is runtime-owned, theme semantics are UI-owned

UI web defines:

```text
allowed theme modes
how classes/variables apply
```

Host/runtime provides storage capability.

Do not make UI token package read browser localStorage.

---

# 23. System theme

Current ThemeProvider listens to:

```text
prefers-color-scheme: dark
```

when mode is `system`.

---

# 24. FE-UI-012 — System appearance follows OS changes while system mode is active

Switching OS appearance should update the rendered theme without requiring reload when platform supports it.

When user explicitly selects light/dark, OS changes should not override that choice.

---

# 25. Theme flash

Current web exports `colorThemeScript` intended to apply stored accent theme before rendering.

Appearance/theme initialization should minimize flash/hydration mismatch.

---

# 26. FE-UI-013 — Initial theme is applied before visually stable paint where feasible

For web/server-rendered surfaces, avoid:

```text
light flash
→ dark
or
default accent flash
→ selected accent
```

when pre-hydration initialization can safely apply the stored preference.

---

# 27. Theme transition

Light/dark transitions can animate selective visual properties.

Over-animation can produce noise and reduced-motion issues.

---

# 28. FE-UI-014 — Theme transition is coherent and bounded

Do not transition every property globally.

Prefer approved:

```text
color
background
border
shadow
```

durations where useful.

Respect reduced-motion/accessibility.

---

# 29. Motion tokens

Current motion tokens include duration categories:

```text
instant
fast
base
slow
deliberate
```

and easing roles.

---

# 30. FE-UI-015 — Motion uses semantic duration/easing tokens

Reusable components SHOULD NOT invent unique transition curves/durations for ordinary interactions when token roles suffice.

Special product motion can define a justified local contract.

---

# 31. Motion meaning

Motion can communicate:

```text
state change
hierarchy
continuity
feedback
```

It should not exist solely for decorative busyness in productivity workflows.

---

# 32. FE-UI-016 — Motion supports task comprehension

Avoid motion that:

```text
delays common actions
moves content unpredictably
distracts from dense work surfaces
```

Productivity UI should remain fast and controlled.

---

# 33. Reduced motion

Animations must account for user reduced-motion preference where meaningful.

---

# 34. FE-UI-017 — Reduced motion preserves functionality

Disabling/reducing animation MUST NOT remove:

```text
state feedback
focus
completion indication
navigation access
```

Provide non-motion cues.

---

# 35. Spacing

Current token source uses an 8-based layout system with explicit spacing values and layout/grid roles.

Exact values are implementation/design evidence.

---

# 36. FE-UI-018 — Spacing tokens express repeated layout rhythm

Use tokens for repeated component/system spacing.

Do not turn every unique composition gap into a new global token.

---

# 37. Density

Enterprise work-management UI requires both:

```text
comfortable general UI
dense data surfaces
```

such as Table/Board.

Density is a semantic interaction/layout contract, not random per-screen padding reduction.

---

# 38. FE-UI-019 — Dense product surfaces use explicit density semantics

If Table/Board needs compact density, define:

```text
row height
control height
padding
type scale
hit-area strategy
```

through an approved density contract/variant.

Do not scatter `px-1`, `h-6`, custom overrides across product cells.

---

# 39. Current density status

Current tokens expose spacing/layout/table-surface primitives, but a complete cross-component density contract must be proven by source/galleries/tests before claiming it frozen.

Do not infer a finished density system solely from spacing tokens.

---

# 40. FE-UI-020 — Density completeness requires component-level evidence

A density token/variant is complete only when representative:

```text
table
board
form/control
menu
```

components demonstrate coherent behavior and accessibility.

---

# 41. Typography

Typography tokens own:

```text
font families
weights
type scale
```

according to current token package.

---

# 42. FE-UI-021 — Typography hierarchy is semantic

Use roles such as:

```text
page title
section heading
body
label
caption
```

through system variants/tokens.

Do not choose font size by component whim where a semantic role exists.

---

# 43. Radius

Radius tokens define visual shape language.

Do not create a different radius system per feature.

---

# 44. FE-UI-022 — Radius communicates one system language

Feature/product components MAY choose among approved radius roles.

They SHOULD NOT introduce a parallel design language without an intentional design change.

---

# 45. Shadows/elevation

Shadows are one part of elevation.

Dark mode may require different perceived elevation than light mode.

---

# 46. FE-UI-023 — Elevation is semantic, not copied shadow literals

Use:

```text
surface role
border
shadow
overlay
```

together to preserve hierarchy across themes.

---

# 47. Focus ring

Current semantic token source defines a focus ring role.

Focus visibility is an accessibility contract.

---

# 48. FE-UI-024 — Interactive controls have visible keyboard focus

Do not remove outline/focus indication without an accessible replacement.

Focus style must remain visible in light/dark/accent themes.

---

# 49. Web UI package

Current `@notrelix/ui-web` depends on `ui-tokens` and web component libraries including Radix primitives and other UI libraries.

It exports root, UI/component subpaths, theme, and logo asset.

---

# 50. FE-UI-025 — ui-web owns reusable web primitives, not product screens

Examples suitable for `ui-web`:

```text
Button
Dialog
Popover
Input
Tabs
Tooltip
generic table primitive
```

Product-specific:

```text
BoardCard
AutomationRuleBuilder
BillingPlanCard with entitlement semantics
```

belongs outward.

---

# 51. Third-party primitives

Radix/shadcn-style component code can provide implementation primitives.

The Notrelix public component API/design semantics remain owned by Notrelix packages.

---

# 52. FE-UI-026 — Third-party primitive does not become architecture authority

Do not expose library-specific behavior everywhere merely because the implementation uses Radix/cmdk/etc.

Wrap/adapt only where Notrelix needs a stable design-system contract.

Do not wrap blindly when direct public use is already the intentional package API.

---

# 53. Vendored component changes

Imported/generated UI source may require local adaptations.

Quality rules can use narrow exceptions.

---

# 54. FE-UI-027 — Vendored/generated UI does not justify global quality-rule weakening

If shadcn/generated code conflicts with formatting/lint style:

```text
use narrow configuration
or
normalize generated source intentionally
```

Do not disable quality checks repository-wide.

---

# 55. Mobile UI package

Current `@notrelix/ui-mobile` depends on:

```text
ui-tokens
React
React Native
```

and not `ui-web`.

---

# 56. FE-UI-028 — Mobile UI is a native implementation

Do not reuse web UI by rendering/importing DOM components into native.

Share:

```text
tokens
semantic component intent
product behavior
```

then implement native controls.

---

# 57. Cross-platform component semantics

Web/mobile can share conceptual component APIs where useful.

Exact prop API need not be identical if platform interaction differs.

---

# 58. FE-UI-029 — Semantic parity does not require prop-for-prop parity

A web Tooltip and mobile explanatory pattern can differ.

Do not force a desktop-hover concept into native solely for API symmetry.

---

# 59. Icons

`ui-icons` is a narrow reusable visual package.

Icons should support accessible use by consumers.

---

# 60. FE-UI-030 — Icon alone is not an accessible label by default

Icon-only interactive control needs:

```text
accessible name
tooltip/help where appropriate
focus/touch behavior
```

Do not rely on glyph recognition.

---

# 61. Component ownership

Classify a component before placing it:

```text
generic UI primitive
product presentation
feature composition
host shell
marketing section
```

---

# 62. FE-UI-031 — Component location follows semantics, not visual reuse alone

A component shown on two screens is not automatically design-system primitive.

If it knows product meaning, keep it with the product/feature owner.

---

# 63. Component public API

Reusable component API should encode stable semantics and variants.

Avoid giant boolean matrices.

---

# 64. FE-UI-032 — Variants model meaningful visual/interaction roles

Prefer:

```text
variant="primary"
size="sm"
density="compact"
```

over dozens of unrelated one-off boolean styling props.

Do not over-generalize every CSS property into a prop.

---

# 65. Button hierarchy

Primary actions should have a coherent semantic hierarchy across product and marketing surfaces.

One screen should not invent a different primary-button language.

---

# 66. FE-UI-033 — Primary action styling derives from semantic brand/theme contract

Marketing MAY use a richer branded primary treatment such as gradient/motion if approved.

It should still derive colors/motion from design-system semantics and preserve accessible states.

Do not create an unrelated marketing-only brand palette.

---

# 67. Gradient

Current tokens include reusable brand gradients.

Gradient is a visual technique, not a default for every control.

---

# 68. FE-UI-034 — Gradient usage is intentional and role-based

Use gradients where they support:

```text
brand emphasis
hero/marketing CTA
selected premium emphasis
```

not across every surface, which would dilute hierarchy and create theme noise.

---

# 69. Marketing UI reuse

Current marketing manifest may consume:

```text
ui-tokens
ui-web
ui-icons
```

Marketing can extend composition/styling for brand storytelling.

---

# 70. FE-UI-035 — Marketing extension cannot fork the core design language silently

If marketing needs a new reusable:

```text
brand gradient
motion
button treatment
surface
```

promote the semantic primitive/token when it belongs to the system.

Keep one-off campaign art/content local.

---

# 71. Dark/light parity

A component is not finished if it only looks correct in one appearance mode when both are supported.

---

# 72. FE-UI-036 — Every reusable web component supports light and dark semantic themes

Test meaningful states:

```text
default
hover
focus
pressed
disabled
error
selected
```

in both appearance modes where applicable.

Do not patch dark mode with scattered unrelated hex values.

---

# 73. Accent-theme parity

Accent themes should not break:

```text
contrast
focus
destructive semantics
disabled state
selected state
```

---

# 74. FE-UI-037 — Accent theme cannot override semantic safety roles arbitrarily

An accent palette should not turn:

```text
danger
warning
focus
disabled
```

into ambiguous brand-only styling.

Semantic safety/accessibility takes precedence.

---

# 75. Color contrast

Repository accessibility standard owns normative contrast requirements.

UI tokens/components must make compliance practical.

---

# 76. FE-UI-038 — Contrast is verified in rendered states

Token-level color values alone do not prove:

```text
text/background
icon/background
focus/border
disabled
```

contrast for every component.

Use automated/manual checks at rendered component level.

---

# 77. Keyboard

Web interactive controls need keyboard semantics appropriate to their role.

Prefer native elements/accessible primitives where practical.

---

# 78. FE-UI-039 — Clickable div is not the default interactive primitive

Use:

```text
button
link
input
semantic Radix/native control
```

according to interaction.

If custom role is necessary, implement full keyboard/focus semantics.

---

# 79. Focus management

Dialogs, menus, popovers and route transitions can require focus management.

---

# 80. FE-UI-040 — Composite/overlay components define focus entry, trap/containment where appropriate, and return

Do not leave keyboard focus behind a modal or lose it to document body after close.

Use tested accessible primitives.

---

# 81. Forms

Inputs require labels, error association and state semantics.

---

# 82. FE-UI-041 — Form error is programmatically associated with its control

Do not rely only on red border/color.

Use text, ARIA relationships, and correct validation semantics.

---

# 83. Disabled versus read-only

These have different semantics.

---

# 84. FE-UI-042 — Disabled and read-only states are not interchangeable

Use the state matching product interaction contract.

Preserve accessibility and submission behavior.

---

# 85. Touch targets

Mobile and touch web need adequate hit areas.

Dense visual layout can preserve larger invisible/semantic hit target when appropriate.

---

# 86. FE-UI-043 — Visual density does not reduce interaction target below accessibility requirements

A compact table row can still provide an accessible actionable region.

Do not make dense mode unusable on touch/keyboard.

---

# 87. Responsive design

Web layout should adapt across supported viewport classes.

Current tokens expose desktop/tablet/mobile grid roles.

---

# 88. FE-UI-044 — Responsive behavior is component/product semantics, not only global CSS breakpoints

A Board, Table, Dialog and Navigation shell can require different adaptation.

Do not expect one grid breakpoint set to solve every component.

---

# 89. Mobile is not responsive web

Native mobile is a separate rendering host.

Responsive web handles browser widths.

---

# 90. FE-UI-045 — Responsive web does not replace native mobile architecture

Do not use “it works at 390px” as evidence the mobile app is implemented.

---

# 91. Loading

Reusable components can provide generic loading primitives.

Product surfaces own contextual loading UX.

---

# 92. FE-UI-046 — Loading state preserves layout and interaction expectations

Avoid:

```text
random spinner replacing entire dense workspace
```

when skeleton/progressive content better preserves context.

Do not announce loading excessively to assistive technology.

---

# 93. Empty/error/permission states

These states can use shared primitives but product copy/actions belong to feature/product UX.

---

# 94. FE-UI-047 — Visual similarity does not collapse semantic state distinctions

Do not render:

```text
no data
no permission
not found
filtered empty
network error
```

as one generic “Nothing here” state when recovery/actions differ.

---

# 95. Toast

Current `ui-web` depends on Sonner as current implementation evidence.

Toast is suitable for transient feedback, not all errors.

---

# 96. FE-UI-048 — Toast does not replace persistent actionable state

Use toast for:

```text
brief confirmation
non-blocking notice
```

Use inline/dialog/page state when user must:

```text
correct
retry
compare
decide
```

---

# 97. Dialog/overlay

Overlays should preserve:

```text
focus
escape/close semantics
screen-reader labeling
background interaction rules
```

according to component role.

---

# 98. FE-UI-049 — Overlay accessibility is part of primitive contract

Product features should not reimplement dialog focus/ARIA behavior independently.

Use the approved primitive.

---

# 99. Tables/data grids

Enterprise tables require semantics for:

```text
row/cell
selection
sorting
keyboard
density
virtualization
loading
empty
```

depending on implementation.

---

# 100. FE-UI-050 — Data-table primitive separates generic mechanics from product columns/data semantics

Generic UI may own:

```text
layout
keyboard
selection primitive
resize affordance
```

Product package owns:

```text
Board field semantics
Billing columns
permission actions
```

---

# 101. Board/Kanban

Board presentation is product-specific even when it consumes generic cards/buttons/popovers.

---

# 102. FE-UI-051 — Work Management views share design language but remain product-owned

Do not move:

```text
Kanban card
Timeline item
Board group header
```

into generic `ui-web` because they appear often.

---

# 103. Editor/document UI

Rich editor UI can require specialized primitives/selection behavior.

Generic design-system tokens remain reusable.

---

# 104. FE-UI-052 — Specialized editor interaction may be product-owned

Do not force complex editor state into generic input primitives solely for component reuse.

---

# 105. Storybook

Current Storybook web tooling depends on:

```text
ui-tokens
ui-web
```

and uses Storybook/Vite with accessibility addon.

---

# 106. FE-UI-053 — Storybook is a design-system verification surface

Use Storybook to demonstrate:

```text
component variants
themes
states
accessibility
density
```

for reusable UI.

Storybook stories do not become production architecture authority.

---

# 107. Story coverage

A reusable component should show states meaningful to its public contract.

---

# 108. FE-UI-054 — Story matrix follows risk, not combinatorial completeness

Include high-value states:

```text
light/dark
default/hover/focus/disabled
error
compact
long content
```

as applicable.

Do not generate every prop Cartesian product if it adds no review value.

---

# 109. Accessibility tests

Current Storybook tooling includes `@storybook/addon-a11y`.

Root commands include:

```bash
pnpm test:ui:a11y
```

using Playwright/axe integration in UI E2E.

---

# 110. FE-UI-055 — Automated accessibility checks are required but not sufficient

Automation can detect many issues.

Manual/reasoned review still covers:

```text
focus order
meaningful labels
keyboard workflow
screen-reader UX
motion
touch ergonomics
```

---

# 111. Visual regression

Current Playwright Storybook config supports screenshot comparison with animations disabled.

Root command:

```bash
pnpm test:ui:visual
```

---

# 112. FE-UI-056 — Visual snapshots protect intentional contracts

When snapshot changes:

```text
review design intent
theme/token effect
layout change
browser/font instability
```

before updating baseline.

Do not approve baseline blindly to make CI green.

---

# 113. Screenshot tolerance

Current threshold/diff values are test configuration evidence.

They are not design-system semantics.

---

# 114. FE-UI-057 — Snapshot threshold does not define acceptable UX regression

A visually important change can be below pixel threshold.

A harmless anti-aliasing change can exceed it.

Review remains required.

---

# 115. UI freeze suite

Current root exposes:

```bash
pnpm test:ui:freeze
```

which runs the Storybook Playwright suite.

Freeze evidence means tested foundation remains stable.

It does not mean UI can never evolve.

---

# 116. FE-UI-058 — UI freeze protects contract, not visual stagnation

Intentional design-system change:

```text
update tokens/components
update stories/tests
review visual diff
```

through architecture/design change process.

Do not disable tests to preserve velocity.

---

# 117. Product visual tests

Product-specific components can have their own visual/interaction tests where risk warrants.

Do not place every product story into design-system Storybook if it creates false ownership.

---

# 118. FE-UI-059 — Verification location follows component owner

```text
ui-web primitive
→ design-system Storybook/test

Work Management component
→ product adapter/feature test
```

Shared test tooling can remain centralized.

---

# 119. Theme testing

Theme-capable components should be exercised in supported appearance/accent combinations based on risk.

---

# 120. FE-UI-060 — Theme tests target semantic boundaries

At minimum prove critical primitives/surfaces across:

```text
light
dark
primary/accent
focus
destructive
```

rather than snapshotting every accent theme for every atom unless needed.

---

# 121. Token testing

Pure tokens can have structural tests if valuable.

Rendered tests prove actual CSS/component integration.

---

# 122. FE-UI-061 — Token existence does not prove token consumption

A component hardcoding a color can pass token unit tests.

Architecture/lint/review and rendered evidence must ensure design-system adoption.

---

# 123. CSS variables

Web theme uses CSS custom properties for semantic roles.

This enables light/dark/accent remapping without component rewrites.

---

# 124. FE-UI-062 — Web components consume semantic CSS variables/classes instead of theme branching when practical

Prefer:

```text
var(--primary)
var(--background)
semantic utility
```

over:

```ts
theme === "dark" ? "#..." : "#...";
```

scattered in components.

---

# 125. Native tokens

Mobile consumes TypeScript token values through native StyleSheet/component implementation.

It cannot rely on browser CSS variables.

---

# 126. FE-UI-063 — Cross-platform tokens expose semantic data, not CSS-only assumptions

If a semantic token must be shared with mobile, make the core value/model available without requiring DOM/CSS parsing.

Web can derive CSS variables from it.

---

# 127. CSS-only token export

`ui-tokens` package currently advertises a `./css` export in package metadata.

The exact source/output path must remain valid and generated/maintained according to package contract.

---

# 128. FE-UI-064 — Declared token export must exist and be verified

Package export metadata is executable contract.

Do not leave:

```text
package.json export
→ missing file
```

or stale generated CSS.

Architecture/build tests should catch it.

---

# 129. Theme API

Current `ui-web/theme` exports:

```text
ThemeProvider
useTheme
useColorTheme
COLOR_THEMES
colorThemeScript
```

as current evidence.

---

# 130. FE-UI-065 — Theme API separates state, persistence and application

A theme API should make clear:

```text
selected preference
effective appearance
accent theme
storage owner
DOM/native application
```

Do not make callers infer effective dark/light from raw stored enum repeatedly.

---

# 131. Theme context fallback

Context/provider APIs should fail clearly or provide a deliberate safe default.

Silent fallback can hide missing provider wiring.

---

# 132. FE-UI-066 — Missing required UI provider is not silently accepted unless fallback is intentional

For required theme/runtime providers, tests should prove behavior when absent.

Do not hide composition bugs with no-op defaults without rationale.

---

# 133. Storage failure

Current theme implementation catches unavailable storage.

UI can continue with in-memory/default theme.

---

# 134. FE-UI-067 — Preference persistence failure degrades safely

A storage failure SHOULD NOT make the entire app unusable.

Use default/in-memory theme and safe diagnostics if needed.

---

# 135. Color-theme metadata

Preview swatches/descriptions are UI metadata.

They are not necessarily the exact semantic primary token.

---

# 136. FE-UI-068 — Theme preview metadata is not styling authority

`primaryColor` used to render a theme swatch MUST NOT become an independent source used by product components.

Actual styling comes through theme/token semantics.

---

# 137. Localization

Design-system component APIs should not hardcode product-visible copy where consumers need localization.

Generic accessible defaults can be configurable.

---

# 138. FE-UI-069 — Product copy belongs to product/feature owner

Design-system primitives should not own phrases such as:

```text
Delete Board
Upgrade plan
Workspace removed
```

They can own generic labels only when part of primitive contract.

---

# 139. Internationalization layout

Text expansion can affect component width/layout.

Reusable components should tolerate reasonable content expansion.

---

# 140. FE-UI-070 — Component contract avoids fixed-width assumptions tied to one language

Test long labels/content for high-risk reusable primitives.

Use truncation only when UX specifies it and expose full content accessibly.

---

# 141. Destructive action

Destructive visual state uses semantic destructive tokens and clear confirmation where product risk requires.

---

# 142. FE-UI-071 — Color alone does not communicate destructive consequence

Use:

```text
label
icon where useful
confirmation/copy
```

according to risk.

Do not rely only on red.

---

# 143. Disabled action

Disabled controls should explain why when the reason is non-obvious and important.

Do not use disabled state to hide permission/business errors silently.

---

# 144. FE-UI-072 — Disabled UX preserves discoverability where appropriate

For unavailable feature/permission:

```text
hide
disable
explain
```

based on product UX/security contract.

UI state is not backend enforcement.

---

# 145. Skeletons

Skeleton shape should approximate final layout and not misrepresent unavailable content.

---

# 146. FE-UI-073 — Skeleton is presentation placeholder, not fake product data

Do not render realistic fake values that users can mistake for actual Workspace data.

---

# 147. Charts

Current `ui-web` depends on Recharts as implementation evidence.

Chart primitive should preserve accessibility/fallback data semantics.

---

# 148. FE-UI-074 — Data visualization does not hide the underlying meaning

Provide accessible labels/table/text summary where product/accessibility requires.

Do not encode meaning only by color.

---

# 149. Carousel/resizable panels/etc.

Third-party interaction components must be adapted to Notrelix accessibility/theme contracts.

---

# 150. FE-UI-075 — Library default is not automatically Notrelix quality

Verify:

```text
keyboard
focus
theme
motion
touch
responsive
```

for adopted primitives.

---

# 151. Design-system architecture change

Changes to:

```text
token authority
theme axes
ui-web/ui-mobile split
public primitive API
density foundation
accessibility foundation
```

can be architecture-significant.

---

# 152. FE-UI-076 — Foundational UI change updates consumers and verification atomically

For a semantic token/component contract change:

```text
tokens/component
→ stories
→ product consumers
→ a11y
→ visual regression
→ docs
```

should remain aligned.

---

# 153. Token migration

Renaming/removing semantic token requires consumer migration.

Avoid indefinite aliases unless compatibility period is intentional.

---

# 154. FE-UI-077 — One semantic role has one active token authority per migration phase

Do not maintain:

```text
old-primary
new-primary
marketing-primary
app-primary
```

indefinitely.

Define cutover/removal.

---

# 155. Design drift

Source can contain one-off colors/spacing or duplicate theme concepts.

Classify rather than copy.

---

# 156. FE-UI-078 — Existing hard-coded visual value is not precedent

If a component bypasses semantic tokens:

```text
review whether it is legitimate local art
or
SOURCE_DEBT
```

Do not propagate the literal across new components.

---

# 157. Marketing visual evolution

Marketing can be more expressive than dense product UI.

It still uses the same brand semantics.

---

# 158. FE-UI-079 — Marketing expressiveness and product clarity have different motion/density budgets

Marketing MAY use:

```text
gradient borders
ambient background
controlled hero motion
```

while product workspace remains calmer/dense.

Both derive from the same brand/tokens and accessibility constraints.

---

# 159. UI performance

Large tables/boards can require virtualization and reduced render churn.

Performance mechanism belongs to product/UI implementation.

---

# 160. FE-UI-080 — Performance optimization preserves accessible semantics

Virtualization MUST consider:

```text
keyboard navigation
focus retention
screen-reader semantics
scroll restoration
```

Do not trade accessibility away silently.

---

# 161. New token checklist

```text
[ ] semantic need
[ ] existing token insufficient
[ ] platform-neutral representation
[ ] light/dark values
[ ] accent interaction
[ ] contrast
[ ] consumers
[ ] Storybook/render proof
[ ] migration if replacing old token
```

---

# 162. New primitive checklist

```text
[ ] generic UI responsibility
[ ] web/mobile owner
[ ] tokens used
[ ] public API
[ ] keyboard/focus
[ ] labels/ARIA
[ ] disabled/error/loading states
[ ] reduced motion
[ ] responsive/touch
[ ] Storybook
[ ] a11y test
[ ] visual test
```

---

# 163. Theme change checklist

```text
[ ] semantic token owner
[ ] light
[ ] dark
[ ] system mode
[ ] accent themes
[ ] persistence
[ ] pre-paint/flash
[ ] focus/contrast
[ ] marketing/product parity
[ ] Storybook/a11y/visual
```

---

# 164. Density change checklist

```text
[ ] semantic density modes
[ ] row/control height
[ ] spacing
[ ] typography
[ ] touch target
[ ] keyboard/focus
[ ] Board/Table representative components
[ ] visual/a11y evidence
```

---

# 165. Stop conditions

Stop implementation if:

- a feature introduces a new “primary brand color” outside tokens;
- dark mode is patched with unrelated per-component hex values;
- accent theme branches are duplicated across components;
- mobile imports `ui-web`;
- generic `ui-web` starts importing product state;
- product-specific Board/Document component is moved into generic UI solely for reuse;
- an interactive div replaces semantic control without full keyboard semantics;
- focus outline is removed with no replacement;
- compact density reduces accessible hit target without compensating interaction design;
- theme storage logic is added to `ui-tokens`;
- generated/vendored component concerns cause global lint/format weakening;
- snapshot baseline is updated without reviewing visual intent;
- accessibility gate is disabled because the component fails;
- marketing creates a separate permanent brand palette instead of design-system semantics;
- gradient/motion is applied so broadly that hierarchy/theme readability degrades;
- a declared package export points to a missing token asset/file.

---

# 166. Executable evidence

Primary current evidence:

```text
frontend/packages/ui/tokens/
frontend/packages/ui/web/
frontend/packages/ui/mobile/
frontend/packages/ui/icons/
frontend/tooling/storybook/web/
frontend/e2e/ui/
frontend/playwright.storybook.config.ts
frontend/package.json
```

Current source visibly demonstrates:

```text
framework-neutral tokens
light/dark semantic variables
motion tokens
web ThemeProvider
separate color-theme API
web/mobile UI split
Storybook a11y tooling
Playwright visual tests
```

---

# 167. Related architecture

Read:

```text
frontend-overview.md
dependency-boundaries.md
hosts-composition-routing.md
state-query-mutations.md
testing-and-quality-gates.md
architecture-change-policy.md
```

Repository quality:

```text
docs/quality/accessibility-standard.md
docs/quality/performance-and-scalability.md
docs/quality/engineering-quality-standard.md
```

---

# 168. Explicit non-responsibilities

This document does not define:

```text
exact marketing landing-page layout
exact product screen copy
product status lifecycle
backend permission semantics
complete current component inventory
exact screenshot threshold values
```

Those belong to product/design/source/test owners.

---

# 169. Final design-system model

The target architecture is:

```text
DESIGN SEMANTICS
ui-tokens
        ↓
┌───────────────────┐
│                   │
ui-web           ui-mobile
│                   │
web adapters      mobile adapters
│                   │
feature/product UI
```

Theme model:

```text
appearance:
light | dark | system

plus

accent/color theme:
default | approved variants
```

Component model:

```text
generic primitive
→ design system

product component
→ product/feature owner

host shell
→ app composition

marketing section
→ marketing owner using approved shared visual semantics
```

Verification model:

```text
tokens
→ Storybook
→ accessibility
→ visual regression
→ product integration
```

The design system succeeds when light/dark/accent themes remain coherent, web/mobile share semantics without sharing incompatible rendering, accessible behavior is built into primitives, and marketing can be expressive without fragmenting the Notrelix brand language.
