#!/usr/bin/env node
import { existsSync, readFileSync, writeFileSync, mkdirSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, "../../../../");

const specPath = resolve(repoRoot, "artifacts/contracts/realtime.v1.json");
const outputDir = resolve(
  repoRoot,
  "frontend/packages/foundation/contracts/src/generated/realtime",
);
const outputPath = resolve(outputDir, "messages.ts");
const indexOutputPath = resolve(outputDir, "index.ts");

if (!existsSync(specPath)) {
  console.error(
    "Required Realtime AsyncAPI spec missing: artifacts/contracts/realtime.v1.json",
  );
  process.exit(1);
}

function stableKeys(value) {
  return Object.keys(value ?? {}).sort();
}

function sanitizeTypeName(value) {
  return String(value)
    .replace(/[^a-zA-Z0-9_]/g, "_")
    .replace(/^([0-9])/, "_$1");
}

function schemaToTs(schema, indent = 0) {
  if (!schema) return "unknown";
  if (schema.$ref) return sanitizeTypeName(schema.$ref.split("/").at(-1));
  if (schema.enum) return schema.enum.map((v) => JSON.stringify(v)).join(" | ");
  if (schema.oneOf)
    return schema.oneOf.map((s) => schemaToTs(s, indent)).join(" | ");

  switch (schema.type) {
    case "string":
      return "string";
    case "integer":
    case "number":
      return "number";
    case "boolean":
      return "boolean";
    case "array":
      return `ReadonlyArray<${schemaToTs(schema.items, indent)}>`;
    case "object":
    default: {
      const properties = schema.properties ?? {};
      const keys = stableKeys(properties);
      if (keys.length === 0) return "Record<string, unknown>";
      const pad = " ".repeat(indent);
      const childPad = " ".repeat(indent + 2);
      const required = new Set(schema.required ?? []);
      const lines = ["{"];
      for (const key of keys) {
        const optional = required.has(key) ? "" : "?";
        lines.push(
          `${childPad}${JSON.stringify(key)}${optional}: ${schemaToTs(properties[key], indent + 2)};`,
        );
      }
      lines.push(`${pad}}`);
      return lines.join("\n");
    }
  }
}

try {
  const rawSpec = readFileSync(specPath, "utf8");
  const spec = JSON.parse(rawSpec);
  const messages = spec.components?.messages ?? {};
  const unionMembers = [];
  const lines = [
    "/**",
    " * Generated from artifacts/contracts/realtime.v1.json",
    " * DO NOT EDIT.",
    " */",
    "",
  ];

  for (const messageKey of stableKeys(messages)) {
    const message = messages[messageKey];
    const eventType = message.name;
    if (!eventType) {
      throw new Error(`Missing message name for ${messageKey}`);
    }

    const payloadName = `${sanitizeTypeName(messageKey)}Payload`;
    lines.push(
      `export interface ${payloadName} ${schemaToTs(message.payload, 0)}`,
    );
    lines.push("");
    unionMembers.push(
      `  | { eventType: ${JSON.stringify(eventType)}; payload: ${payloadName} }`,
    );
  }

  lines.push("export type GeneratedRealtimeMessage =");
  lines.push(
    unionMembers.length > 0 ? unionMembers.join("\n") + ";" : "  never;",
  );
  lines.push("");
  lines.push("export type RealtimeEventMessage = GeneratedRealtimeMessage;");
  lines.push("");

  const indexContent = "export * from './messages';\n";

  mkdirSync(outputDir, { recursive: true });
  writeFileSync(outputPath, lines.join("\n"), "utf8");
  writeFileSync(indexOutputPath, indexContent, "utf8");

  console.log(
    "Generated realtime messages from artifacts/contracts/realtime.v1.json",
  );
} catch (err) {
  console.error("AsyncAPI realtime generation failed:", err);
  process.exit(1);
}
