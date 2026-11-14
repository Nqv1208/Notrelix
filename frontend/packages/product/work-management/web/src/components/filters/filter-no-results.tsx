import { ListFilter } from "lucide-react";
import { fillFiltersCopy, type FiltersCopy } from "./filters.copy";

/**
 * The board has items but the filter keeps none (`WM-Filter-States`, "Đang lọc mà không có kết quả"): what is
 * applied, and the two ways out, "Sửa điều kiện" (opens the filter) and "Xoá bộ lọc".
 */
export function FilterNoResults({
  copy,
  conditions,
  total,
  onEdit,
  onClear,
}: {
  copy: FiltersCopy;
  /** Conditions the filter applies. */
  conditions: number;
  /** Items the filter was applied to. */
  total: number;
  onEdit: () => void;
  onClear: () => void;
}) {
  const t = copy.states;
  return (
    <div
      role="status"
      data-filter-no-results=""
      className="mx-auto flex max-w-[436px] flex-col items-center gap-1.5 rounded-xl border border-line bg-popover px-5 py-8 text-center dark:bg-card"
    >
      {/* The drawn icon sits on a text line (22.3 px), 1 px below its top. */}
      <span
        aria-hidden="true"
        className="h-[22.3px] pt-px text-meta-foreground"
      >
        <ListFilter className="size-4" strokeWidth={1.8} />
      </span>
      <b className="text-sm leading-[1.45] font-bold text-foreground">
        {t.noResults}
      </b>
      <span className="text-crumb leading-[1.45] text-meta-foreground">
        {fillFiltersCopy(t.noResultsHint, { count: conditions, total })}
      </span>
      <div className="mt-2 flex gap-2">
        <button type="button" onClick={onEdit} className={OUTLINE}>
          {t.editConditions}
        </button>
        <button type="button" onClick={onClear} className={PRIMARY}>
          {t.clear}
        </button>
      </div>
    </div>
  );
}

const OUTLINE =
  "box-border inline-flex h-7 cursor-pointer items-center rounded-[7px] border border-line-strong bg-popover px-2.5 text-crumb leading-[1.45] font-medium text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring dark:bg-card";
const PRIMARY =
  "box-border inline-flex h-7 cursor-pointer items-center rounded-[7px] border border-primary bg-primary px-2.5 text-crumb leading-[1.45] font-semibold text-primary-foreground outline-none hover:bg-primary/90 focus-visible:ring-2 focus-visible:ring-focus-ring";
