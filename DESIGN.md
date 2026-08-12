# DESIGN.md — Notrelix Product Design Constitution

> **calm · focused · confident**
>
> Notrelix is a work product before it is a visual showcase.
>
> The interface should help users understand, organize, manipulate, discuss, automate, and trust their work for sustained periods without unnecessary visual or interaction noise.

This document is the **repository-level design constitution** for Notrelix.

It defines:

- product design character;
- visual and interaction principles;
- product versus marketing presentation registers;
- information hierarchy;
- density;
- interaction grammar;
- application states;
- accessibility;
- motion;
- responsive/multi-host expectations;
- component ownership principles;
- design review criteria.

It does **not** own literal implementation tokens, CSS variables, exact component source, web/native primitive implementations, or Storybook configuration.

Those implementation details belong to the frontend design-system owners.

---

# 1. Authority

## 1.1 What this file owns

`DESIGN.md` owns repository-wide answers to:

- What should Notrelix feel like?
- How should product interfaces prioritize information?
- How dense should authenticated work surfaces be?
- How should marketing differ from the authenticated application?
- What visual/interaction behaviors are unacceptable even if technically functional?
- What application states must be designed?
- What accessibility standard applies?
- What design principles apply across web and mobile?
- When should UI primitives be shared versus platform-specific?
- How should Work Management, Documents, Collaboration, Automation, Governance, Billing, and Analytics feel like one product without becoming visually identical?

---

## 1.2 What this file does not own

This file MUST NOT become the canonical source for:

- literal color values;
- literal spacing scales;
- exact border-radius values;
- exact font files;
- Tailwind configuration;
- CSS custom-property declarations;
- web component implementation;
- React Native component implementation;
- package imports;
- shadcn component source;
- product-specific workflow logic.

Frontend implementation ownership belongs to:

```text
frontend/docs/architecture/ui-and-design-system.md

frontend/packages/ui/tokens
frontend/packages/ui/web
frontend/packages/ui/mobile
```

and the actual package/source manifests.

When literal source and this semantic constitution disagree:

- source may be stale implementation;
- this document may be stale semantic intent;
- the discrepancy MUST be classified rather than silently normalized.

---

## 1.3 Relationship to product semantics

[`PRODUCT.md`](PRODUCT.md) defines product meaning.

Design expresses product meaning.

Design MUST NOT redefine it.

Examples:

```text
PRODUCT:
BoardView is presentation/query configuration over shared Board data.

DESIGN:
Table, Kanban, Calendar, Timeline and Dashboard may use different interaction grammars.

DESIGN MUST NOT:
make each View appear to own independent business records.
```

Another example:

```text
PRODUCT:
BoardGroup is structural organization and is not universal Kanban status.

DESIGN:
Kanban columns must visually and behaviorally correspond to the configured grouping field.

DESIGN MUST NOT:
teach users that every BoardGroup is a workflow status.
```

---

# 2. Design identity

The target product identity is:

> **calm · focused · confident**

These words are behavioral constraints, not branding decoration.

---

## 2.1 Calm

Calm means:

- low unnecessary visual noise;
- restrained chrome;
- predictable interactions;
- stable placement;
- meaningful color;
- deliberate motion;
- clear hierarchy;
- recoverable state transitions.

Calm does **not** mean:

- empty;
- low-information;
- gray everywhere;
- oversized whitespace;
- hidden controls;
- weak hierarchy;
- no feedback.

A dense Board can be calm.

A sparse marketing card grid can be noisy.

---

## 2.2 Focused

Focused means the current work remains visually dominant.

Examples:

- Board data is more important than toolbar decoration.
- Document content is more important than editor chrome.
- A permission error is more important than decorative background treatment.
- Current selection and active context are clear.
- Primary actions are discoverable without every action competing for primary emphasis.

Focused interfaces answer:

> What am I looking at?

> What can I do here?

> What changed?

> What needs my attention?

without requiring the user to scan every piece of chrome.

---

## 2.3 Confident

Confident means:

- actions have clear outcomes;
- hierarchy is deliberate;
- destructive behavior is explicit;
- errors are precise;
- state is truthful;
- the product does not look tentative or inconsistent;
- product nouns are used consistently;
- visual emphasis corresponds to semantic importance.

Confidence is lost when:

- optimistic state remains after failure;
- loading flashes protected data;
- buttons change placement unpredictably;
- identical colors mean unrelated things;
- error text says only “Something went wrong” when recovery differs;
- the UI visually implies a state transition that never became authoritative.

---

# 3. One identity, multiple presentation registers

Notrelix has one product identity but not one universal page composition.

The two primary registers are:

```text
Authenticated Product Register
Public / Marketing Register
```

They share:

- brand identity;
- typography principles;
- accessibility baseline;
- semantic color philosophy;
- interaction quality;
- language integrity.

They differ in:

- density;
- storytelling;
- motion volume;
- decorative expression;
- conversion hierarchy.

---

# 4. Authenticated Product Register

The authenticated product is optimized for sustained work.

Primary goals:

- scanning;
- editing;
- navigation;
- comparison;
- collaboration;
- state awareness;
- automation/configuration;
- error recovery.

The authenticated product SHOULD generally be:

- quieter;
- denser;
- more structurally predictable;
- less decorative;
- more content-led;
- more keyboard-efficient;
- more state-explicit.

Decorative visual elements MUST justify their cost in:

- attention;
- space;
- performance;
- accessibility;
- interaction complexity.

---

## 4.1 The work is the visual priority

Authenticated app chrome SHOULD recede behind:

- Boards;
- Items;
- fields;
- documents;
- schedules;
- comments;
- metrics;
- workflows;
- user-authored content.

Navigation and tools remain discoverable.

They should not continuously compete with the content.

---

## 4.2 Avoid “dashboardification”

Do not turn every product screen into:

```text
large hero title
+
four metric cards
+
generic card grid
+
decorative chart
```

unless the semantics of the screen genuinely require that composition.

Work-management software contains many operational surfaces where:

