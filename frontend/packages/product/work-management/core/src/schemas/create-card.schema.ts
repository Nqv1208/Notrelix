import { z } from "zod";
import { ITEM_TITLE_MAX_LENGTH } from "../utils/item-title";

export const createCardSchema = z.object({
  listId: z.string().uuid(),
  title: z.string().min(1).max(ITEM_TITLE_MAX_LENGTH),
  position: z.number().optional(),
  /** The item's own facts, saved with the create. */
  descriptionMd: z.string().max(20000).optional(),
  startDate: z.string().datetime().optional(),
  dueDate: z.string().datetime().optional(),
});

export type CreateCardInput = z.infer<typeof createCardSchema>;
