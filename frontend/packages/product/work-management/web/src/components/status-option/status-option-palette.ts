import type { WorkStatusCategory } from "@notrelix/work-management-core";

/**
 * The categories a user can give a status. "Review" exists on the board read
 * (older boards carry it) but is not offered when naming a new status.
 */
export const STATUS_OPTION_CATEGORIES = [
  "todo",
  "in_progress",
  "done",
] as const satisfies readonly WorkStatusCategory[];

export type StatusOptionCategory = (typeof STATUS_OPTION_CATEGORIES)[number];

/** The category a new status starts with. */
export const DEFAULT_STATUS_OPTION_CATEGORY: StatusOptionCategory =
  "in_progress";

/**
 * One colour a status can take. `value` is what the board stores on the
 * option; the values are the work-status and label tones the design tokens
 * already define (`--work-status-*`, `--label-*`), so a status colour is
 * never a free-form hex.
 */
export interface StatusOptionColor {
  readonly id: StatusOptionColorId;
  readonly value: string;
}

export type StatusOptionColorId =
  "slate" | "blue" | "amber" | "green" | "red" | "orange" | "teal" | "ink";

export const STATUS_OPTION_COLORS: readonly StatusOptionColor[] = [
  { id: "slate", value: "#7A8391" },
  { id: "blue", value: "#1E7BE0" },
  { id: "amber", value: "#C18A12" },
  { id: "green", value: "#2E8B57" },
  { id: "red", value: "#C8143F" },
  { id: "orange", value: "#B4500B" },
  { id: "teal", value: "#2C5E4F" },
  { id: "ink", value: "#3E4757" },
];

/** The colour a new status starts with: the one the Status field suggests per category. */
export const DEFAULT_STATUS_OPTION_COLOR_BY_CATEGORY: Readonly<
  Record<StatusOptionCategory, string>
> = {
  todo: "#7A8391",
  in_progress: "#1E7BE0",
  done: "#2E8B57",
};

/** The palette entry whose stored value is `value` (case-insensitive). */
export function statusOptionColorFor(
  value: string | null | undefined,
): StatusOptionColor | undefined {
  const wanted = value?.trim().toLowerCase();
  return STATUS_OPTION_COLORS.find(
    (color) => color.value.toLowerCase() === wanted,
  );
}
