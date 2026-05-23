import { z } from "zod"

export const cardFileSchema = z.object({
  id: z.string().min(1),
  cardId: z.string().min(1),
  name: z.string().min(1).max(255),
  size: z.number().int().nonnegative(),
  contentType: z.string().min(1),
  url: z.string().url(),
  source: z.enum(["upload", "r2", "s3", "link"]),
  createdAt: z.string().datetime(),
})

export const uploadCardFileSchema = z.object({
  cardId: z.string().min(1),
  name: z.string().min(1).max(255),
  size: z.number().int().nonnegative(),
  contentType: z.string().min(1),
})

export type CardFileInput = z.infer<typeof cardFileSchema>
export type UploadCardFileInput = z.infer<typeof uploadCardFileSchema>
