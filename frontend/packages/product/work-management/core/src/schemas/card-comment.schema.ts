import { z } from "zod";

export const cardCommentSchema = z.object({
  body: z.string().min(1, "Comment content cannot be empty"),
  mentionUserIds: z.array(z.string().uuid()).default([]),
  attachmentIds: z.array(z.string().uuid()).default([]),
});

export type CardCommentInput = z.infer<typeof cardCommentSchema>;
