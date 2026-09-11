import { describe, expect, it } from "vitest";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const repoRoot = resolve(__dirname, "../../../../../");
const workflowPath = resolve(repoRoot, ".github/workflows/frontend-ci.yml");

const FORBIDDEN_UI_LANE = [
  "test:integration:guarded",
  "test:mobile:guarded",
  "test:generators:guarded",
  "e2e:mock",
  "e2e:real",
  "mock:contract",
  "mock:freeze:check",
];

describe("frontend-ci ui-foundation lane", () => {
  const workflow = readFileSync(workflowPath, "utf8");

  it("keeps one ui-foundation job owning the UI contract (TST-114)", () => {
    const jobCount = (workflow.match(/^ {2}ui-foundation:/gm) ?? []).length;
    expect(jobCount).toBe(1);
    const block = workflow.split("  mock-contract:")[0]!;
    expect(block).toContain("ui-foundation:");
  });

  it("runs the planner-provided pinned renderer and the UI-only checks (TST-114)", () => {
    const block = extractJobBlock(workflow);
    expect(block).toMatch(/needs\.changes\.outputs\.renderer_ref/);
    expect(block).toMatch(/needs\.changes\.outputs\.renderer_version/);
    expect(block).toMatch(/check:ui-purity/);
    expect(block).toMatch(/check:ui-actions/);
    expect(block).toMatch(/check:ui-fixtures/);
    expect(block).toMatch(/check:ui-evidence/);
    expect(block).toMatch(/test:ui:freeze/);
    const fallback = workflow.match(
      /FALLBACK_RENDERER_REF: mcr\.microsoft\.com\/playwright:v[\d.]+-jammy@sha256:[0-9a-f]{64}/,
    );
    expect(fallback).not.toBeNull();
    const digestSources = (workflow.match(/FALLBACK_RENDERER_REF:/g) ?? [])
      .length;
    expect(digestSources).toBe(1);
  });

  it("has no application integration dependency in the UI lane (TST-115)", () => {
    const block = extractJobBlock(workflow);
    for (const forbidden of FORBIDDEN_UI_LANE) {
      expect(block).not.toContain(forbidden);
    }
    expect(block.toLowerCase()).not.toMatch(/docker/);
    expect(block.toLowerCase()).not.toMatch(/dotnet/);
    expect(block.toLowerCase()).not.toMatch(/\bbackend\b/);
  });
});

function extractJobBlock(workflow: string): string {
  const marker = "  ui-foundation:";
  const start = workflow.indexOf(marker);
  expect(start).toBeGreaterThanOrEqual(0);
  const tail = workflow.slice(start + marker.length);
  const nextJob = tail.search(/\n {2}[^ ]/);
  const end = nextJob > 0 ? start + marker.length + nextJob : workflow.length;
  return workflow.slice(start, end);
}
