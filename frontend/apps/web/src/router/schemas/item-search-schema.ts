import { z } from "zod";

/** `tab` is the activity filter of the item page; "all" is the default. */
export const itemSearchSchema = z.object({
  tab: z.enum(["all", "comments", "history"]).default("all"),
});

export type ItemSearch = z.infer<typeof itemSearchSchema>;
