import { describe, expect, it } from 'vitest';
import { readFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { wmQueryKeys } from '../queries/keys';

describe('wmQueryKeys canonical factory', () => {
  it('FND-030 keys are deterministic and stable for the same input', () => {
    expect(wmQueryKeys.fullBoard('b1', 'ws-1')).toEqual(
      wmQueryKeys.fullBoard('b1', 'ws-1'),
    );
    expect(wmQueryKeys.cardDetail('c1')).toEqual(wmQueryKeys.cardDetail('c1'));
    expect(wmQueryKeys.cardChecklists('c1')).toEqual(
      wmQueryKeys.cardChecklists('c1'),
    );
  });

  it('FND-030 every factory returns a non-empty array key', () => {
    const factories: Array<() => readonly string[]> = [
      () => wmQueryKeys.list('ws-1'),
      () => wmQueryKeys.workspaceList('ws-1'),
      () => wmQueryKeys.fullBoard('b1'),
      () => wmQueryKeys.fullBoard('b1', 'ws-1'),
      () => wmQueryKeys.view('ws-1', 'b1'),
      () => wmQueryKeys.groups('ws-1', 'b1'),
      () => wmQueryKeys.columns('ws-1', 'b1'),
      () => wmQueryKeys.cardDetail('c1'),
      () => wmQueryKeys.cardUpdates('c1'),
      () => wmQueryKeys.cardFiles('c1'),
      () => wmQueryKeys.cardComments('c1'),
      () => wmQueryKeys.cardActivity('c1'),
      () => wmQueryKeys.cardChecklists('c1'),
    ];

    for (const factory of factories) {
      const key = factory();
      expect(Array.isArray(key)).toBe(true);
      expect(key.length).toBeGreaterThan(0);
      expect(key.every((part) => typeof part === 'string')).toBe(true);
    }
  });

  it('FND-031 workspace-scoped keys carry the workspaceId so different workspaces never share a key', () => {
    expect(wmQueryKeys.list('ws-1')).not.toEqual(wmQueryKeys.list('ws-2'));
    expect(wmQueryKeys.workspaceList('ws-1')).not.toEqual(
      wmQueryKeys.workspaceList('ws-2'),
    );
    expect(wmQueryKeys.view('ws-1', 'b1')).not.toEqual(
      wmQueryKeys.view('ws-2', 'b1'),
    );
    expect(wmQueryKeys.groups('ws-1', 'b1')).not.toEqual(
      wmQueryKeys.groups('ws-2', 'b1'),
    );
    expect(wmQueryKeys.columns('ws-1', 'b1')).not.toEqual(
      wmQueryKeys.columns('ws-2', 'b1'),
    );
    expect(wmQueryKeys.fullBoard('b1', 'ws-1')).not.toEqual(
      wmQueryKeys.fullBoard('b1', 'ws-2'),
    );
  });

  it('FND-031 entity keys differ per entity id', () => {
    expect(wmQueryKeys.cardDetail('c1')).not.toEqual(
      wmQueryKeys.cardDetail('c2'),
    );
    expect(wmQueryKeys.cardUpdates('c1')).not.toEqual(
      wmQueryKeys.cardUpdates('c2'),
    );
    expect(wmQueryKeys.cardChecklists('c1')).not.toEqual(
      wmQueryKeys.cardChecklists('c2'),
    );
    expect(wmQueryKeys.view('ws-1', 'b1')).not.toEqual(
      wmQueryKeys.view('ws-1', 'b2'),
    );
  });

  it('FND-031 all board keys share the boards namespace prefix and card keys the cards prefix', () => {
    expect(wmQueryKeys.all).toEqual(['boards']);

    const boardKeys = [
      wmQueryKeys.list('ws-1'),
      wmQueryKeys.workspaceList('ws-1'),
      wmQueryKeys.fullBoard('b1', 'ws-1'),
      wmQueryKeys.view('ws-1', 'b1'),
      wmQueryKeys.groups('ws-1', 'b1'),
      wmQueryKeys.columns('ws-1', 'b1'),
    ];
    for (const key of boardKeys) {
      expect(key[0]).toBe('boards');
    }

    const cardKeys = [
      wmQueryKeys.cardDetail('c1'),
      wmQueryKeys.cardUpdates('c1'),
      wmQueryKeys.cardFiles('c1'),
      wmQueryKeys.cardComments('c1'),
      wmQueryKeys.cardActivity('c1'),
      wmQueryKeys.cardChecklists('c1'),
    ];
    for (const key of cardKeys) {
      expect(key[0]).toBe('cards');
    }
  });

  it('FND-034 the legacy queryKeys alias is fully removed from exports and source', () => {
    const indexSource = readFileSync(
      join(dirname(fileURLToPath(import.meta.url)), '..', 'index.ts'),
      'utf8',
    );
    expect(indexSource).not.toMatch(/queryKeys/);

    const keysSource = readFileSync(
      join(dirname(fileURLToPath(import.meta.url)), '..', 'queries', 'keys.ts'),
      'utf8',
    );
    expect(keysSource).not.toMatch(/export const queryKeys/);
    expect(keysSource).not.toMatch(/checklists: \(cardId/);
  });
});
