import { z } from "zod";

export const boardSearchSchema = z.object({
  // An unknown layout in the address falls back to the default instead of failing the page.
  view: z
    .enum(["table", "kanban", "calendar", "timeline"])
    .default("kanban")
    .catch("kanban"),
  /** The saved view picked in the tabs; absent: the first one of `view`. */
  savedView: z.string().optional(),
  filter: z.string().optional(),
  sort: z.string().optional(),
  groupBy: z.string().optional(),
  item: z.string().optional(),
  /** The share dialog is open (the settings page's "Quyền truy cập" lands here). */
  share: z.boolean().optional(),
  /** The layout name the address gave when it is not one (`?view=gantt`): the page shows the default and one notice. */
  unknownView: z.string().optional(),
});

export type BoardSearch = z.infer<typeof boardSearchSchema>;

/**
 * The board route's search: the schema, plus the name of a layout the address gave that does not exist (the
 * schema alone falls back to the default and forgets what was asked).
 */
export function parseBoardSearch(search: Record<string, unknown>): BoardSearch {
  const { unknownView: _ignored, ...rest } = search;
  const parsed = boardSearchSchema.parse(rest);
  const raw = rest["view"];
  if (raw === undefined || raw === null || raw === "") return parsed;
  const known = boardSearchSchema.shape.view.removeCatch().safeParse(raw);
  return known.success ? parsed : { ...parsed, unknownView: String(raw) };
}