- tables;
- lists;
- documents;
- timelines;
- forms;
- focused editors

are more appropriate than dashboard cards.

---

# 5. Marketing Register

Marketing may use:

- stronger visual storytelling;
- larger typography;
- richer composition;
- stronger brand accents;
- product-led illustrations;
- controlled gradients;
- richer transition/motion;
- conversion hierarchy.

But marketing MUST NOT:

- invent capabilities the product does not support;
- imply security/enterprise guarantees that are not contracted;
- misrepresent product screenshots;
- become visually disconnected from Notrelix;
- sacrifice accessibility for spectacle;
- reuse dense authenticated-app layout without adaptation.

Marketing is louder.

It is not a different brand.

---

# 6. Information hierarchy

Hierarchy should be produced through the smallest effective combination of:

1. semantic grouping;
2. typography;
3. spacing;
4. alignment;
5. position;
6. size;
7. contrast;
8. color;
9. elevation;
10. motion.

Avoid using all ten simultaneously for ordinary content.

---

## 6.1 Hierarchy levels

A typical authenticated surface may include:

```text
Application context
→ Workspace / Account

Capability context
→ Board / Document / Automation / Billing

View/context mode
→ Table / Kanban / Settings / Activity

Current work
→ Items / fields / blocks / rows / actions

Supporting metadata
→ owner / dates / counts / status / permissions
```

The hierarchy should be understandable without decorative containers around every level.

---

## 6.2 Primary versus secondary action

A screen SHOULD normally have a clear primary action or no primary action.

Do not make every toolbar action visually primary.

Use emphasis according to semantic importance:

```text
Primary
    main forward action

Secondary
    common but not dominant

Tertiary / ghost
    contextual supporting action

Destructive
    explicit irreversible/high-risk action

Inline
    action local to data/component context
```

“Primary” is not a button style to apply whenever a developer wants more visibility.

---

# 7. Calm density

Notrelix is an information-rich enterprise product.

The design target is:

> **high useful information density with low cognitive noise.**

Density should be managed by:

- alignment;
- consistent row rhythm;
- predictable columns;
- grouping;
- progressive disclosure;
- compact controls;
- stable typography;
- contextual toolbars;
- keyboard operation;
- detail-on-demand.

Do not solve complexity by simply increasing padding.

---

## 7.1 Density modes may exist when semantics justify them

For data-heavy views, the product MAY support density preferences such as:

```text
compact
comfortable
```

if implemented consistently.

Density changes SHOULD affect:

- vertical rhythm;
- row/card padding;
- metadata visibility where explicitly designed.

They MUST NOT:

- change product meaning;
- hide critical state;
- make hit targets inaccessible;
- produce different authorization behavior.

---

# 8. Typography semantics

Typography establishes hierarchy and reading rhythm.

The exact font family, sizes, weights, tracking, and tokens are source-owned by the design-token system.

This constitution defines semantic roles.

---

## 8.1 Semantic text roles

The system SHOULD distinguish roles such as:

```text
display
page title
section heading
subheading
body
dense body
label
metadata
caption
code / identifier
editorial / long-form emphasis where approved
```

Do not choose typography values ad hoc per component.

---

## 8.2 Work surfaces

Dense work surfaces prioritize:

- scanability;
- aligned baselines;
- legibility at compact sizes;
- stable row height;
- strong selection/focus indication.

Very large headings generally do not belong inside operational data surfaces.

---

## 8.3 Document surfaces

Document/read-heavy surfaces may use:

- wider line height;
- narrower readable measure;
- lower chrome density;
- stronger content rhythm.

Document typography SHOULD optimize sustained reading/editing rather than mimic board/table density.

---

## 8.4 Marketing typography

Marketing may use larger display hierarchy and more expressive composition.

Body readability remains a requirement.

---

# 9. Color semantics

Color communicates state, identity, category, selection, and emphasis.

Color MUST NOT become uncontrolled decoration.

Literal palette values are owned by tokens.

---

## 9.1 Semantic categories

Design tokens SHOULD distinguish at least conceptual roles for:

```text
surface
text
muted text
border/divider
interactive
focus
selection
success
warning
danger
information
disabled
brand accent
```

Product-specific categorical palettes MAY exist where categorization is part of the workflow.

---

## 9.2 Color must have stable meaning

A semantic state color SHOULD mean the same type of thing across capabilities where semantics match.

Examples:

- destructive action treatment;
- validation/error treatment;
- focus;
- disabled;
- selected.

Do not overload “green” to simultaneously mean:

- completed;
- online;
- selected;
- paid;
- safe;
- primary action

without contextual semantics.

---

## 9.3 Status colors

Status values are product data.

Their colors MAY be configurable or type-specific.

Color does not replace:

- text;
- icon;
- semantic label;
- accessible name.

Users who cannot perceive a color distinction must still understand the state.

---

# 10. Shape, surfaces, and elevation

Literal radius/shadow scales belong to implementation tokens.

Semantic principles:

- radius communicates family/coherence, not product meaning;
- elevation indicates layering or temporary foreground;
- borders separate information only where needed;
- nested borders/panels should be minimized;
- shadows should not be decorative noise.

---

## 10.1 Surface hierarchy

Conceptually distinguish:

```text
background / canvas
subtle secondary surface
raised content surface
overlay
transient feedback surface
```

The implementation does not need a literal universal numeric elevation model if platform conventions differ.

---

## 10.2 Avoid panel nesting

This is discouraged:

```text
page
└─ card
   └─ card
      └─ bordered panel
         └─ boxed field
```

unless each layer has real semantic/interaction purpose.

Prefer:

- spacing;
- alignment;
- headings;
- separators;
- background contrast

before adding another card.

---

# 11. Interaction grammar

The same semantic action should behave predictably across the product.

Examples:

```text
select
open
edit
save
cancel
delete
archive
move
drag
filter
sort
group
search
invite
share
retry
resolve conflict
```

Components may differ by host.

The interaction meaning remains coherent.

---

# 12. Selection

