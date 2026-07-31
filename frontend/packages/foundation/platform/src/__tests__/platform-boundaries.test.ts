import { describe, expect, it } from 'vitest';
import { hasPermission, permissions } from '../permissions/core';

describe('platform boundaries', () => {
  it('evaluates permissions without React or browser globals', () => {
    expect(hasPermission('owner', permissions.workspace.manage)).toBe(true);
    expect(hasPermission('member', permissions.workspace.manage)).toBe(false);
    expect(hasPermission('guest', permissions.comment.create)).toBe(true);
    expect(hasPermission(undefined, permissions.board.create)).toBe(false);
  });

  it('loads platform core in a Node environment without window', async () => {
    expect(typeof globalThis.window).toBe('undefined');

    const core = await import('../permissions/core');

    expect(core.hasPermission('admin', core.permissions.board.create)).toBe(true);
  });
});
