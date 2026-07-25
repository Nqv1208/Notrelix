import { describe, it, expect } from 'vitest';
import { workspaceQueryKeys } from '../core/query/keys';

function validateRemoveOwner(ownerCount: number): { canRemove: boolean; error?: string } {
  if (ownerCount <= 1) {
    return { canRemove: false, error: 'Cannot remove the last owner of a workspace' };
  }
  return { canRemove: true };
}

describe('Workspace Domain Invariants', () => {
  it('cannot remove last owner from workspace', () => {
    expect(validateRemoveOwner(1)).toEqual({
      canRemove: false,
      error: 'Cannot remove the last owner of a workspace',
    });
  });

  it('can remove owner if there are 2 or more owners', () => {
    expect(validateRemoveOwner(2)).toEqual({ canRemove: true });
  });

  it('workspaceQueryKeys should format workspace details key', () => {
    expect(workspaceQueryKeys.detail('ws-100')).toEqual(['workspaces', 'detail', 'ws-100']);
  });
});