Selection state must be explicit.

For selectable rows/cards/items:

- pointer selection;
- keyboard selection where appropriate;
- focus;
- hover;
- active;
- selected;
- disabled

must not be visually indistinguishable.

Focus and selection are different concepts.

A keyboard-focused row is not necessarily selected.

---

# 13. Hover, focus, pressed, and active states

Interactive controls SHOULD communicate:

```text
default
hover     # pointer-capable surfaces
focus
pressed
disabled
loading
selected/active when relevant
```

Do not rely on hover for critical information or actions because:

- mobile/touch has no stable hover;
- keyboard users may never trigger hover;
- assistive technology interaction differs.

---

# 14. Direct manipulation

Notrelix supports direct manipulation where it maps clearly to product semantics.

Examples:

- drag Item in Kanban;
- reorder Item/Block where allowed;
- resize/reorder configurable views/widgets where contracted.

Direct manipulation MUST define:

- actual business mutation;
- allowed target;
- invalid target;
- optimistic behavior;
- keyboard/touch alternative where required;
- rollback;
- conflict;
- authoritative reconciliation.

Animation is not the mutation.

A card visually moving is not success until the product state accepts the change.

---

# 15. Drag and drop

Drag-and-drop is appropriate only when:

- the spatial metaphor is meaningful;
- target is understandable;
- the action can be represented accessibly;
- accidental activation risk is manageable.

Design MUST show:

- draggable state or affordance where needed;
- current drag item;
- valid drop targets;
- invalid targets;
- insertion position/order;
- successful/failed outcome.

For Kanban:

```text
visual column
→ configured grouping-field value
```

Dragging across columns represents a grouping-field mutation plus ordering semantics.

It MUST NOT teach users that BoardGroup and Kanban status are universally the same concept.

---

# 16. Forms

Forms are structured product interactions, not collections of independent inputs.

---

## 16.1 Labels

Every input must have a programmatically associated label or equivalent accessible name.

Placeholder text is not a label.

---

## 16.2 Validation

Validation should appear:

- near the affected field when specific;
- at form/global level when cross-field/system-level;
- after meaningful interaction/submission according to context.

Errors should say:

```text
what is wrong
what is expected
what user can do
```

when safe.

---

## 16.3 Disabled versus unavailable

Do not disable controls without explanation when users reasonably expect the action.

Prefer:

- permission explanation;
- plan/entitlement explanation;
- prerequisite explanation;
- validation guidance

where appropriate.

---

# 17. Tables and data grids

Table is a core Notrelix work surface.

It should optimize:

- scanning;
- editing;
- navigation;
- comparison;
- bulk work;
- schema visibility.

---

## 17.1 Table hierarchy

Table UI should clearly distinguish:

```text
header / field definition
row / BoardItem
cell / field value
group / BoardGroup structural organization
selection
editing state
validation state
```

Do not style all regions identically.

---

## 17.2 Dense editing

Editing should minimize unnecessary modal transitions.

Use inline editing where:

- semantics are local;
- validation can be communicated safely;
- focus/keyboard flow remains predictable.

Use richer popover/dialog editors when:

- field configuration is complex;
- selection requires search/browse;
- destructive implications require explanation.

---

## 17.3 Sticky regions

Sticky headers/columns MAY improve large-data work.

They must not:

- obscure content;
- trap focus;
- create broken scroll layering;
- hide validation/error messages.

---

## 17.4 Virtualization

For large data sets, virtualization/windowing may be required.

Virtualization MUST preserve:

- keyboard navigation;
- selection;
- focus recovery;
- accessibility semantics as practical for the chosen implementation;
- scroll stability.

Performance cannot justify unusable keyboard/screen-reader behavior without an explicit exception.

---

# 18. Kanban

Kanban is a visual grouping of authoritative BoardItems.

Design priorities:

- column/grouping-field clarity;
- item identity;
- status/category scanning;
- direct manipulation;
- ordering;
- clear empty/drop states.

---

## 18.1 Cards

Cards should show only information useful for the current view.

Do not expose every BoardField by default.

Users should be able to understand:

- item identity/title;
- relevant grouping/status;
- key metadata configured for the view;
- interaction state.

---

## 18.2 Columns

Column header communicates the grouping value.

It may include:

- label;
- item count;
- configured metadata;
- actions.

Column visual design MUST NOT imply semantic ownership that does not exist.

---

# 19. Calendar and timeline

Temporal views should make time semantics explicit.

The user should be able to identify:

- which field defines date/range;
- timezone context where relevant;
- invalid/missing temporal state;
- overlapping items;
- current time/date context.

Dragging/resizing a temporal item must map to explicit product field changes.

---

# 20. Dashboard and analytics

Dashboard design should emphasize meaning over chart decoration.

Every metric/widget should make understandable where appropriate:

- title;
- value;
- unit;
- time window;
- filters/scope;
- comparison;
- freshness;
- empty/no-data state.

Charts MUST NOT use visual complexity that obscures the metric.

Color legends must remain accessible.

Dashboards are derived views.

The design must not imply the chart itself owns editable business truth.

---

# 21. Documents

Document surfaces optimize writing and reading.

They may be visually quieter and less dense than Boards.

---

## 21.1 Writing focus

Document chrome SHOULD recede during active reading/writing.

Contextual formatting/tooling may appear:

- on selection;
- on focus;
- through keyboard command;
- through explicit toolbar.

Avoid permanently surrounding every block with controls/borders.

---

## 21.2 Blocks

Block interaction should make clear:

- selection;
- insertion;
- drag/reorder;
- nesting/hierarchy;
- block-specific actions.

Block affordances may be subtle by default but must remain discoverable and accessible.

---

## 21.3 Linked resources

Embeds/links to Boards/Items must communicate:

- referenced resource identity;
- unavailable/permission state;
- loading;
- deletion/archive;
- navigation action.

Do not render inaccessible resource content briefly before access is resolved.

---

# 22. Collaboration surfaces

Comments, mentions, notifications, activity, presence, and reactions have different temporal durability.

