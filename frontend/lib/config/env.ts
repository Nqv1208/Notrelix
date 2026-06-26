import { z } from "zod"

// Validates and exposes environment variables to ensure compile-time security.
const envSchema = z.object({
  NEXT_PUBLIC_API_URL: z.string().url().default("http://localhost:5000"),
  NEXT_PUBLIC_WS_URL: z.string().url().default("ws://localhost:5000/stream"),
  NODE_ENV: z.enum(["development", "production", "test"]).default("development"),
})

const parsedEnv = envSchema.safeParse({
  NEXT_PUBLIC_API_URL: process.env.NEXT_PUBLIC_API_URL,
  NEXT_PUBLIC_WS_URL: process.env.NEXT_PUBLIC_WS_URL,
  NODE_ENV: process.env.NODE_ENV,
})

if (!parsedEnv.success) {
  console.error("Invalid environment variables:", parsedEnv.error.format())
  throw new Error("Invalid environment variables configuration")
}

export const env = parsedEnv.data
