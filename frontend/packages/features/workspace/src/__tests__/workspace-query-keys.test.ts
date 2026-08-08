import { describe, expect, it } from 'vitest';
import { workspaceQueryKeys } from '../core/query/keys';

describe('workspaceQueryKeys canonical factory', () => {
  it('FND-032 keys are deterministic and stable for the same input', () => {
    expect(workspaceQueryKeys.detail('ws-1')).toEqual(
      workspaceQueryKeys.detail('ws-1'),
    );
    expect(workspaceQueryKeys.members('ws-1')).toEqual(
      workspaceQueryKeys.members('ws-1'),
    );
    expect(workspaceQueryKeys.invitationByToken('tok-1')).toEqual(
      workspaceQueryKeys.invitationByToken('tok-1'),
    );
  });

  it('FND-032 every factory returns a non-empty array key', () => {
    const factories: Array<() => readonly string[]> = [
      () => workspaceQueryKeys.all,
      () => workspaceQueryKeys.detail('ws-1'),
      () => workspaceQueryKeys.snapshot('ws-1'),
      () => workspaceQueryKeys.members('ws-1'),
      () => workspaceQueryKeys.views('ws-1'),
      () => workspaceQueryKeys.activeView('ws-1', 'table'),
      () => workspaceQueryKeys.invitations('ws-1'),
      () => workspaceQueryKeys.invitationByToken('tok-1'),
      () => workspaceQueryKeys.pendingInvitations,
      () => workspaceQueryKeys.activity('ws-1'),
    ];

    for (const factory of factories) {
      const key = factory();
      expect(Array.isArray(key)).toBe(true);
      expect(key.length).toBeGreaterThan(0);
      expect(key.every((part) => typeof part === 'string')).toBe(true);
    }
  });

  it('FND-033 workspace-scoped keys differ across workspaces', () => {
    expect(workspaceQueryKeys.detail('ws-1')).not.toEqual(
      workspaceQueryKeys.detail('ws-2'),
    );
    expect(workspaceQueryKeys.members('ws-1')).not.toEqual(
      workspaceQueryKeys.members('ws-2'),
    );
    expect(workspaceQueryKeys.views('ws-1')).not.toEqual(
      workspaceQueryKeys.views('ws-2'),
    );
    expect(workspaceQueryKeys.invitations('ws-1')).not.toEqual(
      workspaceQueryKeys.invitations('ws-2'),
    );
    expect(workspaceQueryKeys.activity('ws-1')).not.toEqual(
      workspaceQueryKeys.activity('ws-2'),
    );
  });

  it('FND-033 all keys share the workspaces namespace prefix', () => {
    const keys = [
      workspaceQueryKeys.all,
      workspaceQueryKeys.detail('ws-1'),
      workspaceQueryKeys.snapshot('ws-1'),
      workspaceQueryKeys.members('ws-1'),
      workspaceQueryKeys.views('ws-1'),
      workspaceQueryKeys.activeView('ws-1', 'table'),
      workspaceQueryKeys.invitations('ws-1'),
      workspaceQueryKeys.invitationByToken('tok-1'),
      workspaceQueryKeys.pendingInvitations,
      workspaceQueryKeys.activity('ws-1'),
    ];

    for (const key of keys) {
      expect(key[0]).toBe('workspaces');
    }
  });

  it('FND-033 invitationByToken is globally unique per token and pending invitations is a static key', () => {
    expect(workspaceQueryKeys.invitationByToken('tok-1')).not.toEqual(
      workspaceQueryKeys.invitationByToken('tok-2'),
    );
    expect(workspaceQueryKeys.pendingInvitations).toEqual([
      'workspaces',
      'invitations',
      'pending',
    ]);
  });
});