Design must reflect that difference.

---

## 22.1 Comments

Comments should prioritize:

- author;
- content;
- time;
- thread/reply context;
- mentions;
- resolution state where applicable.

Avoid decorative card treatment around every comment when simple conversational structure is clearer.

---

## 22.2 Notifications

Notifications should answer:

```text
What happened?
What resource?
Who/what caused it?
When?
Do I need to act?
```

Read/unread state must not rely on color alone.

---

## 22.3 Activity versus audit

User-facing activity can be approachable.

Security/compliance audit UI may require:

- stronger precision;
- immutable-event semantics;
- explicit actor/resource/action;
- filtering;
- timestamp/timezone clarity.

Do not visually or linguistically collapse audit and user activity into one concept if their product semantics differ.

---

# 23. Automation interfaces

Automation builders should reveal the model:

```text
Trigger
→ Conditions
→ Actions
```

Users should understand:

- what starts the automation;
- which scope/resource it applies to;
- what conditions are evaluated;
- what actions may occur;
- whether it is enabled;
- recent execution state where provided.

Avoid generic “magic automation” UI that hides important scope or side-effect behavior.

---

## 23.1 Riskful actions

Provider actions, destructive actions, bulk mutation, billing-impacting behavior, or recursively-triggering actions should communicate risk before activation where relevant.

---

# 24. Governance and permission UI

Permission UI is an explanation/projection of backend policy.

It is not the security boundary.

Design should make understandable:

- current access;
- inherited versus explicit access;
- guest/share-link access;
- read versus edit/admin difference;
- unavailable actions;
- dangerous sharing changes.

Do not expose internals such as policy engine class names.

Use product language.

---

# 25. Billing and entitlement UI

Commercial state should be clear without hijacking product workflow.

Examples:

- plan limits;
- upgrade availability;
- blocked creation;
- read-only/excess resource state;
- payment recovery.

Do not:

- hide user data because billing changed without product policy;
- use dark-pattern urgency;
- represent provider status directly when product billing semantics differ.

---

# 26. Search and command surfaces

Search/command experiences should optimize:

- fast keyboard access;
- clear scope;
- result category;
- highlighted matching context where useful;
- permission-safe results;
- predictable navigation.

Do not mix global/account/workspace/resource search without showing or safely handling scope.

A command palette may expose actions but MUST respect actual availability/authorization.

---

# 27. Navigation

Navigation should express the product hierarchy rather than implementation routes.

Typical levels may include:

```text
Account
Workspace
Capability
Resource
View
Subsurface/settings
```

Navigation labels use product nouns.

Do not expose code/package/route terminology.

---

## 27.1 Persistent navigation

Persistent shell navigation should contain high-frequency context.

Do not fill it with every feature merely because a route exists.

---

## 27.2 Contextual navigation

Resource-level navigation may expose:

- Board views;
- Document outline;
- Automation sections;
- settings.

Contextual navigation should not compete with global workspace navigation.

---

# 28. Menus and command density

Menus are useful for low-frequency/contextual actions.

Do not hide every action in a `...` menu merely to make the UI look clean.

High-frequency or critical actions should remain appropriately discoverable.

Conversely, do not permanently expose every rare action.

Use frequency, risk, and context to choose presentation.

---

# 29. Dialogs, sheets, and overlays

Overlays interrupt context.

Use them intentionally.

---

## 29.1 Dialog

Appropriate for:

- focused decision;
- destructive confirmation;
- small bounded form;
- critical explanation.

Avoid long workflow journeys inside stacked dialogs.

---

## 29.2 Side sheet / inspector

Appropriate when:

- preserving underlying work context is valuable;
- viewing/editing detail adjacent to a table/board;
- supplementary configuration fits a secondary pane.

Do not recursively open sheets over sheets.

---

## 29.3 Popover

Appropriate for:

- compact contextual choice;
- date/status/person selection;
- small configuration.

Must manage:

- focus;
- dismissal;
- viewport collision;
- keyboard navigation.

---

# 30. Feedback

Feedback should correspond to actual state.

---

## 30.1 Inline feedback

Use for:

- validation;
- local save state;
- field-specific conflict.

---

## 30.2 Toast / transient feedback

Use for:

- completed low-risk confirmation;
- background result;
- reversible action notification where appropriate.

Do not use toast as the only communication for:

- destructive failure;
- blocking validation;
- authorization denial requiring user action;
- critical conflict.

---

## 30.3 Persistent banners

Use when state persists and affects the current experience:

- disconnected/reconnecting;
- read-only;
- workspace suspended;
- entitlement limitation;
- incident/degraded mode.

Do not show persistent banners for transient success.

---

# 31. Application state grammar

Every significant surface must consider the states relevant to its contract.

At minimum evaluate:

```text
initial loading
incremental loading
empty
success
validation error
permission denied
read-only
not found / unavailable
conflict
offline / disconnected
reconnecting
partial/stale
provider failure
destructive confirmation
disabled / prerequisite missing
```

Do not automatically render all states.

Design the ones the capability can actually enter.

---

# 32. Loading

Loading communicates progress without lying about state.

---

## 32.1 Initial load

For initial protected data:

- avoid flashing stale/inaccessible data before scope/auth resolution;
- use stable skeleton/progress appropriate to layout;
- avoid excessive layout shift.

---

## 32.2 Incremental loading

When existing authoritative content remains valid:

- preserve context;
- show local/incremental progress;
- avoid replacing entire page with a spinner.

---

## 32.3 Long-running action

If duration is materially long/async:

- show acknowledged state;
- allow user to understand work is in progress;
- avoid fake percentage unless actual progress exists.

---

# 33. Empty states

Empty is not one state.

Distinguish:

```text
nothing created yet
no results for current filter/search
no permission to view content
data unavailable
content deleted/archived
```

Do not show “Create” CTA to users who cannot create.

Empty copy should explain the next valid action.

---

# 34. Error states

Error design must match recovery semantics.

---

## 34.1 Validation

