# Hướng C · Di động (đợt C1) — quy ước chung

Read together with `review-decisions.md` (world facts, product rules, editing rules — all still apply).

## Principles (DESIGN.md §40.3, §41, §47, §75.10)

- Semantic parity, not layout parity: same data, permissions, states and actions as the desktop Final screens, redesigned for touch. A desktop table becomes a list → detail flow. Never shrink desktop toolbars.
- Touch targets ≥ 44×44 px. No hover-only actions. Every swipe action also reachable from a visible ⋯ button or long-press menu (show the menu in a state where useful).
- Safe areas: status bar 47 px on top, home indicator 34 px at the bottom.
- Platform navigation: bottom tab bar + push navigation (back chevron + title), bottom sheets for pickers/menus/filters/quick add, full-screen for detail.
- Text uses the desktop scale shifted up for touch: body 15 px, secondary 13 px, large title 28 px/700. Same fonts (Be Vietnam Pro + IBM Plex Mono), same colour tokens and dark mode (`.nx.mode-dem`) as the desktop Final files.

## Frame

- Artboard 390×844 (iPhone 14/15). File names `Final-M-<Name>.dc.html`.
- Root `.nx.m` = 390×844, `overflow:hidden`, position relative, rounded 0 (the canvas draws the device).
- Status bar (47 px): left "10:40", right signal/wifi/battery glyphs (aria-hidden).
- Content area scrolls between the top bar and the tab bar (mockup: content may be clipped at the bottom; show a natural cut).
- Bottom tab bar: height 49 + 34 safe area, top hairline border, 5 tabs, icon 24 + label 11 px, active = accent colour + `aria-current="page"`:
  1. Trang chủ → Final-M-Home.dc.html
  2. Việc của tôi (badge 7) → `#`
  3. Hộp thư (badge 3) → Final-M-Inbox.dc.html
  4. Phòng ban → `#` (built in C3)
  5. Tìm → Final-M-Search.dc.html
  Board and Detail screens are pushed from a tab, so the tab bar stays visible on Board (active tab = Phòng ban, since the board lives under a department) and is hidden on full-screen Detail.
- Workspace switching (the desktop system rail): the workspace chip "MA ▾" at the top-left of Trang chủ opens a bottom sheet listing Minh An Group (current), Minh An Đà Nẵng (2 chưa đọc), Tân Lộc Logistics (khách), plus "Hộp thư mọi workspace" and "Cài đặt tài khoản".
- Bottom sheet: grabber 36×5, radius 14 top corners, scrim behind, `role="dialog" aria-modal="true" aria-labelledby`, primary action at the bottom inside the safe area.
- FAB is NOT used; "Thêm" lives in the top bar (+ button, aria-label) to avoid covering list content.

## Tweaks

Every file: `state` (enum, screen-specific, first value default) and `mode` (sang / dem). No `nav` tweak (no system rail on mobile).

## Canvas

Boards go in a new row above the desktop row: y = -2900, x = 0, 470, 940, 1410, 1880 (Home, Inbox, Board, Detail, Search). Title note "Hướng chốt · Di động" at y = -3200.
