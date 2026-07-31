import { describe, it, expect } from 'vitest';
import { authQueryKeys } from '../core/query/keys';

describe('authQueryKeys', () => {
  it('should generate auth root query key', () => {
    expect(authQueryKeys.all).toEqual(['auth']);
  });

  it('should generate auth profile query key', () => {
    expect(authQueryKeys.profile).toEqual(['auth', 'profile']);
  });
});