Explain what to fix.

---

## 34.2 Authorization

Explain access limitation without leaking protected facts.

---

## 34.3 Concurrency

Explain that data changed and may require refresh/review/reapply.

Do not present concurrency conflict as generic network failure.

---

## 34.4 Connectivity

Preserve user input where safe.

Offer retry/recovery according to actual operation semantics.

---

## 34.5 Unknown side-effect result

For provider/time-out cases where the outcome may be unknown, do not simply show “Failed — retry” if retry could duplicate the effect.

The product UX should reflect reconciliation/idempotency design.

---

# 35. Permission and read-only states

A disabled control alone is often insufficient.

Where appropriate, communicate:

- why action is unavailable;
- required permission;
- owner/admin contact path;
- billing/entitlement requirement;
- resource state preventing change.

Do not reveal confidential policy/resource details merely to explain denial.

---

# 36. Conflict states

Concurrency/version conflict is a first-class product state.

Possible UI patterns depend on resource semantics:

- refetch and notify;
- compare changes;
- preserve unsaved input;
- allow manual reapply;
- retry with fresh version after review.

Do not silently overwrite a user's newer/older data without approved product behavior.

---

# 37. Offline, reconnecting, and stale state

Notrelix should not pretend online authority when connectivity is unavailable.

If the current host supports offline/stale behavior:

- communicate connectivity;
- distinguish cached data from confirmed fresh state where meaningful;
- queue actions only when retry/idempotency semantics support it;
- reconcile after reconnect.

Realtime disconnect must not imply all displayed data is invalid.

It means convergence/freshness guarantees changed.

---

# 38. Optimistic UI

Optimistic interaction is a product-design and state-consistency decision.

Use it when:

- likely to succeed;
- rollback is safe;
- effect is local/understandable;
- authoritative reconciliation is defined.

Avoid or use caution when:

- destructive;
- externally side-effecting;
- permission-sensitive;
- high conflict;
- multi-resource transactional;
- billing/security critical.

Optimistic design must provide a failure/reconciliation path.

---

# 39. Motion

Motion explains:

- spatial continuity;
- hierarchy;
- cause/effect;
- state transition;
- direct manipulation.

Motion should not exist merely because the interface can animate.

---

## 39.1 Product motion character

Authenticated product motion is:

- restrained;
- quick;
- functional;
- interruptible where appropriate.

Marketing may be more expressive.

---

## 39.2 Avoid toy-like motion

Avoid:

- bounce on routine actions;
- constant gradient movement;
- celebratory animation for trivial changes;
- delayed interaction because an animation must finish.

---

## 39.3 Reduced motion

Respect platform/browser reduced-motion preferences.

Critical state change must remain understandable with motion reduced or removed.

---

# 40. Responsive design

Responsive design changes composition without changing product semantics.

---

## 40.1 Web desktop

May use:

- dense tables;
- multi-pane layouts;
- keyboard shortcuts;
- hover enhancement;
- drag-and-drop;
- broad context visibility.

---

## 40.2 Narrow web/tablet

May:

- collapse secondary panes;
- adapt navigation;
- reduce simultaneous columns;
- move contextual tools.

Do not merely shrink desktop controls until unusable.

---

## 40.3 Mobile/native

Mobile requires native interaction priorities:

- touch;
- safe areas;
- platform navigation;
- native accessibility;
- limited viewport;
- keyboard behavior;
- lifecycle/backgrounding.

Do not assume feature parity means identical layout.

Semantic parity may use different interaction design.

---

# 41. Web and mobile parity

Shared product capability should preserve:

- meaning;
- authorization;
- state;
- lifecycle;
- contract.

It MAY differ in:

- navigation;
- input;
- density;
- gesture;
- layout;
- primitive implementation.

A web table may become a mobile list/detail flow.

That is not semantic divergence if the same authoritative state and actions are represented correctly.

---

# 42. Accessibility baseline

New/changed product work targets **WCAG 2.2 AA**.

Accessibility is a product-quality requirement under `NRX-015`.

It must be considered during design and implementation, not after feature completion.

---

# 43. Keyboard accessibility

Web interactions that can be performed with pointer should have appropriate keyboard behavior when the interaction is relevant to keyboard users.

Key requirements include:

- logical focus order;
- visible focus;
- reachable controls;
- no keyboard trap;
- Escape/dismiss behavior where appropriate;
- menu/listbox/dialog keyboard semantics;
- direct-manipulation alternatives.

Do not invent custom keyboard behavior when established platform patterns exist.

---

# 44. Focus management

Focus should move according to user intent.

Examples:

### Dialog opens

Focus enters the dialog.

### Dialog closes

Focus generally returns to the initiating context when still available.

### Item deleted

Focus moves to a predictable neighboring/parent context.

### Route/surface transition

Focus management must avoid leaving keyboard/screen-reader users in stale hidden content.

---

# 45. Screen readers and semantic structure

Use native/semantic controls where possible.

Custom widgets must implement appropriate roles/states/labels.

Do not simulate:

- button with generic `div`;
- input with contenteditable without semantics;
- checkbox with color-only icon.

Dynamic feedback should use appropriate announcements where necessary without overwhelming the user.

---

# 46. Color and contrast accessibility

Text and meaningful graphical state must meet the adopted accessibility standard.

State MUST NOT be communicated by color alone.

Examples:

```text
error
→ color + message/icon/semantics

selected
→ color + structure/indicator/state

status
→ text label + color
```

---

# 47. Touch accessibility

Mobile/touch targets must be comfortably operable.

Avoid tiny icon-only targets packed densely without sufficient hit area.

Gesture-only behavior needs an alternative when the action is important.

---

# 48. Zoom and text scaling

Critical product functionality should remain usable under browser zoom and platform text scaling within the supported accessibility expectations.

Avoid fixed layout assumptions that:

- clip labels;
- hide actions;
- overlap content;
- make dialogs unusable.

---

# 49. Content and product language

UI copy is part of product design.

Use product nouns consistently.

Prefer:

