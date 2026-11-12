import { describe, it, expect } from "vitest";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import { chatOperations } from "../contexts/chat";

/**
 * Every Chat operation of the generated REST schema has a mock handler with the
 * same operation id, method and route, and no handler names an operation the
 * schema does not have.
 */

const SCHEMA_PATH = join(
  __dirname,
  "../../../../foundation/contracts/src/generated/rest/schema.ts",
);

interface SchemaOperation {
  readonly operationId: string;
  readonly method: string;
  readonly route: string;
}

function schemaChatOperations(): SchemaOperation[] {
  const lines = readFileSync(SCHEMA_PATH, "utf8").split("\n");
  const found: SchemaOperation[] = [];
  let currentPath: string | null = null;
  for (const line of lines) {
    const pathMatch =
      /^ {4}"(\/api\/v1\/workspaces\/\{workspaceId\}\/chat[^"]*)": \{/.exec(
        line,
      );
    if (pathMatch) {
      currentPath = pathMatch[1]!;
      continue;
    }
    if (/^ {4}"[^"]+": \{/.test(line)) currentPath = null;
    if (currentPath === null) continue;
    const opMatch =
      /^ {8}(get|put|post|delete|patch): operations\["([^"]+)"\]/.exec(line);
    if (opMatch) {
      found.push({
        operationId: opMatch[2]!,
        method: opMatch[1]!.toUpperCase(),
        route: currentPath
          .replace(/^\/api\/v1/, "")
          .replace(/\{(\w+)\}/g, ":$1"),
      });
    }
  }
  return found;
}

describe("MFB-CHAT-S1.6: Chat mock handlers match the generated contract", () => {
  const schema = schemaChatOperations();
  const handlerKeys = chatOperations.map((op) => ({
    operationId: (op.contract as { operationId: string }).operationId,
    method: op.method,
    route: op.route,
  }));

  it("finds the Chat operations in the generated schema", () => {
    expect(schema.length).toBe(48);
  });

  it("has a handler for every Chat operation of the schema, with its method and route", () => {
    const handled = new Set(handlerKeys.map((h) => `${h.method} ${h.route}`));
    const missing = schema.filter(
      (s) => !handled.has(`${s.method} ${s.route}`),
    );
    expect(missing).toEqual([]);
  });

  it("names no handler operation the schema does not have", () => {
    const declared = new Set(schema.map((s) => s.operationId));
    const unknown = handlerKeys.filter((h) => !declared.has(h.operationId));
    expect(unknown).toEqual([]);
  });

  it("has exactly one handler per operation id", () => {
    const ids = handlerKeys.map((h) => h.operationId);
    expect(new Set(ids).size).toBe(ids.length);
    expect(ids.length).toBe(schema.length);
  });
});
