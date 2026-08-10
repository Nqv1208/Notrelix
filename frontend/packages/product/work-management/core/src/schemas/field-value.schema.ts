import { z } from "zod";

export const updateFieldValueSchema = z.object({
  cardId: z.string().uuid(),
  fieldDefinitionId: z.string().uuid(),
  value: z.unknown(),
});

export type UpdateFieldValueInput = z.infer<typeof updateFieldValueSchema>;
