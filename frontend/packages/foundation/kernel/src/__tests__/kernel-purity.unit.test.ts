import { describe, expect, it } from "vitest";
import { readFileSync, readdirSync, statSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import {
  AppError,
  parseEnv,
  generateCorrelationId,
  invariant,
  assertNonNull,
} from "../index";

function collectProductionSources(): string[] {
  const srcDir = join(dirname(fileURLToPath(import.meta.url)), "..");
  const files: string[] = [];

  function walk(dir: string): void {
    for (const entry of readdirSync(dir)) {
      if (entry === "__tests__") continue;
      const full = join(dir, entry);
      const stat = statSync(full);
      if (stat.isDirectory()) {
        walk(full);
      } else if (full.endsWith(".ts") || full.endsWith(".tsx")) {
        files.push(full);
      }
    }
  }

  walk(srcDir);
  return files;
}

describe("kernel purity", () => {
  it("FND-010 kernel core utilities work in a Node environment without window", async () => {
    expect(typeof globalThis.window).toBe("undefined");

    const error = new AppError({ kind: "network", message: "offline" });
    expect(error instanceof Error).toBe(true);
    expect(error.kind).toBe("network");

    const env = parseEnv({
      mode: "development",
      apiUrl: "https://api.example.com",
      realtimeUrl: "wss://ws.example.com",
    });
    expect(env.mode).toBe("development");
    expect(env.apiUrl).toBe("https://api.example.com");

    const id = generateCorrelationId();
    expect(id).toMatch(/^[0-9a-f-]{36}$/);

    expect(() => invariant(false, "boom")).toThrow("boom");
    expect(assertNonNull("value", "not null")).toBe("value");
  });

  it("FND-010 parseEnv stays deterministic and fails fast in production", () => {
    const dev = parseEnv({ mode: "development" });
    expect(dev.apiUrl).toBe("http://localhost:5000");
    expect(dev.realtimeUrl).toBe("ws://localhost:5000/realtime");
    expect(dev.isProduction).toBe(false);

    expect(() =>
      parseEnv({ mode: "production", apiUrl: "https://api.example.com" }),
    ).toThrow(/[Kernel Env]/);
  });

  it("FND-011 kernel root exposes only pure utilities and no framework hooks", async () => {
    const kernel = await import("../index");

    const keys = Object.keys(kernel).sort();

    expect(keys).toEqual(
      expect.arrayContaining([
        "AppError",
        "assertNonNull",
        "envSchema",
        "envSchemaDefinition",
        "errorMap",
        "generateCorrelationId",
        "getErrorMessage",
        "getFormErrorMessage",
        "getUserFacingErrorMessage",
        "invariant",
        "isAppError",
        "mapStatusToKind",
        "parseEnv",
      ]),
    );

    expect("useQuery" in kernel).toBe(false);
    expect("useNavigate" in kernel).toBe(false);
    expect("createClient" in kernel).toBe(false);
    expect("RealtimeClient" in kernel).toBe(false);
  });

  it("FND-011 kernel production sources contain no framework, browser, or ambient reads", () => {
    const sources = collectProductionSources();
    expect(sources.length).toBeGreaterThan(0);

    for (const file of sources) {
      const content = readFileSync(file, "utf8");

      expect(content, file).not.toMatch(/from ['"]react['"]/);
      expect(content, file).not.toMatch(/from ['"]@notrelix\//);
      expect(content, file).not.toMatch(/window\./);
      expect(content, file).not.toMatch(/document\./);
      expect(content, file).not.toMatch(/import\.meta\.env/);
      expect(content, file).not.toMatch(/process\.env/);
      expect(content, file).not.toMatch(/Date\.now\(\)/);
      expect(content, file).not.toMatch(/new Date\(\)/);
      expect(content, file).not.toMatch(/console\.(log|warn|error|info|debug)/);

      if (file.endsWith("correlation-id.ts")) {
        expect(content, file).toMatch(/Math\.random/);
      } else {
        expect(content, file).not.toMatch(/Math\.random/);
      }
    }
  });

  it("FND-011 correlation id prefers crypto.randomUUID over the fallback", () => {
    expect(generateCorrelationId()).toMatch(/^[0-9a-f-]{36}$/);
    const src = readFileSync(
      join(
        dirname(fileURLToPath(import.meta.url)),
        "..",
        "ids",
        "correlation-id.ts",
      ),
      "utf8",
    );
    expect(src).toMatch(/crypto\.randomUUID/);
  });
});
