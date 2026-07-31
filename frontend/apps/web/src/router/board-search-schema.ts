import { z } from 'zod';

export const boardSearchSchema = z.object({
  view: z.enum(['table', 'kanban', 'calendar', 'timeline']).default('kanban'),
  filter: z.string().optional(),
  sort: z.string().optional(),
  groupBy: z.string().optional(),
  item: z.string().optional(),
});

export type BoardSearch = z.infer<typeof boardSearchSchema>;
