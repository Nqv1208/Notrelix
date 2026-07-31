import { describe, it, expect } from 'vitest';
import { governanceQueryKeys } from '../core/query/keys';

function isRoleAllowed(currentRole: 'owner' | 'admin' | 'member' | 'guest', requiredRole: 'owner' | 'admin' | 'member' | 'guest'): boolean {
  const roleHierarchy: Record<string, number> = {
    owner: 4,
    admin: 3,
    member: 2,
    guest: 1,
  };
  return (roleHierarchy[currentRole] ?? 0) >= (roleHierarchy[requiredRole] ?? 0);
}

describe('Governance Role Authorization Invariants', () => {
  it('owner should have higher or equal access than admin, member, guest', () => {
    expect(isRoleAllowed('owner', 'admin')).toBe(true);
    expect(isRoleAllowed('owner', 'member')).toBe(true);
    expect(isRoleAllowed('owner', 'guest')).toBe(true);
  });

  it('member should not have admin or owner privileges', () => {
    expect(isRoleAllowed('member', 'admin')).toBe(false);
    expect(isRoleAllowed('member', 'owner')).toBe(false);
  });

  it('governanceQueryKeys should format resource permission keys', () => {
    expect(governanceQueryKeys.permissions('ws-1')).toEqual(['governance', 'permissions', 'ws-1']);
  });
});
