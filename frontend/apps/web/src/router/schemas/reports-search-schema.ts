import { z } from "zod";

/**
 * `report` is the report open (none = the list). `edit` is the layout editor,
 * `add` the add-widget dialog (it opens inside the editor), `drill` the
 * workload row whose list is open ("none" = the unassigned row), `export` the
 * export dialog, `view=work` the read-only work report and `assignee` the person the numbers are narrowed to.
 */
export const reportsSearchSchema = z.object({
  report: z.string().optional(),
  edit: z.boolean().optional(),
  add: z.boolean().optional(),
  drill: z.string().optional(),
  export: z.boolean().optional(),
  assignee: z.string().optional(),
  view: z.literal("work").optional(),
  tab: z.enum(["mine", "shared", "all"]).optional(),
});

export type ReportsSearch = z.infer<typeof reportsSearchSchema>;