- direct verbs;
- precise outcomes;
- understandable state;
- actionable errors.

Avoid:

- framework jargon;
- backend class names;
- database terms;
- vague “Something went wrong” where recovery differs;
- anthropomorphic system language that obscures responsibility.

---

## 49.1 Destructive language

Use the actual lifecycle verb:

```text
Archive Board
Disable Automation
Revoke Access
Disconnect Integration
Cancel Subscription
Delete Item
```

Do not call every action “Delete”.

---

# 50. Time, date, and numeric presentation

When displaying temporal/numeric business data, consider:

- user locale;
- timezone;
- relative versus absolute time;
- precision;
- units;
- currency;
- freshness.

Do not show a relative time such as “today” where ambiguity materially affects business interpretation without enough context.

---

# 51. Design ownership

Design implementation ownership follows frontend architecture.

Conceptually:

```text
tokens
    shared visual semantics

ui-web
    reusable web primitives

ui-mobile
    reusable native primitives

product packages
    capability-specific components/interactions

feature packages
    cross-product workflow composition

apps
    host composition
```

Generic primitives MUST NOT import product workflows.

Product components SHOULD NOT be pushed into generic UI merely because multiple screens use them.

---

# 52. Token ownership

The token package is the shared implementation authority for design tokens.

Root DESIGN owns semantic intent.

Therefore this file SHOULD say:

```text
primary interactive emphasis should be consistent
```

rather than:

```text
--brand-primary = <literal implementation value>
```

unless the literal brand value itself becomes a formal brand contract intentionally owned at repository level.

Default rule:

> literal implementation token values remain source-owned.

---

# 53. Web versus mobile UI primitives

Web and mobile implementations remain separate where runtime/platform behavior differs.

Shared tokens do not require shared rendered primitives.

Do not solve visual consistency by forcing DOM components through React Native or vice versa unless an explicit architecture decision supports it.

---

# 54. Product components versus primitives

A reusable primitive answers generic interaction needs:

```text
Button
Dialog
Menu
Input
Tabs
Popover
Tooltip
Table primitive
```

A product component carries business semantics:

```text
BoardItemRow
FieldValueEditor
AutomationTriggerEditor
PermissionEditor
SubscriptionEntitlementNotice
```

Product components belong with their product/feature owner.

Do not move them into generic UI to avoid imports.

---

# 55. Vendor and generated component policy

Third-party/generated component code—including shadcn-derived code—is not exempt from Notrelix quality.

Once incorporated, it must satisfy applicable:

- formatting;
- lint;
- type safety;
- accessibility;
- token ownership;
- architecture boundaries;
- platform compatibility;
- interaction grammar.

“Generated by vendor” is not an acceptable reason to preserve inconsistent behavior.

However, avoid unnecessary stylistic rewriting of vendor code if it creates maintenance cost without improving product quality.

---

# 56. Component state contract

Reusable interactive components SHOULD explicitly support the states relevant to their semantics.

Common examples:

```text
default
hover
focus
pressed
disabled
loading
selected
invalid
read-only
```

Product components additionally handle:

- permission;
- server error;
- conflict;
- stale/reconnecting

where those states belong to the product capability rather than primitive.

---

# 57. Data visualization

Charts and analytics should communicate data, not decoration.

Required principles:

- readable labels;
- accessible legend;
- meaningful scales;
- appropriate zero/baseline;
- no 3D decoration that distorts values;
- color is not sole encoding;
- empty/no-data is distinct from zero.

Metric semantics belong to Analytics/Product.

Design must not imply unsupported precision.

---

# 58. Icons

Icons support recognition and compactness.

Do not rely on unfamiliar icon-only actions when meaning is not clear.

Use text labels/tooltips/accessibility names according to context.

Icon style should be coherent through the shared icon/design system rather than mixing arbitrary libraries per feature.

---

# 59. Avatars and identity representation

Avatar/color/image does not replace user identity.

When ambiguity matters, show an accessible name/label.

Do not use color/avatar alone to distinguish multiple assignees in a way that excludes screen-reader or color-vision users.

---

# 60. Status, badges, and tags

Badges should represent compact semantic state/category.

Avoid turning every metadata value into a colorful pill.

Pills should be used when the shape communicates:

- token;
- discrete state;
- tag;
- compact selection.

Ordinary text metadata may remain text.

Too many badges create visual noise and destroy hierarchy.

---

# 61. Destructive actions

Destructive action design should reflect:

- reversibility;
- scope;
- data loss;
- cross-context consequences;
- recovery.

Do not show confirmation dialogs for every harmless action.

Do use stronger friction where irreversible/high-impact action warrants it.

Confirmation text should name the actual resource and consequence when possible.

---

# 62. Undo

Undo can be preferable to confirmation for frequent, safely reversible actions.

Only offer undo when the product/backend can genuinely restore the state.

Do not display “Undo” as a UI illusion if the durable operation cannot be reversed reliably.

---

# 63. Progressive disclosure

Advanced capability should be discoverable without overwhelming default workflows.

Techniques include:

- contextual controls;
- detail panels;
- advanced sections;
- command palette;
- keyboard shortcuts;
- conditional configuration.

Do not permanently show every enterprise feature on every surface.

---

# 64. Settings

Settings should be organized by semantic owner, not implementation module.

Users think in concepts such as:

```text
Workspace
Members
Permissions
Integrations
Billing
Notifications
Automation
```

not:

```text
Infrastructure
API
Feature package
```

Dangerous settings should clearly state consequences.

---

# 65. Onboarding

Onboarding should help users achieve meaningful product state.

Avoid forcing long tours that explain every control.

Prefer:

- contextual guidance;
- templates where semantically valid;
- progressive setup;
- clear first meaningful action.

Onboarding must not create fake/demo state that later conflicts with real authoritative state without explicit design.

---

# 66. Empty product bootstrap

When a new Workspace/Board/Document has no data, the design should guide toward a valid next step while respecting permission and entitlement.

Examples:

