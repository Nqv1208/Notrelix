import type { QueryClient, QueryKey } from '@tanstack/react-query';

export interface OptimisticSnapshot {
  readonly queryKey: QueryKey;
  readonly existed: boolean;
  readonly value: unknown;
}

export interface OptimisticUpdate<TVariables> {
  readonly queryKey: QueryKey;
  apply(queryClient: QueryClient, variables: TVariables): OptimisticSnapshot;
}

export function defineOptimisticUpdate<TData, TVariables>(
  queryKey: QueryKey,
  updater: (current: TData | undefined, variables: TVariables) => TData
): OptimisticUpdate<TVariables> {
  return {
    queryKey,
    apply(queryClient: QueryClient, variables: TVariables): OptimisticSnapshot {
      const existed = queryClient.getQueryState(queryKey) !== undefined;
      const previousValue = queryClient.getQueryData<TData>(queryKey);

      queryClient.setQueryData<TData>(queryKey, (current) => updater(current, variables));

      return {
        queryKey,
        existed,
        value: previousValue,
      };
    },
  };
}

export interface ExecuteOptimisticCommandOptions<TData, TVariables> {
  queryClient: QueryClient;
  updates: OptimisticUpdate<TVariables>[];
  mutationFn: (variables: TVariables) => Promise<TData>;
  variables: TVariables;
}

export async function executeOptimisticCommand<TData, TVariables>({
  queryClient,
  updates,
  mutationFn,
  variables,
}: ExecuteOptimisticCommandOptions<TData, TVariables>): Promise<TData> {
  // Check for duplicate query keys
  const keyStrings = updates.map((u) => JSON.stringify(u.queryKey));
  const uniqueKeys = new Set(keyStrings);
  if (uniqueKeys.size !== keyStrings.length) {
    throw new Error('[OptimisticCommand] Duplicate query keys passed in single command');
  }

  // 1. Cancel ongoing refetches for all target queries
  await Promise.all(
    updates.map((update) => queryClient.cancelQueries({ queryKey: update.queryKey }))
  );

  const snapshots: OptimisticSnapshot[] = [];

  try {
    // 2. Snapshot & apply optimistic updates sequentially
    for (const update of updates) {
      snapshots.push(update.apply(queryClient, variables));
    }

    // 3. Run the actual mutation
    const result = await mutationFn(variables);
    return result;
  } catch (error) {
    // 4. ROLLBACK in reverse order if anything fails
    for (let i = snapshots.length - 1; i >= 0; i--) {
      const snapshot = snapshots[i]!;
      if (snapshot.existed) {
        queryClient.setQueryData(snapshot.queryKey, snapshot.value);
      } else {
        queryClient.removeQueries({ queryKey: snapshot.queryKey, exact: true });
      }
    }
    throw error;
  } finally {
    // 5. Invalidate target queries to ensure authoritative server convergence
    await Promise.all(
      updates.map((update) =>
        queryClient.invalidateQueries({ queryKey: update.queryKey })
      )
    );
  }
}
