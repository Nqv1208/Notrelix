import { z } from "zod";

export const kanbanFilterSchema = z.object({
  searchQuery: z.string().optional(),
  priority: z.array(z.string()).optional(),
  status: z.array(z.string()).optional(),
  assigneeId: z.array(z.string()).optional(),
  labelId: z.array(z.string()).optional(),
});

export type KanbanFilterInput = z.infer<typeof kanbanFilterSchema>;
