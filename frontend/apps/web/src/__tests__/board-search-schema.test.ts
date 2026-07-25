/**
 * Board Route Search Schema — Unit Tests
 *
 * Tests validate the PRODUCTION schema defined in router.tsx, not a local copy.
 * This ensures tests actually guard the behavior of the deployed application.
 */
import { describe, it, expect } from 'vitest';
import { boardSearchSchema } from '../router';

describe('boardSearchSchema (from router.tsx)', () => {
  describe('view param', () => {
    it('defaults to "kanban" when not provided', () => {
      const parsed = boardSearchSchema.parse({});
      expect(parsed.view).toBe('kanban');
    });

    it('accepts all valid view enum values', () => {
      expect(boardSearchSchema.parse({ view: 'table' }).view).toBe('table');
      expect(boardSearchSchema.parse({ view: 'kanban' }).view).toBe('kanban');
      expect(boardSearchSchema.parse({ view: 'calendar' }).view).toBe('calendar');
      expect(boardSearchSchema.parse({ view: 'timeline' }).view).toBe('timeline');
    });

    it('rejects invalid view values', () => {
      expect(() => boardSearchSchema.parse({ view: 'list' })).toThrow();
      expect(() => boardSearchSchema.parse({ view: 'gantt' })).toThrow();
      expect(() => boardSearchSchema.parse({ view: '' })).toThrow();
    });
  });

  describe('optional params', () => {
    it('omits filter when not provided', () => {
      const parsed = boardSearchSchema.parse({});
      expect(parsed.filter).toBeUndefined();
    });

    it('preserves filter, sort and item when provided', () => {
      const parsed = boardSearchSchema.parse({
        filter: 'status=done',
        sort: 'priority:asc',
        item: 'item-100',
      });
      expect(parsed.filter).toBe('status=done');
      expect(parsed.sort).toBe('priority:asc');
      expect(parsed.item).toBe('item-100');
    });
  });
});
