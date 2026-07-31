#!/usr/bin/env node
import { existsSync, readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '../../../../');

const specPath = resolve(repoRoot, 'artifacts/contracts/openapi.v1.json');
const outputDir = resolve(repoRoot, 'frontend/packages/foundation/contracts/src/generated/rest');
const outputPath = resolve(outputDir, 'schema.ts');
const indexOutputPath = resolve(outputDir, 'index.ts');

const HTTP_METHODS = ['get', 'post', 'put', 'patch', 'delete', 'options', 'head'];

if (!existsSync(specPath)) {
  console.error('Required OpenAPI spec missing: artifacts/contracts/openapi.v1.json');
  process.exit(1);
}

function stableKeys(value) {
  return Object.keys(value ?? {}).sort();
}

function sanitizeTypeName(value) {
  return String(value)
    .replace(/[^a-zA-Z0-9_]/g, '_')
    .replace(/^([0-9])/, '_$1');
}

function refName(ref) {
  return sanitizeTypeName(ref.split('/').at(-1));
}

function schemaToTs(schema, indent = 0, required = []) {
  if (!schema) return 'unknown';
  if (schema.$ref) return refName(schema.$ref);
  if (schema.enum) return schema.enum.map((v) => JSON.stringify(v)).join(' | ');
  if (schema.oneOf) return schema.oneOf.map((s) => schemaToTs(s, indent, required)).join(' | ');
  if (schema.anyOf) return schema.anyOf.map((s) => schemaToTs(s, indent, required)).join(' | ');
  if (schema.allOf) return schema.allOf.map((s) => schemaToTs(s, indent, required)).join(' & ');

  switch (schema.type) {
    case 'string':
      return 'string';
    case 'integer':
    case 'number':
      return 'number';
    case 'boolean':
      return 'boolean';
    case 'array':
      return `ReadonlyArray<${schemaToTs(schema.items, indent, required)}>`;
    case 'object':
    default: {
      const properties = schema.properties ?? {};
      const keys = stableKeys(properties);
      if (keys.length === 0) {
        return schema.additionalProperties ? 'Record<string, unknown>' : 'Record<string, never>';
      }
      const pad = ' '.repeat(indent);
      const childPad = ' '.repeat(indent + 2);
      const requiredSet = new Set(schema.required ?? required);
      const lines = ['{'];
      for (const key of keys) {
        const optional = requiredSet.has(key) ? '' : '?';
        lines.push(`${childPad}${JSON.stringify(key)}${optional}: ${schemaToTs(properties[key], indent + 2)};`);
      }
      lines.push(`${pad}}`);
      return lines.join('\n');
    }
  }
}

function parametersToTs(parameters = [], indent = 0) {
  const grouped = new Map();
  for (const parameter of parameters) {
    const location = parameter.in;
    if (!grouped.has(location)) grouped.set(location, []);
    grouped.get(location).push(parameter);
  }

  if (grouped.size === 0) return undefined;

  const pad = ' '.repeat(indent);
  const childPad = ' '.repeat(indent + 2);
  const lines = ['{'];
  for (const location of [...grouped.keys()].sort()) {
    const params = grouped.get(location).sort((a, b) => a.name.localeCompare(b.name));
    lines.push(`${childPad}${location}: {`);
    for (const parameter of params) {
      const optional = parameter.required ? '' : '?';
      lines.push(`${childPad}  ${JSON.stringify(parameter.name)}${optional}: ${schemaToTs(parameter.schema, indent + 4)};`);
    }
    lines.push(`${childPad}};`);
  }
  lines.push(`${pad}}`);
  return lines.join('\n');
}

function contentSchemaToTs(content, indent = 0) {
  const jsonContent = content?.['application/json'];
  return schemaToTs(jsonContent?.schema, indent);
}

function statusKeyToTs(status) {
  return /^\d+$/.test(status) ? status : JSON.stringify(status);
}

try {
  const rawSpec = readFileSync(specPath, 'utf8');
  const spec = JSON.parse(rawSpec);
  const operationEntries = [];
  const lines = [
    '/**',
    ' * Generated from artifacts/contracts/openapi.v1.json',
    ' * DO NOT EDIT.',
    ' */',
    '',
    'export interface paths {',
  ];

  for (const pathKey of stableKeys(spec.paths)) {
    lines.push(`  ${JSON.stringify(pathKey)}: {`);
    const pathItem = spec.paths[pathKey];

    for (const method of HTTP_METHODS.filter((m) => pathItem[m])) {
      const operation = pathItem[method];
      const operationId = operation.operationId;
      if (!operationId) {
        throw new Error(`Missing operationId for ${method.toUpperCase()} ${pathKey}`);
      }

      lines.push(`    ${method}: {`);

      const parameters = parametersToTs(operation.parameters, 6);
      if (parameters) {
        lines.push('      parameters: ' + parameters.replace(/\n/g, '\n      ') + ';');
      }

      if (operation.requestBody) {
        lines.push('      requestBody: {');
        lines.push('        content: {');
        lines.push(`          "application/json": ${contentSchemaToTs(operation.requestBody.content, 10)};`);
        lines.push('        };');
        lines.push('      };');
      }

      lines.push('      responses: {');
      for (const status of stableKeys(operation.responses)) {
        const response = operation.responses[status];
        lines.push(`        ${statusKeyToTs(status)}: {`);
        if (response.content) {
          lines.push('          content: {');
          lines.push(`            "application/json": ${contentSchemaToTs(response.content, 12)};`);
          lines.push('          };');
        } else {
          lines.push('          content: never;');
        }
        lines.push('        };');
      }
      lines.push('      };');
      lines.push('    };');
      operationEntries.push({ operationId, pathKey, method });
    }

    lines.push('  };');
  }

  lines.push('}');
  lines.push('');
  lines.push('export interface operations {');
  for (const { operationId, pathKey, method } of operationEntries.sort((a, b) => a.operationId.localeCompare(b.operationId))) {
    lines.push(`  ${JSON.stringify(operationId)}: paths[${JSON.stringify(pathKey)}][${JSON.stringify(method)}];`);
  }
  lines.push('}');
  lines.push('');
  lines.push('export type OperationRequestBody<TOperation extends keyof operations> =');
  lines.push('  operations[TOperation] extends { requestBody: { content: { "application/json": infer TBody } } } ? TBody : never;');
  lines.push('');
  lines.push('export type OperationResponse<TOperation extends keyof operations, TStatus extends keyof operations[TOperation]["responses"] = 200 & keyof operations[TOperation]["responses"]> =');
  lines.push('  operations[TOperation]["responses"][TStatus] extends { content: { "application/json": infer TResponse } } ? TResponse : never;');
  lines.push('');
  lines.push('export type OperationPathParams<TOperation extends keyof operations> =');
  lines.push('  operations[TOperation] extends { parameters: { path: infer TPath } } ? TPath : never;');
  lines.push('');

  const indexContent = "export * from './schema';\n";

  mkdirSync(outputDir, { recursive: true });
  writeFileSync(outputPath, lines.join('\n'), 'utf8');
  writeFileSync(indexOutputPath, indexContent, 'utf8');

  console.log('Generated REST contract schema from artifacts/contracts/openapi.v1.json');
} catch (err) {
  console.error('OpenAPI generation failed:', err);
  process.exit(1);
}
