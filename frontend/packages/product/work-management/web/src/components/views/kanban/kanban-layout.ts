/**
 * Shared Kanban column width. The value is the `--layout-kanban-column` token
 * emitted by `@notrelix/ui-tokens` (spacing.ts, `layout`); the column, the
 * add-column slot and the loading skeleton must all use this one class so the
 * board does not jump when data arrives.
 */
export const kanbanColumnWidthClass = "w-(--layout-kanban-column)";

/** The tablet lane is 330 px wide (Final-T-Board); it goes beside the token class on the lane, its add slot and the skeleton. */
export const kanbanColumnTabletWidthClass = "tablet:w-82.5";
