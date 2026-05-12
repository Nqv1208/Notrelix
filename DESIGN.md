# Notrelix — Design System

> *Where structured thought meets vibrant action.*
> Notrelix merges Notion's editorial depth with Monday.com's kinetic energy — a workspace that breathes when you write and pulses when you manage.

---

## 1. Vision & Philosophy

| Pillar | Notion Influence | Monday Influence | Notrelix Synthesis |
|--------|-----------------|------------------|--------------------|
| **Aesthetic** | Dark, precise, editorial | Light, vibrant, playful | Dual-mode: calm canvas + vivid action layer |
| **Typography** | Tight-tracked Inter, sparse serif accents | Geometric Poppins, bold hierarchy | Poppins for UI chrome, Inter for document body |
| **Color** | Midnight Ink depth, accent bursts | Rainbow card palette, Violet CTA | Dark structural shell + colorful content surfaces |
| **Shape language** | 12px cards, 8px buttons, pill badges | 24px cards, 160px pill buttons | 16px cards, 100px pill buttons — a confident middle |
| **Motion** | Precise, restrained transitions | Energetic, flowing gradients | Purposeful: calm by default, lively on action |

---

## 2. Color Tokens

### 2.1 Primitive Palette

| Name | Hex | Token | Origin | Role |
|------|-----|-------|--------|------|
| **Deep Space** | `#02093a` | `--color-deep-space` | Notion | Hero backgrounds, sidebar, dark surfaces |
| **Void Black** | `#000000` | `--color-void-black` | Notion | Page canvas (dark mode), darkest neutral |
| **Paper** | `#ffffff` | `--color-paper` | Both | Light canvas, card backgrounds, text-on-dark |
| **Fog** | `#f5f6f8` | `--color-fog` | Monday | Subtle section backgrounds, badge fills |
| **Mist** | `#f6f5f4` | `--color-mist` | Notion | Off-white panels, hover states |
| **Graphite** | `#333333` | `--color-graphite` | Monday | Primary body text, headings (light mode) |
| **Slate** | `#676879` | `--color-slate` | Monday | Secondary text, metadata, muted labels |
| **Iron** | `#535768` | `--color-iron` | Monday | Nav links, interactive neutral text |
| **Silver** | `#d0d4e4` | `--color-silver` | Monday | Card borders, dividers, input borders |
| **Ash** | `#c6c6c5` | `--color-ash` | Notion | Subtle borders on dark surfaces |

### 2.2 Brand & Interactive

| Name | Hex | Token | Role |
|------|-----|-------|------|
| **Notrelix Violet** | `#6161ff` | `--color-brand-violet` | Primary CTA, active states, brand accent |
| **Deep Indigo** | `#455dd3` | `--color-brand-indigo` | Secondary CTA, dark-mode primary action |
| **Electric Purple** | `#9450fd` | `--color-brand-purple` | Gradient start, feature highlights |
| **Ocean Blue** | `#0075de` | `--color-brand-ocean` | Links, info states, interactive hover |
| **Sky Blue** | `#3ac9ff` | `--color-brand-sky` | Accent buttons, badge highlights |
| **Blue Frost** | `#62aef0` | `--color-brand-frost` | Decorative glows on dark surfaces |

### 2.3 Semantic States

| Name | Hex | Token | Role |
|------|-----|-------|------|
| **Success Green** | `#1aae39` | `--color-success` | Completion, positive status |
| **Warning Gold** | `#ffb110` | `--color-warning` | Alerts, pending states |
| **Danger Red** | `#f64932` | `--color-danger` | Errors, destructive actions |
| **Info Blue** | `#097fe8` | `--color-info` | Informational badges, tooltips |

### 2.4 Content Surface Palette

Used to color-code pages, boards, and project cards — directly inherited from Monday's card system, unified with Notion's functional accents.

