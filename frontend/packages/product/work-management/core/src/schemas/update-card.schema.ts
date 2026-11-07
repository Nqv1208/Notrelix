import { z } from "zod";
import { ITEM_TITLE_MAX_LENGTH } from "../utils/item-title";

export const updateCardSchema = z.object({
  title: z.string().min(1).max(ITEM_TITLE_MAX_LENGTH).optional(),
  descriptionMd: z.string().optional(),
  priority: z.enum(["urgent", "high", "medium", "low"]).nullable().optional(),
  dueDate: z.string().datetime().nullable().optional(),
  startDate: z.string().datetime().nullable().optional(),
});

export type UpdateCardInput = z.infer<typeof updateCardSchema>;