```text
empty Board
→ create Item / configure schema / use approved template

empty Workspace
→ create/open capability / invite members if allowed

empty Automation list
→ create rule if authorized
```

Do not show actions the principal cannot perform.

---

# 67. Performance perception

Perceived performance is part of design.

Use:

- immediate local feedback;
- stable layout;
- incremental loading;
- optimistic behavior when safe;
- skeletons when shape is known.

Do not use animation to hide fundamentally slow/unbounded operations.

Performance architecture remains an engineering concern, but design must not encourage unbounded UI patterns.

---

# 68. Large data sets

Work surfaces must be designed with scale in mind.

Avoid UI assumptions such as:

- all Items always rendered;
- all members loaded in one menu;
- all search results available at once;
- all activity history expanded.

Design pagination, search, virtualization, incremental loading, and filtering with the backend/query architecture.

---

# 69. Responsiveness under mutation

A mutation should communicate:

```text
interaction accepted
→ pending if materially relevant
→ success / authoritative convergence
or
→ failure / rollback / recovery
```

Avoid locking the entire screen for a local mutation when the architecture supports localized progress.

Avoid allowing conflicting operations when state correctness requires a temporary lock.

---

# 70. Realtime feedback

Realtime updates should not create chaotic visual movement.

When another actor updates data:

- preserve user's focus;
- avoid unnecessary reordering while editing where possible;
- communicate relevant conflict;
- update unobtrusively when safe.

Do not animate every incoming event.

---

# 71. Multi-user editing

Where concurrent collaboration exists, design may communicate:

- presence;
- active editors;
- selection;
- conflict;
- latest state.

Ephemeral presence must not be represented as durable product state.

---

# 72. Notifications and interruption budget

Not every event deserves interruption.

Choose channel based on urgency/actionability:

```text
inline
badge
notification center
toast
email/push
blocking modal
```

Blocking interruption should be rare.

Automation of notifications must avoid alert fatigue.

---

# 73. Error prevention versus recovery

Good design prevents high-cost errors and makes recoverable errors cheap.

Use stronger prevention for:

- irreversible deletion;
- access changes;
- billing-impacting actions;
- external provider actions with uncertain reversibility.

Use smooth recovery for:

- reversible local changes;
- validation;
- transient connectivity.

---

# 74. Product consistency versus platform convention

Shared product semantics matter.

Platform convention also matters.

When they conflict, preserve semantic meaning while adapting interaction to the host.

Example:

```text
same action: open Item detail

web:
side panel / route

mobile:
native stack screen
```

The interaction can differ.

The product action and state must remain equivalent.

---

# 75. Design anti-patterns

The following patterns require active justification and are generally discouraged.

---

## 75.1 Generic SaaS template

Symptoms:

- repetitive icon cards;
- generic hero metrics;
- decorative gradient blobs;
- tiny uppercase eyebrow labels everywhere;
- same card layout for unrelated content.

Reason:

It weakens product-specific information hierarchy.

---

## 75.2 Cluttered enterprise

Symptoms:

- nested permanent toolbars;
- every capability visible simultaneously;
- panels inside panels;
- modal over modal;
- dozens of status colors.

Reason:

Enterprise capability is not measured by visible controls per pixel.

---

## 75.3 Toy-like motion/color

Symptoms:

- bounce-heavy feedback;
- rainbow surfaces everywhere;
- decorative animation on routine updates.

Reason:

It reduces confidence and long-session usability.

---

## 75.4 Flat/no-hierarchy calm

Symptoms:

- everything gray;
- weak selection;
- weak focus;
- no clear primary action;
- no visible state difference.

Reason:

Calm is not absence of hierarchy.

---

## 75.5 Card-everything design

Symptoms:

Every row/section/resource becomes a rounded shadow card.

Reason:

Cards stop expressing grouping/elevation when everything is a card.

---

## 75.6 Pill-everything design

Symptoms:

Every label/button/filter/metadata value is pill-shaped.

Reason:

Semantic distinctions collapse.

---

## 75.7 Modal workflow tunnel

Symptoms:

Dialog opens dialog opens another dialog.

Reason:

Users lose context, focus management breaks, mobile becomes unusable.

---

## 75.8 Color-only product model

Symptoms:

Status or assignment understood only by color.

Reason:

Accessibility and semantic clarity fail.

---

## 75.9 Optimistic fiction

Symptoms:

UI instantly mutates complex state and never properly reconciles failure/conflict.

Reason:

The interface becomes a competing source of truth.

---

## 75.10 Desktop squeezed into mobile

Symptoms:

Desktop table/toolbars compressed to narrow width.

Reason:

Semantic parity does not require layout parity.

---

# 76. Work-surface design checklist

For every substantial authenticated product surface, answer:

### Context

- What product capability/resource is this?
- What user goal dominates?
- Is tenant/workspace context clear?

### Hierarchy

- What is primary?
- What is secondary?
- What is metadata?
- What is currently selected/focused?

### Actions

- What is the primary action?
- Which actions are frequent?
- Which are destructive?
- Which require permission/entitlement?

### State

- loading?
- empty?
- error?
- permission?
- read-only?
- conflict?
- reconnecting?
- stale?
- destructive confirmation?

### Input

- keyboard?
- pointer?
- touch?
- screen reader?
- form validation?

### Scale

- pagination?
- virtualization?
- large result handling?
- progressive loading?

### Convergence

- optimistic?
- server result?
- realtime?
- conflict?

If the surface cannot answer the relevant questions, it is not design-complete.

---

# 77. Component design checklist

Before adding/changing a reusable component:

```text
Who owns it?
Is it primitive or product-specific?
Which hosts use it?
Which states exist?
What keyboard behavior?
What touch behavior?
What accessibility semantics?
Which tokens?
What loading/error behavior belongs here?
What does NOT belong here?
Which tests prove it?
```

Do not promote product-specific components to generic UI merely because they are reused.

---

# 78. Design review severity

Design findings may be classified:

## Blocker

Examples:

