import { z } from "zod";

export const envSchema = z.object({
  NEXT_PUBLIC_API_URL: z.string().url().optional(),
  NEXT_PUBLIC_WS_URL: z.string().url().optional(),
  VITE_API_URL: z.string().url().optional(),
  VITE_WS_URL: z.string().url().optional(),
  VITE_APP_URL: z.string().url().optional(),
  VITE_RELEASE_SHA: z.string().optional(),
  NODE_ENV: z.enum(["development", "production", "test"]).default("development"),
});

export type Env = z.infer<typeof envSchema>;

export interface ResolvedEnv {
  apiUrl: string;
  wsUrl: string;
  nodeEnv: "development" | "production" | "test";
  appUrl?: string;
  releaseSha?: string;
}

export function parseEnv(input: Partial<Record<string, string | undefined>>): ResolvedEnv {
  const parsedEnv = envSchema.safeParse(input);

  if (!parsedEnv.success) {
    console.error("[Kernel Env] Invalid environment variables:", parsedEnv.error.format());
    throw new Error("Invalid environment variables configuration");
  }

  const data = parsedEnv.data;
  const isProduction = data.NODE_ENV === "production";

  // Fail-fast in production if required URLs are missing
  if (isProduction) {
    const missingVars: string[] = [];
    if (!data.VITE_API_URL && !data.NEXT_PUBLIC_API_URL) missingVars.push("VITE_API_URL");
    if (!data.VITE_WS_URL && !data.NEXT_PUBLIC_WS_URL) missingVars.push("VITE_WS_URL");
    if (missingVars.length > 0) {
      throw new Error(
        `[Kernel Env] Missing required environment variables in production: ${missingVars.join(", ")}`
      );
    }
  }

  const apiUrl = data.VITE_API_URL ?? data.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";
  const wsUrl = data.VITE_WS_URL ?? data.NEXT_PUBLIC_WS_URL ?? "ws://localhost:5000/stream";

  return {
    apiUrl,
    wsUrl,
    nodeEnv: data.NODE_ENV,
    appUrl: data.VITE_APP_URL,
    releaseSha: data.VITE_RELEASE_SHA,
  };
}

// NOTE: Do NOT export a global `env = parseEnv()` singleton.
// Applications must call parseEnv(import.meta.env) explicitly via createAppRuntime().
export const envSchemaDefinition = envSchema;
