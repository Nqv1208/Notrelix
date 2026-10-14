import { z } from "zod";

/** `item` is the item open in the side sheet (the board route's parameter). */
export const myWorkSearchSchema = z.object({
  item: z.string().optional(),
});

export type MyWorkSearch = z.infer<typeof myWorkSearchSchema>;
