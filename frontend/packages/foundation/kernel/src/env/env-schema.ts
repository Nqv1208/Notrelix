import { z } from "zod"

// Validates and exposes environment variables to ensure compile-time security.
const envSchema = z.object({
  NEXT_PUBLIC_API_URL: z.string().url().default("http://localhost:5000"),
  NEXT_PUBLIC_WS_URL: z.string().url().default("ws://localhost:5000/stream"),
  NODE_ENV: z.enum(["development", "production", "test"]).default("development"),
})

export type Env = z.infer<typeof envSchema>

export function parseEnv(input: Partial<Record<keyof Env, string | undefined>> = {}): Env {
  const parsedEnv = envSchema.safeParse(input)

  if (!parsedEnv.success) {
    console.error("Invalid environment variables:", parsedEnv.error.format())
    throw new Error("Invalid environment variables configuration")
  }

  return parsedEnv.data
}

export const envSchemaDefinition = envSchema
export const env = parseEnv()
