import { z } from "zod";

export const searchSearchSchema = z.object({
  q: z.string().optional(),
  types: z.string().optional(),
});
