import { describe, it, expect } from "vitest";
import { execSync } from "node:child_process";
import { resolve } from "node:path";
import { writeFileSync, unlinkSync, existsSync } from "node:fs";

describe("MFB-FZ-08: Production Artifact Isolation Gate", () => {
  const rootDir = resolve(__dirname, "../../../../../");
  const distDir = resolve(rootDir, "apps/web/dist");

  it("passes when dist contains no mock signatures", () => {
    if (!existsSync(distDir)) return;
    const output = execSync("node ./scripts/assert-no-mock-artifact.mjs", {
      cwd: rootDir,
      encoding: "utf-8",
    });
    expect(output).toContain("PASS");
  });

  it("fails when a mock signature is deliberately injected into dist", () => {
    if (!existsSync(distDir)) return;
    const testFilePath = resolve(distDir, "test-mock-leak.js");
    writeFileSync(testFilePath, 'const test = "mock-user-owner";', "utf-8");

    try {
      expect(() => {
        execSync("node ./scripts/assert-no-mock-artifact.mjs", {
          cwd: rootDir,
          encoding: "utf-8",
          stdio: "pipe",
        });
      }).toThrow();
    } finally {
      if (existsSync(testFilePath)) {
        unlinkSync(testFilePath);
      }
    }
  });
});
