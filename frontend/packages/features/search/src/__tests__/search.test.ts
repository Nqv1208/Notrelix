import { describe, it, expect } from 'vitest';
import { searchQueryKeys } from '../core/query/keys';

describe('searchQueryKeys', () => {
  it('should generate global search query key with search query string', () => {
    expect(searchQueryKeys.global('ws-1', 'project spec')).toEqual(['search', 'global', 'ws-1', 'project spec']);
  });

  it('should generate recent search query key', () => {
    expect(searchQueryKeys.recent).toEqual(['search', 'recent']);
  });
});