| Name | Hex | Token | Pairing Use |
|------|-----|-------|-------------|
| **Mint** | `#bcfe90` | `--color-surface-mint` | Tasks done, personal workspace |
| **Lavender** | `#eddff7` | `--color-surface-lavender` | Design & creative projects |
| **Sky** | `#abf0ff` | `--color-surface-sky` | Research, documentation |
| **Sunset** | `#ff8940` | `--color-surface-sunset` | Urgent, active sprints |
| **Pale Blue** | `#e7ecff` | `--color-surface-pale-blue` | General pages, notes |
| **Ocean** | `#93beff` | `--color-surface-ocean` | Engineering projects |
| **Ice** | `#d1faff` | `--color-surface-ice` | Archive, cold storage |
| **Fuchsia** | `#ff83dd` | `--color-surface-fuchsia` | Marketing & events |
| **Gold** | `#ffc95e` | `--color-surface-gold` | Finance, analytics |
| **Teal** | `#2a9d99` | `--color-surface-teal` | Operations, logistics |
| **Coral** | `#ff8a33` | `--color-surface-coral` | Product roadmap |
| **Grape** | `#ad6ded` | `--color-surface-grape` | Strategy & planning |

### 2.5 Gradients

| Name | Value | Token | Use |
|------|-------|-------|-----|
| **Vibrant Flow** | `linear-gradient(90deg, #fe81e4 0%, #fe81e4 31%, #fda900 88%)` | `--gradient-vibrant-flow` | Hero decorative elements |
| **Brand Sweep** | `linear-gradient(135deg, #6161ff 0%, #9450fd 100%)` | `--gradient-brand-sweep` | Primary CTA, feature callouts |
| **Depth Fade** | `linear-gradient(180deg, #02093a 0%, #000000 100%)` | `--gradient-depth-fade` | Dark hero sections, sidebar |
| **Spectrum Ring** | `conic-gradient(from 270deg, #8181ff 15%, #33dbdb 40%, #33d58e 55%, #ffd633 65%, #fc527d 85%, #8181ff 100%)` | `--gradient-spectrum-ring` | Avatar rings, loading indicators |
| **Glass Light** | `linear-gradient(135deg, rgba(255,255,255,0.15), rgba(255,255,255,0.05))` | `--gradient-glass-light` | Glass cards on dark backgrounds |

---

## 3. Typography

### 3.1 Font Stack

| Role | Family | Token | Origin | Fallback |
|------|--------|-------|--------|----------|
| **Display & UI Chrome** | Poppins | `--font-display` | Monday | `ui-sans-serif, system-ui, sans-serif` |
| **Document Body** | Inter | `--font-body` | Notion | `ui-sans-serif, system-ui, sans-serif` |
| **Editorial Accent** | Lyon Text | `--font-editorial` | Notion | `Georgia, 'Times New Roman', serif` |
| **Code & Monospace** | JetBrains Mono | `--font-mono` | Notrelix | `'Courier New', monospace` |

**Rationale:** Poppins owns the shell (nav, headings, buttons, labels) because its geometric confidence drives action. Inter owns the content (paragraphs, block notes, database cells) because its precision sustains long reading. Lyon Text appears only for pull-quotes and editorial page headers, adding humanity.

### 3.2 Type Scale

| Role | Size | Line Height | Letter Spacing | Font | Weight | Token |
|------|------|-------------|----------------|------|--------|-------|
| `caption` | 12px | 1.5 | +0.01em | Inter | 400 | `--text-caption` |
| `label` | 13px | 1.4 | +0.01em | Poppins | 500 | `--text-label` |
| `body-sm` | 14px | 1.5 | -0.006em | Inter | 400 | `--text-body-sm` |
| `body` | 16px | 1.6 | -0.006em | Inter | 400 | `--text-body` |
| `body-lg` | 18px | 1.5 | -0.011em | Inter | 400 | `--text-body-lg` |
| `subheading` | 20px | 1.35 | -0.011em | Poppins | 500 | `--text-subheading` |
| `heading-sm` | 24px | 1.3 | -0.015em | Poppins | 600 | `--text-heading-sm` |
| `heading` | 32px | 1.3 | -0.02em | Poppins | 700 | `--text-heading` |
| `heading-lg` | 40px | 1.2 | -0.02em | Poppins | 700 | `--text-heading-lg` |
| `display-sm` | 52px | 1.15 | -0.03em | Poppins | 700 | `--text-display-sm` |
| `display` | 64px | 1.1 | -0.04em | Poppins | 700 | `--text-display` |
| `editorial` | 32px | 1.25 | normal | Lyon Text | 400 | `--text-editorial` |

