import type { ReactNode } from "react";

export function TableStickyHeader({ children }: { children: ReactNode }) {
  return (
    <div
      data-slot="sticky-header"
      className="sticky top-0 z-30 overflow-hidden border-b border-l-[6px] border-border bg-card rounded-t-sm"
    >
      {children}
    </div>
  );
}
