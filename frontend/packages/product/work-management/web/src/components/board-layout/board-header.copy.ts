/**
 * Board header and view bar copy, vi and en, registered as the `board-header`
 * catalog (`board-header.catalog.ts`). The pure surfaces take a `copy` prop;
 * `useBoardHeaderCopy` resolves it for the active locale.
 */
export const en = {
  /** Accessible name of the view tab list. */
  boardViews: "Board views",
  views: {
    kanban: "Board",
    table: "Table",
    calendar: "Calendar",
    timeline: "Timeline",
  },
  addView: "Add view",
  filter: "Filter board",
  search: "Search...",
  searchLabel: "Search board",
};

export type BoardHeaderCopy = typeof en;

export const vi: BoardHeaderCopy = {
  boardViews: "Chế độ xem bảng",
  views: {
    kanban: "Kanban",
    table: "Bảng",
    calendar: "Lịch",
    timeline: "Dòng thời gian",
  },
  addView: "Thêm chế độ xem",
  filter: "Lọc bảng",
  search: "Tìm kiếm...",
  searchLabel: "Tìm kiếm trong bảng",
};

export function boardHeaderCopy(locale: "vi" | "en"): BoardHeaderCopy {
  return locale === "vi" ? vi : en;
}