---

## 4. Spacing & Grid

### 4.1 Base Unit

**Base:** `8px` (Monday-inherited, comfortable density)

### 4.2 Spacing Scale

| Token | Value | Use |
|-------|-------|-----|
| `--spacing-4` | 4px | Micro gaps, icon padding |
| `--spacing-8` | 8px | Element gap, inline spacing |
| `--spacing-12` | 12px | Badge padding, compact row gap |
| `--spacing-16` | 16px | Input padding, list item gap |
| `--spacing-24` | 24px | Card padding, section inner gap |
| `--spacing-32` | 32px | Column gap, block spacing |
| `--spacing-40` | 40px | Large element separation |
| `--spacing-48` | 48px | Section gap (between major zones) |
| `--spacing-64` | 64px | Hero vertical padding |
| `--spacing-80` | 80px | Page-level breathing room |
| `--spacing-96` | 96px | Max hero section padding |

### 4.3 Layout Grid

| Context | Columns | Gutter | Max Width |
|---------|---------|--------|-----------|
| Desktop | 12 | 24px | 1280px |
| Tablet | 8 | 16px | 960px |
| Mobile | 4 | 16px | 100% |

**Section rhythm:** 48px gap between major sections. Content blocks within a section use 32px.

---

## 5. Shape & Depth

### 5.1 Border Radius

| Element | Value | Token | Rationale |
|---------|-------|-------|-----------|
| Buttons (primary) | 100px | `--radius-pill` | Monday's pill energy, softened |
| Buttons (secondary/ghost) | 8px | `--radius-button` | Notion's grounded secondary actions |
| Cards | 16px | `--radius-card` | Between Monday (24) and Notion (12) |
| Inputs | 6px | `--radius-input` | Approachable but structured |
| Badges | 9999px | `--radius-badge` | Full pill — Notion's tag language |
| Images | 12px | `--radius-image` | Consistent media rounding |
| Modals | 20px | `--radius-modal` | Elevated, prominent |
| Tooltips | 6px | `--radius-tooltip` | Compact, clear |

### 5.2 Shadow Scale

| Token | Value | Use |
|-------|-------|-----|
| `--shadow-xs` | `rgba(0,0,0,0.04) 0px 1px 3px` | Inline elevated elements |
| `--shadow-sm` | `rgba(205,208,223,0.4) 0px 2px 16px` | Default card elevation |
| `--shadow-md` | `rgba(205,208,223,0.4) 0px 2px 48px` | Feature cards (Monday xl) |
| `--shadow-lg` | `rgba(0,0,0,0.15) 0px 5px 45px` | Modals, dropdowns |
| `--shadow-xl` | `rgba(0,0,0,0.4) 0px 5px 55px` | Dark surface elevation |
| `--shadow-glow-violet` | `0px 0px 24px rgba(97,97,255,0.35)` | Brand CTA hover glow |
| `--shadow-glow-frost` | `0px 0px 20px rgba(98,174,240,0.3)` | Dark mode interactive glow |
| `--shadow-notion` | `rgba(0,0,0,0.01) 0px 1px 3px, rgba(0,0,0,0.02) 0px 3px 7px, rgba(0,0,0,0.04) 0px 14px 28px` | Deep layered elevation (Notion) |

---

## 6. Components

### 6.1 Buttons

