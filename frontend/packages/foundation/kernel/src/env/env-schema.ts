import { z } from "zod";

export const envSchema = z.object({
  NEXT_PUBLIC_API_URL: z.string().url().optional(),
  NEXT_PUBLIC_WS_URL: z.string().url().optional(),
  VITE_API_URL: z.string().url().optional(),
  VITE_WS_URL: z.string().url().optional(),
  NODE_ENV: z.enum(["development", "production", "test"]).default("development"),
});

export type Env = z.infer<typeof envSchema>;

export interface ResolvedEnv {
  apiUrl: string;
  wsUrl: string;
  nodeEnv: "development" | "production" | "test";
}

export function parseEnv(input: Partial<Record<string, string | undefined>> = {}): ResolvedEnv {
  const parsedEnv = envSchema.safeParse(input);

  if (!parsedEnv.success) {
    console.error("[Kernel Env] Invalid environment variables:", parsedEnv.error.format());
    throw new Error("Invalid environment variables configuration");
  }

  const data = parsedEnv.data;
  const apiUrl = data.VITE_API_URL || data.NEXT_PUBLIC_API_URL || "http://localhost:5000";
  const wsUrl = data.VITE_WS_URL || data.NEXT_PUBLIC_WS_URL || "ws://localhost:5000/stream";

  if (data.NODE_ENV === "production" && (!data.VITE_API_URL && !data.NEXT_PUBLIC_API_URL)) {
    throw new Error("[Kernel Env] Missing API URL environment configuration in production");
  }

  return {
    apiUrl,
    wsUrl,
    nodeEnv: data.NODE_ENV,
  };
}

export const envSchemaDefinition = envSchema;
export const env = parseEnv();
