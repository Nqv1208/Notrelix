import { createContext, useContext } from "react";
import { useIsMobile, useIsTablet } from "@notrelix/ui-web";

/**
 * The three drawings of the Board table: the 1440 grid (desktop, 40 px rows), `Final-T-Table` (the tablet band
 * 768 to 1279 px, 52 px rows with the title and its key in one pinned column) and the card list of
 * `Final-M-Board` (phone). The data and the writes are the same; only the drawing changes.
 */
export type TableLayout = "desktop" | "tablet" | "phone";

const TableLayoutContext = createContext<TableLayout>("desktop");

export const TableLayoutProvider = TableLayoutContext.Provider;

/** The layout the surface draws in; desktop outside a surface. */
export function useTableLayout(): TableLayout {
  return useContext(TableLayoutContext);
}

/** The layout of the current viewport width. */
export function useViewportTableLayout(): TableLayout {
  const phone = useIsMobile();
  const tablet = useIsTablet();
  return phone ? "phone" : tablet ? "tablet" : "desktop";
}