#### Primary CTA (Monday-dominant)
```
Background:   --color-brand-violet (#6161ff)
Text:         --color-paper (#ffffff)
Font:         Poppins 16px weight 500
Radius:       100px (pill)
Padding:      13px 28px
Hover:        background → #4f4fe8, box-shadow: --shadow-glow-violet
Active:       scale(0.97)
```

#### Secondary / Outlined
```
Background:   transparent
Border:       1.5px solid --color-silver (#d0d4e4)
Text:         --color-graphite (#333333)
Font:         Poppins 16px weight 500
Radius:       8px
Padding:      12px 24px
Hover:        border-color → --color-brand-violet, text → --color-brand-violet
```

#### Ghost Dark (for dark backgrounds)
```
Background:   transparent
Border:       1px solid rgba(255,255,255,0.2)
Text:         --color-paper (#ffffff)
Font:         Poppins 16px weight 400
Radius:       8px
Padding:      10px 20px
Hover:        background → rgba(255,255,255,0.08)
```

#### Destructive
```
Background:   --color-danger (#f64932)
Text:         --color-paper (#ffffff)
Font:         Poppins 14px weight 500
Radius:       8px
Padding:      10px 20px
```

---

### 6.2 Cards

#### Feature Card (Light)
```
Background:   --color-paper (#ffffff)
Border:       1px solid --color-silver (#d0d4e4)
Radius:       16px
Padding:      24px
Shadow:       --shadow-md
Hover:        shadow → --shadow-lg, translateY(-2px)
```

#### Accent Color Card
```
Background:   any --color-surface-* token
Border:       none
Radius:       16px
Padding:      24px
Shadow:       none
Use:          Project cards, board columns, page covers
```

#### Dark Feature Card (Notion-dominant)
```
Background:   --color-deep-space (#02093a)
Border:       1px solid rgba(98,174,240,0.15)
Radius:       16px
Padding:      24px
Shadow:       --shadow-xl
Text:         --color-paper (#ffffff)
Use:          Dark hero sections, premium feature callouts
```

#### Document Block Card
```
Background:   --color-paper (#ffffff)
Border:       none
Radius:       8px
Padding:      16px 20px
Shadow:       --shadow-xs
Left border:  3px solid --color-brand-violet (for callouts)
Use:          Note blocks, callouts, inline content cards
```

#### Glass Card (on dark backgrounds)
```
Background:   linear-gradient(135deg, rgba(255,255,255,0.1), rgba(255,255,255,0.04))
Backdrop:     blur(12px)
Border:       1px solid rgba(255,255,255,0.12)
Radius:       16px
Padding:      24px
Use:          Dark hero overlays, modals on dark surface
```

---

### 6.3 Navigation

#### Top Bar
```
Background:   --color-paper (#ffffff) or --color-deep-space (dark mode)
Height:       56px
Padding:      0 24px
Border-bottom: 1px solid --color-silver
Position:     sticky top-0
z-index:      100
Shadow:       --shadow-sm (on scroll)
```

#### Sidebar
```
Width:        240px (expanded), 56px (collapsed)
Background:   --color-mist (#f6f5f4) light / --color-deep-space (#02093a) dark
Border-right: 1px solid --color-silver
Font:         Inter 14px weight 400
Item height:  32px
Item radius:  6px
Item hover:   background rgba(0,0,0,0.06)
Item active:  background rgba(97,97,255,0.1), text --color-brand-violet
```

---

### 6.4 Inputs & Forms

#### Text Input
```
Background:   --color-paper (#ffffff)
Border:       1.5px solid --color-silver (#d0d4e4)
Radius:       6px
Padding:      10px 14px
Font:         Inter 16px weight 400
Color:        --color-graphite
Placeholder:  --color-slate (#676879)
Focus:        border-color → --color-brand-violet, box-shadow: 0 0 0 3px rgba(97,97,255,0.15)
```

#### Search Input (Monday-style)
```
Background:   --color-fog (#f5f6f8)
Border:       none
Radius:       100px (pill)
Padding:      10px 20px
Font:         Poppins 15px weight 400
Focus:        background → --color-paper, border: 1.5px solid --color-brand-violet
```