- inaccessible primary workflow;
- permission-protected data flash;
- UI represents failed mutation as success;
- destructive action consequence hidden;
- mobile production path depends on web-only primitive.

## Major

Examples:

- broken hierarchy;
- confusing product vocabulary;
- missing important error/conflict state;
- severe density/interaction inconsistency.

## Minor

Examples:

- local spacing inconsistency;
- secondary alignment;
- small polish issue without semantic/accessibility impact.

Do not classify a semantic/accessibility/state-integrity issue as “polish”.

---

# 79. Design change classification

## Local visual implementation

Examples:

- token-aligned spacing adjustment;
- icon alignment;
- local responsive fix.

May not require root DESIGN change.

---

## Design-system contract change

Examples:

- global interaction behavior;
- token semantic role;
- primitive state model;
- accessibility baseline implementation.

Requires frontend UI architecture review and relevant tests.

---

## Product-design semantic change

Examples:

- changing product versus marketing register;
- changing accessibility baseline;
- changing direct-manipulation meaning;
- changing product-wide state grammar.

Requires update to this constitution and product/design review.

---

# 80. Relationship to tokens

Tokens implement semantic decisions.

Typical semantic categories may include:

```text
color
typography
spacing
radius
elevation
motion
breakpoints
z/layering
```

Exact category structure is implementation-owned.

Do not introduce one-off literal values in product code when an appropriate semantic token exists.

Do not create a new global token for a one-component implementation detail without proving reusable semantic meaning.

---

# 81. Theme and appearance

If multiple themes/appearance modes exist, semantic meaning must survive theme changes.

Examples:

- selected remains selected;
- danger remains danger;
- focus remains visible;
- contrast remains compliant.

A dark theme is not merely color inversion.

Theme-specific surfaces/elevation may need host-specific implementation.

---

# 82. Brand usage

Brand accents should identify/energize important moments without covering operational screens in brand color.

The product should remain recognizable even when most of the authenticated surface is neutral/content-led.

Marketing may carry more brand color.

---

# 83. Images and illustration

Illustration can support:

- onboarding;
- empty state;
- marketing storytelling;
- explanatory education.

Avoid illustrations that:

- consume large space in high-frequency operational flows;
- hide actionable information;
- look generic/unrelated;
- reduce accessibility/performance without value.

---

# 84. Data and privacy in visual design

Do not expose sensitive information merely because it is visually useful.

Consider:

- screenshots;
- previews;
- notifications;
- hover cards;
- recent items;
- command palette;
- activity feed.

Permission and privacy semantics apply to previews as well as full pages.

---

# 85. Localization readiness

Design should avoid assumptions that:

- labels are short;
- dates have one format;
- names fit fixed widths;
- pluralization is trivial;
- left-to-right is guaranteed forever.

Even if localization is not currently complete, avoid unnecessary layout decisions that make it prohibitively difficult.

---

# 86. Descriptive names over internal names

User-facing strings should use product vocabulary.

Avoid exposing:

```text
BoardItemDto
ResourceKind
AggregateVersion
IdempotencyOperation
RLS
Outbox
```

unless the interface is explicitly developer/admin tooling where technical terms are appropriate.

---

# 87. Admin and technical surfaces

Admin/operational interfaces may legitimately expose more technical detail.

Even there:

- hierarchy;
- permission;
- destructive consequences;
- error precision;
- accessibility

still apply.

Do not use internal/admin context as justification for poor interaction quality.

---

# 88. Marketing honesty

Marketing screenshots/copy must not imply:

- nonexistent features;
- nonexistent AI behavior;
- unsupported realtime guarantees;
- unsupported compliance certification;
- misleading performance;
- misleading collaboration state.

Product truth outranks conversion copy.

---

# 89. Design evidence

Design quality should be proven through combinations of:

- source implementation;
- token usage;
- component tests;
- accessibility tests;
- Storybook/component gallery where used;
- web/mobile host tests;
- E2E;
- manual keyboard review;
- screen-reader review for complex widgets;
- visual review;
- product behavior tests.

A static screenshot does not prove interaction quality.

---

# 90. Canonical references

Product meaning:

- [`PRODUCT.md`](PRODUCT.md)
- `docs/product/**`

Repository invariants:

- [`RULE.md`](RULE.md)

Frontend implementation:

- `frontend/docs/architecture/ui-and-design-system.md`
- `frontend/docs/architecture/dependency-boundaries.md`
- `frontend/docs/architecture/hosts-composition-routing.md`
- `frontend/docs/architecture/state-query-mutations.md`
- `frontend/docs/architecture/realtime.md`

Quality:

- `docs/quality/accessibility-standard.md`
- `docs/quality/performance-and-scalability.md`

Current state:

- [`CONTEXT.md`](CONTEXT.md)

---

# 91. Design completion standard

A product UI change is not complete merely because:

- it looks polished;
- it matches a screenshot;
- it renders without errors;
- it uses shared tokens.

It is complete when, as applicable:

- product semantics are represented truthfully;
- hierarchy is clear;
- density supports the task;
- primary/secondary actions are appropriate;
- loading/empty/error/permission/read-only/conflict states are designed;
- optimistic/realtime behavior converges to authoritative state;
- keyboard/pointer/touch behavior is coherent;
- accessibility baseline is satisfied;
- web/mobile host boundaries are respected;
- literal styles use appropriate token/component ownership;
- destructive behavior is explicit;
- large-data behavior remains usable;
- tests/review provide evidence.

---

# 92. Design constitution

Notrelix should not be designed by copying the visible style of another productivity product.

Reference products may teach useful lessons.

They do not define Notrelix.

The durable design position is:

```text
Work first.
State truthful.
Hierarchy clear.
Density useful.
Chrome restrained.
Interaction predictable.
Accessibility built in.
Motion purposeful.
Product and marketing related, not identical.
Web and mobile semantically coherent, not mechanically identical.
```

The intended feeling remains:

> **calm · focused · confident**

A successful Notrelix surface allows the user to spend attention on their work rather than on understanding the interface.
