import { describe, it, expect } from "vitest";
import { readFileSync, readdirSync, statSync } from "node:fs";
import { join } from "node:path";
import { MockStore } from "../state/mock-store";
import { defaultConfig } from "../config/mock-config";

describe("MFB-FZ-05D Deterministic World Closure", () => {
  it("1. Two stores with same config produce identical initial snapshots", () => {
    const storeA = new MockStore({ ...defaultConfig, seed: 42 });
    const storeB = new MockStore({ ...defaultConfig, seed: 42 });

    expect(storeA.getSnapshot()).toEqual(storeB.getSnapshot());
    expect(storeA.getClock().isoNow()).toBe(storeB.getClock().isoNow());
  });

  it("2. Two stores with same config and identical mutation sequences produce identical IDs, timestamps, and state", () => {
    const storeA = new MockStore({ ...defaultConfig, seed: 42 });
    const storeB = new MockStore({ ...defaultConfig, seed: 42 });

    const wsA = storeA.createWorkspaceForCurrentUser({
      name: "Deterministic WS",
    });
    const wsB = storeB.createWorkspaceForCurrentUser({
      name: "Deterministic WS",
    });

    expect(wsA.workspace.id).toBe(wsB.workspace.id);
    expect(wsA.membership.id).toBe(wsB.membership.id);
    expect(wsA.membership.joinedAt).toBe(wsB.membership.joinedAt);

    const boardA = storeA.createBoard(wsA.workspace.id, { title: "Roadmap" });
    const boardB = storeB.createBoard(wsB.workspace.id, { title: "Roadmap" });

    expect(boardA.id).toBe(boardB.id);

    const listA = storeA.createList(boardA.id, { title: "To Do" });
    const listB = storeB.createList(boardB.id, { title: "To Do" });

    expect(listA.id).toBe(listB.id);

    const cardA = storeA.createCard(boardA.id, listA.id, { title: "Task 1" });
    const cardB = storeB.createCard(boardB.id, listB.id, { title: "Task 1" });

    expect(cardA.id).toBe(cardB.id);

    const pageA = storeA.createPage(wsA.workspace.id, { title: "Doc 1" });
    const pageB = storeB.createPage(wsB.workspace.id, { title: "Doc 1" });

    expect(pageA.id).toBe(pageB.id);

    expect(storeA.getSnapshot()).toEqual(storeB.getSnapshot());
  });

  it("3. Store reset with same config produces identical initial snapshot", () => {
    const store = new MockStore({ ...defaultConfig, seed: 42 });
    const initialSnapshot = store.getSnapshot();

    store.createWorkspaceForCurrentUser({ name: "Mutated" });
    expect(store.getSnapshot()).not.toEqual(initialSnapshot);

    store.seedBaseWorld();
    expect(store.getSnapshot()).toEqual(initialSnapshot);
  });

  it("4. Different seeds produce deterministic but distinct clock and factory outputs", () => {
    const store1 = new MockStore({ ...defaultConfig, seed: 1001 });
    const store2 = new MockStore({ ...defaultConfig, seed: 2002 });

    expect(store1.getClock().isoNow()).not.toBe(store2.getClock().isoNow());
  });

  it("5. Static/source guard: zero forbidden uncontrolled time/random calls in mock-backend source", () => {
    const srcDir = join(__dirname, "..");
    const forbiddenPatterns = [
      /\bDate\.now\s*\(/,
      /\bMath\.random\s*\(/,
      /\bcrypto\.randomUUID\s*\(/,
      /\brandomUUID\s*\(/,
    ];

    const violations: { file: string; line: number; match: string }[] = [];

    function scanDir(dir: string) {
      const entries = readdirSync(dir);
      for (const entry of entries) {
        const fullPath = join(dir, entry);
        const stat = statSync(fullPath);
        if (stat.isDirectory()) {
          if (entry !== "__tests__") {
            scanDir(fullPath);
          }
        } else if (fullPath.endsWith(".ts") && !fullPath.endsWith(".d.ts")) {
          const content = readFileSync(fullPath, "utf-8");
          const lines = content.split("\n");
          lines.forEach((line, index) => {
            const trimmed = line.trim();
            if (
              trimmed.startsWith("*") ||
              trimmed.startsWith("//") ||
              trimmed.startsWith("/*")
            ) {
              return;
            }
            // Check for new Date() outside clock.ts
            if (
              /\bnew\s+Date\s*\(/.test(line) &&
              !fullPath.endsWith("clock.ts")
            ) {
              violations.push({
                file: fullPath,
                line: index + 1,
                match: `Uncontrolled new Date(): ${trimmed}`,
              });
            }
            for (const pattern of forbiddenPatterns) {
              if (pattern.test(line)) {
                violations.push({
                  file: fullPath,
                  line: index + 1,
                  match: trimmed,
                });
              }
            }
          });
        }
      }
    }

    scanDir(srcDir);
    expect(violations).toEqual([]);
  });
});