#### Document Title Input (Notion-style)
```
Background:   transparent
Border:       none
Font:         Poppins 40px weight 700
Letter-spacing: -0.02em
Color:        --color-graphite
Placeholder:  rgba(51,51,51,0.25)
```

---

### 6.5 Badges & Tags

#### Status Badge (Pill)
```
Radius:       9999px
Padding:      4px 10px
Font:         Inter 12px weight 500
Background:   any --color-surface-* (40% opacity for subtle), or solid for vivid
```

#### Category Badge (Monday-style)
```
Background:   --color-fog (#f5f6f8)
Text:         --color-graphite (#333333)
Radius:       12px
Padding:      6px 14px
Font:         Poppins 13px weight 500
```

#### Informational Badge (Notion-style)
```
Background:   --color-surface-pale-blue (#e7ecff)
Text:         --color-brand-ocean (#0075de)
Radius:       9999px
Padding:      3px 10px
Font:         Inter 12px weight 500
```

---

### 6.6 Tables & Databases

Notrelix's signature element — the Notion database table with Monday's color-coding.

```
Header row:
  Background:   --color-fog (#f5f6f8)
  Font:         Poppins 13px weight 600
  Color:        --color-iron (#535768)
  Border-bottom: 2px solid --color-silver

Body rows:
  Background:   --color-paper (#ffffff)
  Font:         Inter 14px weight 400
  Color:        --color-graphite (#333333)
  Border-bottom: 1px solid --color-silver
  Hover:        background → rgba(97,97,255,0.04)

Property cell (status type):
  Use accent color surface tokens as pill backgrounds
  
Cell padding: 12px 16px
Row height:   40px (compact), 56px (comfortable)
```

---

## 7. Motion & Animation

| Token | Value | Use |
|-------|-------|-----|
| `--duration-instant` | 80ms | Micro feedback (click, press) |
| `--duration-fast` | 150ms | Hover states, badge transitions |
| `--duration-base` | 250ms | Card transitions, sidebar open |
| `--duration-slow` | 400ms | Page transitions, modal enter |
| `--duration-deliberate` | 600ms | Hero animations, onboarding reveals |
| `--ease-out` | `cubic-bezier(0.0, 0.0, 0.2, 1)` | Elements entering |
| `--ease-in` | `cubic-bezier(0.4, 0.0, 1, 1)` | Elements leaving |
| `--ease-spring` | `cubic-bezier(0.34, 1.56, 0.64, 1)` | Playful bounces (Monday energy) |

**Principles:**
- Sidebar: slide + fade, 250ms ease-out
- Cards on hover: `translateY(-2px)` + shadow upgrade, 150ms
- CTA buttons: glow pulse on hover, scale(0.97) on press
- Page transitions: Notion-style fade-in with subtle `translateY(4px)` lift
- Board column drop: spring ease for satisfying card placement

---

## 8. Surfaces & Layers

| Level | Name | Light Value | Dark Value | Purpose |
|-------|------|-------------|------------|---------|
| `-1` | Underlay | `#f0f1f4` | `#000000` | Page background beneath everything |
| `0` | Canvas | `#ffffff` | `#02093a` | Main content area |
| `1` | Raised | `#f5f6f8` | `rgba(255,255,255,0.04)` | Sidebar, panel backgrounds |
| `2` | Card | `#ffffff` | `rgba(255,255,255,0.08)` | Card surfaces |
| `3` | Overlay | `#ffffff` | `#02093a` | Modals, dropdowns, tooltips |
| `4` | Toast | `#1a1a2e` | `#ffffff` | Notification toasts (inverted) |

---

## 9. Page Modes

### 9.1 Document Mode (Notion-dominant)
Used when editing a page/note. Maximum reading comfort.

