import { describe, it, expect } from 'vitest';
import { z } from 'zod';

const boardSearchSchema = z.object({
  view: z.enum(['table', 'kanban', 'calendar', 'timeline']).default('table'),
  filter: z.string().optional(),
  sort: z.string().optional(),
  item: z.string().optional(),
});

describe('Board Route Search Schema', () => {
  it('parses empty search params with default view table', () => {
    const parsed = boardSearchSchema.parse({});
    expect(parsed.view).toBe('table');
  });

  it('validates view enum values', () => {
    expect(boardSearchSchema.parse({ view: 'kanban' }).view).toBe('kanban');
    expect(boardSearchSchema.parse({ view: 'timeline' }).view).toBe('timeline');
    expect(() => boardSearchSchema.parse({ view: 'invalid_view' })).toThrow();
  });

  it('parses optional filter and item params', () => {
    const parsed = boardSearchSchema.parse({ filter: 'status=done', item: 'item-100' });
    expect(parsed.filter).toBe('status=done');
    expect(parsed.item).toBe('item-100');
  });
});
