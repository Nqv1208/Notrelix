import { z } from "zod"

export const moveCardSchema = z.object({
  cardId: z.string().uuid(),
  listId: z.string().uuid(),
  position: z.number(),
})

export type MoveCardInput = z.infer<typeof moveCardSchema>
