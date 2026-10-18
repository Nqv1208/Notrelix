# Hướng C · Tablet ngang (đợt C2) — quy ước chung

Read together with `review-decisions.md` (world facts, product rules, editing rules) and `mobile-brief.md` (principles section applies here too).

## Principles

- Tablet sits between desktop and phone: desktop information density where it helps (multi-column, split view), touch interaction everywhere (≥44×44 targets, no hover-only actions, swipe/drag always has a visible alternative). A hardware keyboard may be attached, so keyboard shortcuts (⌘K, J/K) keep working and are shown as hints only when useful.
- Same data, permissions, states and wording as the desktop Final screens; layout re-composed for 1180×820, never the desktop page scaled down.
- Detail opens as a split view (list/board on the left, item on the right, both interactive), not as a modal; the divider has a visible drag handle button (aria-label "Đổi độ rộng") and a "Mở toàn màn hình" button.

## Frame

- Artboard 1180×820 (iPad Air 11" landscape). File names `Final-T-<Name>.dc.html`.
- Root `.nx.t` = 1180×820, `overflow:hidden`, position relative.
- iPadOS status bar 24 px: left "10:40  Thứ Hai 12 thg 10", right wifi/battery glyphs (aria-hidden).
- No Notrelix system rail and no bottom tab bar. Workspace sidebar 248 px, pinned by default, collapsible to a 64 px icon column via a sidebar toggle button in the top bar (aria-label "Thu gọn thanh bên" / "Mở thanh bên", aria-expanded). Sidebar content = the canonical desktop sidebar (see Final-Space.dc.html) with touch-sized rows (44 px), workspace chip "MA Minh An Group ▾" at the top (opens a popover listing workspaces, replacing the system rail), and the user block + "Thiết lập workspace" at the bottom.
- Top bar 56 px: sidebar toggle, breadcrumb (CN chip · Triển khai ERP Q4), view tabs (Kanban · Bảng · Lịch · Dòng thời gian) as 44 px segmented control, right: search (⌘K) button, "+" Thêm công việc, Chia sẻ, ⋯.
- Popovers (pickers, menus) anchor to their trigger like iPadOS popovers (arrow optional), max 360 px wide; on tablet they are NOT bottom sheets.
- Home indicator 5 px bar at the bottom centre (aria-hidden).

## Tweaks

Every file: `state` (enum, screen-specific, first value default), `mode` (sang / dem), `sidebar` (mo / thu-gon).

## Canvas

New row "Hướng chốt · Tablet ngang (đợt C2)" title at y = -4400; boards at y = -4100, x = 0, 1260, 2520, 3780 (Board, Table, Calendar, Detail).
