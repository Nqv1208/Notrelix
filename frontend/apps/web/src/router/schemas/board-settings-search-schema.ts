import { z } from "zod";

/**
 * `section` is the settings section ("fields" is the default); `field` and
 * `view` name the field or the saved view whose panel is open; `from` is the
 * board view to go back to.
 */
export const boardSettingsSearchSchema = z.object({
  section: z
    .enum(["general", "fields", "views", "groups", "automations"])
    .default("fields"),
  field: z.string().optional(),
  /** The board view the page was opened from; "Về board" returns to it. */
  from: z.enum(["table", "kanban", "calendar", "timeline"]).optional(),
  view: z.string().optional(),
  /** The automation rule open in its editor (section "automations"). */
  rule: z.string().optional(),
  /** The run log of the automations is open. */
  log: z.boolean().optional(),
  /** The run open in the log's sheet. */
  run: z.string().optional(),
});

export type BoardSettingsSearch = z.infer<typeof boardSettingsSearchSchema>;
