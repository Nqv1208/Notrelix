import { z } from "zod";

export const createCardSchema = z.object({
  listId: z.string().uuid(),
  title: z.string().min(1).max(500),
  position: z.number().optional(),
});

export type CreateCardInput = z.infer<typeof createCardSchema>;
