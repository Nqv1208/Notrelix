import { describe, expect, it } from 'vitest';
import {
  globalQueryKey,
  accountQueryKey,
  workspaceQueryKey,
  assertNotrelixQueryKey,
} from '../query-key-scope';

describe('QRY-001 global helper shape', () => {
  it('constructs ["global", resource, ...segments]', () => {
    expect(globalQueryKey('workspace-invitation', 'token', 'tok-1')).toEqual([
      'global',
      'workspace-invitation',
      'token',
      'tok-1',
    ]);
  });

  it('allows zero segments', () => {
    expect(globalQueryKey('feature-flags')).toEqual(['global', 'feature-flags']);
  });
});

describe('QRY-002 account helper shape', () => {
  it('constructs ["account", resource, ...segments]', () => {
    expect(accountQueryKey('workspaces', 'list')).toEqual(['account', 'workspaces', 'list']);
    expect(accountQueryKey('notifications', 'unread-count')).toEqual([
      'account',
      'notifications',
      'unread-count',
    ]);
  });
});

describe('QRY-003 workspace helper shape', () => {
  it('constructs ["workspace", workspaceId, resource, ...segments]', () => {
    expect(workspaceQueryKey('ws-1', 'work-management', 'boards', 'list')).toEqual([
      'workspace',
      'ws-1',
      'work-management',
      'boards',
      'list',
    ]);
  });

  it('keeps workspace ID before the resource', () => {
    const key = workspaceQueryKey('ws-1', 'automation', 'rules');
    expect(key[0]).toBe('workspace');
    expect(key[1]).toBe('ws-1');
    expect(key[2]).toBe('automation');
  });
});

describe('assertNotrelixQueryKey validation', () => {
  it('accepts valid global/account/workspace keys', () => {
    expect(() => assertNotrelixQueryKey(globalQueryKey('x'))).not.toThrow();
    expect(() => assertNotrelixQueryKey(accountQueryKey('x', 1))).not.toThrow();
    expect(() =>
      assertNotrelixQueryKey(workspaceQueryKey('ws-1', 'x', 'segment')),
    ).not.toThrow();
  });

  it('QRY-004 rejects an invalid root', () => {
    expect(() => assertNotrelixQueryKey([])).toThrow(/must not be empty/);
    expect(() => assertNotrelixQueryKey(['bogus', 'x'])).toThrow(/invalid Notrelix query key root/);
    expect(() => assertNotrelixQueryKey(['boards', 'list'])).toThrow(/invalid Notrelix query key root/);
    expect(() => assertNotrelixQueryKey(['pages', 'detail', 'p-1'])).toThrow(/invalid Notrelix query key root/);
  });

  it('rejects a missing resource', () => {
    expect(() => assertNotrelixQueryKey(['global'])).toThrow(/requires a resource/);
    expect(() => assertNotrelixQueryKey(['account'])).toThrow(/requires a resource/);
    expect(() => assertNotrelixQueryKey(['workspace', 'ws-1'])).toThrow(/requires a resource/);
  });

  it('QRY-005 rejects an empty or missing workspace ID', () => {
    expect(() => assertNotrelixQueryKey(['workspace', '', 'board'])).toThrow(
      /non-empty workspace ID/,
    );
    expect(() => assertNotrelixQueryKey(['workspace'])).toThrow(/requires a resource/);
  });
});
