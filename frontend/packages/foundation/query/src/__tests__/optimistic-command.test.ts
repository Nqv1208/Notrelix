import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient } from '@tanstack/react-query';
import { executeOptimisticCommand, defineOptimisticUpdate } from '../optimistic-command';

describe('executeOptimisticCommand with defineOptimisticUpdate', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  it('supports distinct per-key updaters for detail and list cache shapes', async () => {
    const detailKey = ['boards', '1'];
    const listKey = ['boards', 'list'];

    queryClient.setQueryData(detailKey, { id: '1', title: 'Old Title' });
    queryClient.setQueryData(listKey, [{ id: '1', title: 'Old Title' }]);

    const mutationFn = vi.fn().mockResolvedValue({ id: '1', title: 'New Title' });

    const result = await executeOptimisticCommand({
      queryClient,
      updates: [
        defineOptimisticUpdate(detailKey, (old: any, vars: { title: string }) => ({
          ...old,
          title: vars.title,
        })),
        defineOptimisticUpdate(listKey, (old: any[] | undefined, vars: { title: string }) =>
          (old ?? []).map((item) => ({ ...item, title: vars.title }))
        ),
      ],
      mutationFn,
      variables: { title: 'New Title' },
    });

    expect(result).toEqual({ id: '1', title: 'New Title' });
    expect(queryClient.getQueryData(detailKey)).toEqual({ id: '1', title: 'New Title' });
    expect(queryClient.getQueryData(listKey)).toEqual([{ id: '1', title: 'New Title' }]);
  });

  it('rolls back all updates in reverse order on mutation failure', async () => {
    const detailKey = ['boards', '1'];
    queryClient.setQueryData(detailKey, { id: '1', title: 'Original Title' });

    const mutationFn = vi.fn().mockRejectedValue(new Error('Network error'));

    await expect(
      executeOptimisticCommand({
        queryClient,
        updates: [
          defineOptimisticUpdate(detailKey, (old: any, vars: { title: string }) => ({
            ...old,
            title: vars.title,
          })),
        ],
        mutationFn,
        variables: { title: 'Optimistic Title' },
      })
    ).rejects.toThrow('Network error');

    // Cache should be restored to pre-mutation state
    expect(queryClient.getQueryData(detailKey)).toEqual({ id: '1', title: 'Original Title' });
  });

  it('removes entries that did not exist prior to optimistic command on failure', async () => {
    const nonExistentKey = ['boards', 'new-id'];

    expect(queryClient.getQueryState(nonExistentKey)).toBeUndefined();

    const mutationFn = vi.fn().mockRejectedValue(new Error('Creation failed'));

    await expect(
      executeOptimisticCommand({
        queryClient,
        updates: [
          defineOptimisticUpdate(nonExistentKey, (_old: any, vars: { title: string }) => ({
            id: 'new-id',
            title: vars.title,
          })),
        ],
        mutationFn,
        variables: { title: 'New Item' },
      })
    ).rejects.toThrow('Creation failed');

    // Cache entry should be completely removed, not left as undefined/empty state
    expect(queryClient.getQueryState(nonExistentKey)).toBeUndefined();
  });
});
