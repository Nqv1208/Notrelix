import { describe, it, expect } from 'vitest';
import { collaborationQueryKeys } from '../core/query/keys';

describe('collaborationQueryKeys', () => {
  it('should generate resource comments key', () => {
    expect(collaborationQueryKeys.comments('page-1')).toEqual(['collaboration', 'comments', 'page-1']);
  });

  it('should generate reactions key', () => {
    expect(collaborationQueryKeys.reactions('page-1')).toEqual(['collaboration', 'reactions', 'page-1']);
  });
});
