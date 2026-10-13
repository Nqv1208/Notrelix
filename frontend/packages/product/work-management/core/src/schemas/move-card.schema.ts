import { z } from "zod";

export const moveCardSchema = z.object({
  cardId: z.string().uuid(),
  listId: z.string().uuid(),
  /** Local order position, for the optimistic placement. */
  position: z.number(),
  /**
   * The new neighbours in the group's order (`null` = none on that side).
   * When both are omitted they are worked out from `position`.
   */
  previousSiblingId: z.string().nullable().optional(),
  nextSiblingId: z.string().nullable().optional(),
  /** The item version the move is based on; read from the board when omitted. */
  expectedVersion: z.number().int().positive().optional(),
});

export type MoveCardInput = z.infer<typeof moveCardSchema>;
