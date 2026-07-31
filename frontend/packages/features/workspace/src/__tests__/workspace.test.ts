/**
 * Workspace Domain Invariants — Unit Tests
 *
 * Tests validate the PRODUCTION rules from workspace-rules.ts, not a local copy.
 * This ensures tests guard real application behavior: if the rule changes, the
 * test breaks — not silently passes with a stale local copy.
 */
import { describe, it, expect } from 'vitest';
import { validateRemoveOwner, validateDowngradeOwner } from '../core/rules/workspace-rules';
import { workspaceQueryKeys } from '../core/query/keys';

describe('validateRemoveOwner', () => {
  it('blocks removal when only 1 owner exists', () => {
    expect(validateRemoveOwner(1)).toEqual({
      canRemove: false,
      error: 'Cannot remove the last owner of a workspace',
    });
  });

  it('blocks removal when 0 owners (degenerate/corrupted state)', () => {
    expect(validateRemoveOwner(0)).toEqual({
      canRemove: false,
      error: 'Cannot remove the last owner of a workspace',
    });
  });

  it('allows removal when 2 or more owners exist', () => {
    expect(validateRemoveOwner(2)).toEqual({ canRemove: true });
    expect(validateRemoveOwner(10)).toEqual({ canRemove: true });
  });
});

describe('validateDowngradeOwner', () => {
  it('blocks downgrade when only 1 owner exists', () => {
    expect(validateDowngradeOwner(1)).toEqual({
      canRemove: false,
      error: 'Cannot downgrade the last owner of a workspace',
    });
  });

  it('allows downgrade when 2 or more owners exist', () => {
    expect(validateDowngradeOwner(2)).toEqual({ canRemove: true });
  });
});

describe('workspaceQueryKeys', () => {
  it('formats workspace detail key correctly', () => {
    expect(workspaceQueryKeys.detail('ws-100')).toEqual(['workspaces', 'detail', 'ws-100']);
  });

  it('formats workspace all and members key correctly', () => {
    expect(workspaceQueryKeys.all).toEqual(['workspaces']);
    expect(workspaceQueryKeys.members('ws-100')).toEqual(['workspaces', 'members', 'ws-100']);
  });
});
