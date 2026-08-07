import { describe, expect, it } from 'vitest';
import { boardSearchSchema } from '../router';

describe('boardSearchSchema', () => {
  it('defaults view and preserves route URL state fields', () => {
    expect(boardSearchSchema.parse({})).toEqual({ view: 'kanban' });

    expect(
      boardSearchSchema.parse({
        view: 'table',
        filter: 'status=done',
        sort: 'priority:asc',
        groupBy: 'assignee',
        item: 'item-100',
      }),
    ).toEqual({
      view: 'table',
      filter: 'status=done',
      sort: 'priority:asc',
      groupBy: 'assignee',
      item: 'item-100',
    });
  });
});
