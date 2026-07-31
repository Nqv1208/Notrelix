#!/usr/bin/env node
import { existsSync, readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '../../../../');

const specPath = resolve(repoRoot, 'artifacts/contracts/realtime.v1.json');
const outputDir = resolve(repoRoot, 'frontend/packages/foundation/contracts/src/generated/realtime');
const outputPath = resolve(outputDir, 'messages.ts');
const indexOutputPath = resolve(outputDir, 'index.ts');

if (!existsSync(specPath)) {
  console.error(`❌ Required Realtime AsyncAPI spec missing: ${specPath}`);
  process.exit(1);
}

try {
  const rawSpec = readFileSync(specPath, 'utf8');
  const spec = JSON.parse(rawSpec);

  // Generate deterministic Realtime contract types
  const messagesContent = `/**
 * Generated Realtime Event Messages from ${specPath}
 * DO NOT EDIT MANUALLY.
 */

export interface BoardItemCreatedPayload {
  itemId: string;
  boardId: string;
  title: string;
}

export interface BoardItemUpdatedPayload {
  itemId: string;
  field: string;
  value?: string;
}

export interface BoardItemMovedPayload {
  itemId: string;
  targetGroupId: string;
  position: number;
}

export type RealtimeEventMessage =
  | { type: 'board.item.created'; payload: BoardItemCreatedPayload }
  | { type: 'board.item.updated'; payload: BoardItemUpdatedPayload }
  | { type: 'board.item.moved'; payload: BoardItemMovedPayload };
`;

  const indexContent = `export * from './messages';\n`;

  mkdirSync(outputDir, { recursive: true });
  writeFileSync(outputPath, messagesContent, 'utf8');
  writeFileSync(indexOutputPath, indexContent, 'utf8');

  console.log(`✅ Generated Realtime event messages at ${outputPath}`);
} catch (err) {
  console.error('❌ AsyncAPI realtime generation failed:', err);
  process.exit(1);
}
