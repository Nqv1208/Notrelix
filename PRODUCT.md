# Product

## Register

product

> Product-led, but mixed. The app UI (workspaces, boards, tables, document editor, dashboards) is the primary surface and follows the **product** register: design serves the work. The marketing surfaces (`app/(app)/v2` landing, `contact`, `terms`, `privacy`) switch to the **brand** register per task — bold, polished, conversion-focused. When a task targets a marketing page, treat it as brand; everything else is product.

## Users

Teams and individuals running projects and knowledge work in one place. They arrive already in a workflow — triaging a board, writing a doc, scanning a table, checking what's due — and the interface's job is to disappear so the work stays in focus. They range from technical power users (keyboard-driven, multi-workspace) to non-technical collaborators invited into a workspace. Context of use: focused desk sessions, often for long stretches, in both light and dark environments.

## Product Purpose

Notrelix is a SaaS workspace that unifies three tools that are usually separate:

- **Notion-like documents** — block-based editor with rich content types
- **Trello-like boards** — Kanban project management with cards, lists, labels, checklists
- **Calendar sync** — two-way sync with Google Calendar for cards and pages

Success is a team that stops switching between a doc tool, a board tool, and a calendar — and trusts Notrelix to hold all three coherently. The product wins on **coherence and flow**, not on having the most features.

## Brand Personality

**Calm · focused · confident.**

The app should feel quiet and spacious — low-stimulation, gets out of the way so the content (boards, docs, data) is the star. Closer to Linear/Notion than to a busy enterprise dashboard. Motion is purposeful and restrained; color is carried by content and status, not by chrome. Copy is plain, direct, and unhurried.

The marketing surfaces are allowed to be louder: bold, polished, product-led, conversion-focused, with confident copy and refined (not rainbow) gradients — ClickUp-grade craft without cloning ClickUp. The personality stays the same; the volume goes up.

## Anti-references

- **Generic SaaS template.** No cream/sand/paper body backgrounds, no hero-metric blocks (big number + label + stat row + gradient), no identical icon-card grids, no tiny uppercase tracked eyebrow above every section, no numbered `01 / 02 / 03` section scaffolding by reflex.
- **Cluttered enterprise (Jira).** No dense toolbars, nested panels, modal-on-modal, or visual noise that buries the primary task. Density should never cost clarity.
- **Toy-like / overly playful.** No heavy bounce/elastic animation, no oversaturated rainbow palettes, no cartoonish illustration. Friendly ≠ childish.
- **Flat & lifeless.** No zero-motion, gray-on-gray, hierarchy-free screens. Calm is not the same as dead — craft and small moments of delight still belong.
- **Marketing-specific:** ClickUp is an *inspiration ceiling, not a template* — do not clone it, avoid generic SaaS hero templates, avoid excessive rainbow gradients, and keep the **app** UI calmer and more focused than the marketing pages.

## Design Principles

1. **The work is the star.** Chrome recedes; content (boards, docs, tables, status) carries the color and the eye. If a UI element competes with the user's content, it's wrong.
2. **Coherence over surface area.** Three tools, one feel. Shared tokens, shared components, shared interaction grammar — a board card and a doc block should feel like the same product.
3. **Calm density.** Power users need information density; deliver it without noise. Spacing, hierarchy, and restraint do the work that extra borders and boxes can't.
4. **Confident, plain language.** Labels, empty states, and errors say what's true in the fewest clear words. No jargon, no hype in the app; earned confidence in marketing.
5. **Volume scales with surface.** Same brand, different loudness: quiet in the app, bold on the landing page. Never invert this.

## Accessibility & Inclusion

**Baseline: WCAG 2.2 AA for all future work.**

- Body text ≥ 4.5:1 contrast; large text ≥ 3:1; placeholders held to the same bar as body.
- Full keyboard navigation with visible, non-default focus states.
- Semantic HTML and accessible forms (labels, error association, fieldsets).
- Reduced-motion alternatives for every animation (`prefers-reduced-motion: reduce`).
- Status and meaning never conveyed by color alone (pair with icon, text, or shape — applies to badges, board labels, calendar states).
- **AAA where feasible** for critical text and long-reading surfaces (the document editor), but never at the cost of product clarity or usability.