```
Sidebar:        240px, --color-mist background
Content area:   max-width 720px, centered
Font:           Inter for body, Poppins for page title
Line height:    1.7 for body text
Background:     --color-paper (#ffffff)
Toolbar:        minimal, appears on selection
Block spacing:  4px between blocks
```

### 9.2 Board Mode (Monday-dominant)
Used in project boards, kanban views.

```
Layout:         horizontal scroll, fixed columns
Column width:   280px
Column bg:      --color-fog (#f5f6f8)
Card style:     Accent Color Cards with color-coded headers
Drag indicator: --color-brand-violet 2px left border + shadow
Add card btn:   Dashed border, --color-slate text
```

### 9.3 Dashboard Mode (Hybrid)
Used for the home screen and analytics.

```
Layout:         12-column grid, drag-resizable widgets
Widget style:   Feature Card (Light)
Section titles: Poppins 20px weight 600
Charts:         Use surface palette for bar/pie colors
KPI cards:      Dark Feature Card with glow accent
```

---

## 10. Do's and Don'ts

### Do ✅
- Use `--color-brand-violet` (#6161ff) as the single primary action color across all modes
- Apply `--radius-pill` (100px) only to primary CTA buttons — nowhere else
- Use Poppins for all UI chrome (labels, nav, headings); Inter for all content blocks
- Color-code project entities exclusively with the Content Surface Palette
- Maintain 48px section gap between major layout zones
- Use the Dark Feature Card with `--shadow-glow-frost` for any premium/dark-surface callout
- Respect Inter's tight negative tracking — do not apply Poppins tracking values to Inter text

### Don't ❌
- Don't mix two vivid surface colors on adjacent cards without a neutral buffer
- Don't apply `--radius-pill` to inputs or secondary buttons — it belongs only to the primary CTA
- Don't use Lyon Text for anything smaller than 28px or any UI element; it's editorial-only
- Don't deviate from the shadow scale — don't create new custom shadows
- Don't use `--color-brand-violet` as a background fill on large surfaces (only buttons & accents)
- Don't center body text in document mode — always left-align content blocks
- Don't overcrowd board cards — maximum 4 properties visible without expanding

---

## 11. CSS Custom Properties (Complete)

```css
:root {
  /* ─── Base Colors ─── */
  --color-deep-space:           #02093a;
  --color-void-black:           #000000;
  --color-paper:                #ffffff;
  --color-fog:                  #f5f6f8;
  --color-mist:                 #f6f5f4;
  --color-graphite:             #333333;
  --color-slate:                #676879;
  --color-iron:                 #535768;
  --color-silver:               #d0d4e4;
  --color-ash:                  #c6c6c5;

  /* ─── Brand ─── */
  --color-brand-violet:         #6161ff;
  --color-brand-indigo:         #455dd3;
  --color-brand-purple:         #9450fd;
  --color-brand-ocean:          #0075de;
  --color-brand-sky:            #3ac9ff;
  --color-brand-frost:          #62aef0;

  /* ─── Semantic ─── */
  --color-success:              #1aae39;
  --color-warning:              #ffb110;
  --color-danger:               #f64932;
  --color-info:                 #097fe8;

  /* ─── Content Surfaces ─── */
  --color-surface-mint:         #bcfe90;
  --color-surface-lavender:     #eddff7;
  --color-surface-sky:          #abf0ff;
  --color-surface-sunset:       #ff8940;
  --color-surface-pale-blue:    #e7ecff;
  --color-surface-ocean:        #93beff;
  --color-surface-ice:          #d1faff;
  --color-surface-fuchsia:      #ff83dd;
  --color-surface-gold:         #ffc95e;
  --color-surface-teal:         #2a9d99;
  --color-surface-coral:        #ff8a33;
  --color-surface-grape:        #ad6ded;

  /* ─── Gradients ─── */
  --gradient-brand-sweep:       linear-gradient(135deg, #6161ff 0%, #9450fd 100%);
  --gradient-vibrant-flow:      linear-gradient(90deg, #fe81e4 0%, #fe81e4 31%, #fda900 88%);
  --gradient-depth-fade:        linear-gradient(180deg, #02093a 0%, #000000 100%);
  --gradient-glass-light:       linear-gradient(135deg, rgba(255,255,255,0.15), rgba(255,255,255,0.05));
  --gradient-spectrum-ring:     conic-gradient(from 270deg, #8181ff 15%, #33dbdb 40%, #33d58e 55%, #ffd633 65%, #fc527d 85%, #8181ff 100%);

  /* ─── Typography ─── */
  --font-display:   'Poppins', ui-sans-serif, system-ui, sans-serif;
  --font-body:      'Inter', ui-sans-serif, system-ui, sans-serif;
  --font-editorial: 'Lyon Text', Georgia, 'Times New Roman', serif;
  --font-mono:      'JetBrains Mono', 'Courier New', monospace;

  --font-weight-regular:   400;
  --font-weight-medium:    500;
  --font-weight-semibold:  600;
  --font-weight-bold:      700;

  /* ─── Type Scale ─── */
  --text-caption:      12px; --leading-caption:      1.5;  --tracking-caption:     0.01em;
  --text-label:        13px; --leading-label:        1.4;  --tracking-label:       0.01em;
  --text-body-sm:      14px; --leading-body-sm:      1.5;  --tracking-body-sm:    -0.006em;
  --text-body:         16px; --leading-body:         1.6;  --tracking-body:       -0.006em;
  --text-body-lg:      18px; --leading-body-lg:      1.5;  --tracking-body-lg:    -0.011em;
  --text-subheading:   20px; --leading-subheading:   1.35; --tracking-subheading: -0.011em;
  --text-heading-sm:   24px; --leading-heading-sm:   1.3;  --tracking-heading-sm: -0.015em;
  --text-heading:      32px; --leading-heading:      1.3;  --tracking-heading:    -0.02em;
  --text-heading-lg:   40px; --leading-heading-lg:   1.2;  --tracking-heading-lg: -0.02em;
  --text-display-sm:   52px; --leading-display-sm:   1.15; --tracking-display-sm: -0.03em;
  --text-display:      64px; --leading-display:      1.1;  --tracking-display:    -0.04em;
  --text-editorial:    32px; --leading-editorial:    1.25; --tracking-editorial:  normal;

  /* ─── Spacing ─── */
  --spacing-unit: 8px;
  --spacing-4:    4px;
  --spacing-8:    8px;
  --spacing-12:   12px;
  --spacing-16:   16px;
  --spacing-24:   24px;
  --spacing-32:   32px;
  --spacing-40:   40px;
  --spacing-48:   48px;
  --spacing-64:   64px;
  --spacing-80:   80px;
  --spacing-96:   96px;

  /* ─── Layout ─── */
  --section-gap:       48px;
  --card-padding:      24px;
  --element-gap:       8px;
  --sidebar-width:     240px;
  --content-max-width: 720px;
  --page-max-width:    1280px;

  /* ─── Border Radius ─── */
  --radius-pill:    100px;
  --radius-modal:   20px;
  --radius-card:    16px;
  --radius-button:  8px;
  --radius-input:   6px;
  --radius-image:   12px;
  --radius-tooltip: 6px;
  --radius-badge:   9999px;
  --radius-block:   8px;

  /* ─── Shadows ─── */
  --shadow-xs:          rgba(0,0,0,0.04) 0px 1px 3px;
  --shadow-sm:          rgba(205,208,223,0.4) 0px 2px 16px;
  --shadow-md:          rgba(205,208,223,0.4) 0px 2px 48px;
  --shadow-lg:          rgba(0,0,0,0.15) 0px 5px 45px;
  --shadow-xl:          rgba(0,0,0,0.4) 0px 5px 55px;
  --shadow-glow-violet: 0px 0px 24px rgba(97,97,255,0.35);
  --shadow-glow-frost:  0px 0px 20px rgba(98,174,240,0.3);
  --shadow-notion:      rgba(0,0,0,0.01) 0px 1px 3px, rgba(0,0,0,0.02) 0px 3px 7px, rgba(0,0,0,0.04) 0px 14px 28px;

  /* ─── Motion ─── */
  --duration-instant:    80ms;
  --duration-fast:       150ms;
  --duration-base:       250ms;
  --duration-slow:       400ms;
  --duration-deliberate: 600ms;
  --ease-out:    cubic-bezier(0.0, 0.0, 0.2, 1);
  --ease-in:     cubic-bezier(0.4, 0.0, 1, 1);
  --ease-spring: cubic-bezier(0.34, 1.56, 0.64, 1);
}
```

---

## 12. Tailwind v4 Theme

```css
@theme {
  /* Colors */
  --color-deep-space:        #02093a;
  --color-paper:             #ffffff;
  --color-fog:               #f5f6f8;
  --color-mist:              #f6f5f4;
  --color-graphite:          #333333;
  --color-slate:             #676879;
  --color-iron:              #535768;
  --color-silver:            #d0d4e4;
  --color-brand-violet:      #6161ff;
  --color-brand-indigo:      #455dd3;
  --color-brand-purple:      #9450fd;
  --color-brand-ocean:       #0075de;
  --color-brand-sky:         #3ac9ff;
  --color-brand-frost:       #62aef0;
  --color-success:           #1aae39;
  --color-warning:           #ffb110;
  --color-danger:            #f64932;
  --color-info:              #097fe8;
  --color-surface-mint:      #bcfe90;
  --color-surface-lavender:  #eddff7;
  --color-surface-sky:       #abf0ff;
  --color-surface-sunset:    #ff8940;
  --color-surface-pale-blue: #e7ecff;
  --color-surface-ocean:     #93beff;
  --color-surface-ice:       #d1faff;
  --color-surface-fuchsia:   #ff83dd;
  --color-surface-gold:      #ffc95e;
  --color-surface-grape:     #ad6ded;

  /* Fonts */
  --font-display:   'Poppins', ui-sans-serif, system-ui, sans-serif;
  --font-body:      'Inter', ui-sans-serif, system-ui, sans-serif;
  --font-editorial: 'Lyon Text', Georgia, serif;
  --font-mono:      'JetBrains Mono', 'Courier New', monospace;

  /* Spacing */
  --spacing-4:  4px;  --spacing-8:  8px;  --spacing-12: 12px;
  --spacing-16: 16px; --spacing-24: 24px; --spacing-32: 32px;
  --spacing-40: 40px; --spacing-48: 48px; --spacing-64: 64px;
  --spacing-80: 80px; --spacing-96: 96px;

  /* Border Radius */
  --radius-pill:    100px;
  --radius-modal:   20px;
  --radius-card:    16px;
  --radius-button:  8px;
  --radius-input:   6px;
  --radius-badge:   9999px;

  /* Shadows */
  --shadow-sm:          rgba(205,208,223,0.4) 0px 2px 16px;
  --shadow-md:          rgba(205,208,223,0.4) 0px 2px 48px;
  --shadow-lg:          rgba(0,0,0,0.15) 0px 5px 45px;
  --shadow-xl:          rgba(0,0,0,0.4) 0px 5px 55px;
  --shadow-glow-violet: 0px 0px 24px rgba(97,97,255,0.35);
}
```

---

## 13. Quick Reference

```
Primary CTA:     #6161ff bg · #ffffff text · 100px radius · Poppins 16px 500
Secondary btn:   transparent · #d0d4e4 border · #333333 text · 8px radius
Dark surface:    #02093a bg · #ffffff text · 16px radius card
Light card:      #ffffff bg · #d0d4e4 border · 16px radius · shadow-md
Document body:   Inter 16px 400 · #333333 · line-height 1.6
Page title:      Poppins 40px 700 · #333333 · tracking -0.02em
Badge:           surface palette bg · 9999px radius · Inter 12px 500
Section gap:     48px
Card padding:    24px
Element gap:     8px
```
