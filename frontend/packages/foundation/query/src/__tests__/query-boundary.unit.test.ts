import { describe, expect, it } from "vitest";
import { readdirSync, readFileSync, statSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

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

describe("FND-035 @notrelix/query domain-key boundary", () => {
  it("exposes only generic client factories and the optimistic engine", async () => {
    const query = await import("../index");

    const keys = Object.keys(query).sort();

    expect(keys).toEqual(
      expect.arrayContaining([
        "createQueryClient",
        "defineOptimisticUpdate",
        "executeOptimisticCommand",
      ]),
    );

    expect("wmQueryKeys" in query).toBe(false);
    expect("workspaceQueryKeys" in query).toBe(false);
    expect("queryKeys" in query).toBe(false);
  });

  it("production sources contain no domain query key declarations", () => {
    const sources = collectProductionSources();
    expect(sources.length).toBeGreaterThan(0);

    for (const file of sources) {
      const content = readFileSync(file, "utf8");
      expect(content, file).not.toMatch(
        /wmQueryKeys|workspaceQueryKeys|queryKeys/,
      );
    }
  });
});
