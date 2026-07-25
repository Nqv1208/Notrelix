import type { QueryClient, QueryKey } from '@tanstack/react-query';

/**
 * Snapshot of a single query before an optimistic mutation.
 * Tracks both the data value AND whether the query cache entry existed at all,
 * so rollback can remove stale optimistic entries when previousData was undefined.
 */
interface OptimisticSnapshot {
  queryKey: QueryKey;
  previousData: unknown;
  /** True if the cache entry existed before the mutation (even if data was undefined) */
  hadCacheEntry: boolean;
}

export interface OptimisticCommandOptions<TData, TVariables> {
  queryClient: QueryClient;
  /**
   * One or more query keys whose cache should be atomically snapshotted,
   * optimistically updated, and rolled back together on mutation failure.
   */
  queryKeys: QueryKey[];
  updateFn: (oldData: TData | undefined, variables: TVariables) => TData;
  mutationFn: (variables: TVariables) => Promise<TData>;
  variables: TVariables;
}

/**
 * Execute an optimistic mutation that atomically updates multiple query cache
 * entries and rolls them back on failure — including removing entries that
 * were created by the optimistic update (previousData === undefined).
 *
 * @example
 * await executeOptimisticCommand({
 *   queryClient,
 *   queryKeys: [boardKeys.detail(boardId), boardKeys.list(workspaceId)],
 *   updateFn: (old, vars) => ({ ...old, title: vars.title }),
 *   mutationFn: (vars) => api.post('/boards', vars),
 *   variables: { title: 'New Board' },
 * });
 */
export async function executeOptimisticCommand<TData, TVariables>({
  queryClient,
  queryKeys,
  updateFn,
  mutationFn,
  variables,
}: OptimisticCommandOptions<TData, TVariables>): Promise<TData> {
  // 1. Cancel any outgoing refetches for all affected queries so they don't
  //    overwrite our optimistic update.
  await Promise.all(
    queryKeys.map((key) => queryClient.cancelQueries({ queryKey: key }))
  );

  // 2. Snapshot the current cache state for ALL affected query keys.
  //    We track `hadCacheEntry` to distinguish "data was undefined" from
  //    "no cache entry existed", which changes the rollback strategy.
  const snapshots: OptimisticSnapshot[] = queryKeys.map((key) => ({
    queryKey: key,
    previousData: queryClient.getQueryData(key),
    hadCacheEntry: queryClient.getQueryState(key) !== undefined,
  }));

  // 3. Apply optimistic updates to all query keys immediately.
  for (const key of queryKeys) {
    queryClient.setQueryData<TData>(key, (old) => updateFn(old, variables));
  }

  try {
    const result = await mutationFn(variables);
    return result;
  } catch (error) {
    // 4. ROLLBACK: restore every query key to its pre-mutation state.
    for (const snapshot of snapshots) {
      if (snapshot.hadCacheEntry) {
        // Restore to original data (even if it was undefined — that's valid cache state)
        queryClient.setQueryData(snapshot.queryKey, snapshot.previousData);
      } else {
        // The cache entry did not exist before our optimistic update.
        // Remove it entirely so stale optimistic data doesn't linger.
        queryClient.removeQueries({ queryKey: snapshot.queryKey, exact: true });
      }
    }
    throw error;
  } finally {
    // 5. Invalidate all affected queries to trigger a server refetch,
    //    ensuring the cache converges with the authoritative server state.
    await Promise.all(
      queryKeys.map((key) =>
        queryClient.invalidateQueries({ queryKey: key })
      )
    );
  }
}
