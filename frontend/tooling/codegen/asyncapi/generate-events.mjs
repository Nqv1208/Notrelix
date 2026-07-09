#!/usr/bin/env node

/**
 * AsyncAPI Event Generator
 *
 * Generates TypeScript types for realtime events from AsyncAPI spec.
 * Target: packages/foundation/contracts/src/generated/events/
 *
 * Usage: node asyncapi/generate-events.mjs
 */

import { readFileSync, writeFileSync, mkdirSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = join(__dirname, '../../..');
const specPath = join(rootDir, 'asyncapi.json');
const outputDir = join(rootDir, 'packages/foundation/contracts/src/generated/events');

console.log('AsyncAPI Event Generator');
console.log('========================');
console.log(`Spec: ${specPath}`);
console.log(`Output: ${outputDir}`);

if (!existsSync(specPath)) {
  console.log('\nNo asyncapi.json found. Skipping generation.');
  console.log('Place your AsyncAPI spec at the project root as asyncapi.json');
  process.exit(0);
}

// Ensure output directory exists
mkdirSync(outputDir, { recursive: true });

// Read and parse spec
const spec = JSON.parse(readFileSync(specPath, 'utf-8'));
console.log(`\nParsed spec: ${spec.info?.title || 'Unknown'} v${spec.info?.version || '?'}`);

// Generate event types (placeholder)
const eventsContent = `/**
 * Auto-generated event types from AsyncAPI spec
 * DO NOT EDIT MANUALLY
 *
 * Spec: ${spec.info?.title || 'Unknown'} v${spec.info?.version || '?'}
 * Generated: ${new Date().toISOString()}
 */

export interface BaseEvent {
  eventId: string;
  type: string;
  version: number;
  workspaceId?: string;
  actorId?: string;
  occurredAt: string;
  correlationId?: string;
  causationId?: string;
}

export interface BoardPatchEvent extends BaseEvent {
  type: 'board.patch';
  boardId: string;
  patches: BoardPatch[];
}

export interface BoardPatch {
  entity: 'cell' | 'item' | 'field' | 'view';
  action: 'created' | 'updated' | 'deleted' | 'moved';
  entityId: string;
  fieldId?: string;
  value?: unknown;
}

export interface NotificationEvent extends BaseEvent {
  type: 'notification.created';
  notificationId: string;
  title: string;
  body: string;
  link?: string;
}

export interface PresenceEvent extends BaseEvent {
  type: 'presence.joined' | 'presence.left' | 'presence.updated';
  userId: string;
  status: 'active' | 'idle' | 'offline';
  cursor?: { x: number; y: number };
}

export type RealtimeEvent = BoardPatchEvent | NotificationEvent | PresenceEvent;
`;

writeFileSync(join(outputDir, 'index.ts'), eventsContent);
console.log('Generated index.ts');

console.log('\nDone!');
