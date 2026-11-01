import { z } from "zod";

export const createCardUpdateSchema = z.object({
  cardId: z.string().min(1),
  body: z.string().trim().min(1).max(5000),
  mentionUserIds: z.array(z.string().min(1)).default([]),
  attachmentIds: z.array(z.string().min(1)).default([]),
  /** Answers this comment; a reply to a reply lands on the top-level comment. */
  parentCommentId: z.string().min(1).optional(),
});

export type CreateCardUpdateInput = z.infer<typeof createCardUpdateSchema>;
