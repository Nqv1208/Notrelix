import type { QueryClient, QueryKey } from "@tanstack/react-query";
import type { AppError } from "@notrelix/kernel";

export interface OptimisticSnapshot {
  readonly queryKey: QueryKey;
  readonly existed: boolean;
  readonly value: unknown;
}

export interface OptimisticUpdate<TVariables> {
  readonly queryKey: QueryKey;
  apply(queryClient: QueryClient, variables: TVariables): OptimisticSnapshot;
}

export interface CommandContext {
  readonly commandId: string;
  readonly correlationId: string;
  readonly idempotencyKey: string;
}

export function defineOptimisticUpdate<TData, TVariables>(
  queryKey: QueryKey,
  updater: (current: TData | undefined, variables: TVariables) => TData,
): OptimisticUpdate<TVariables> {
  return {
    queryKey,
    apply(queryClient: QueryClient, variables: TVariables): OptimisticSnapshot {
      const existed = queryClient.getQueryState(queryKey) !== undefined;
      const previousValue = queryClient.getQueryData<TData>(queryKey);

      queryClient.setQueryData<TData>(queryKey, (current) =>
        updater(current, variables),
      );

      return {
        queryKey,
        existed,
        value: previousValue,
      };
    },
  };
}

export interface ExecuteOptimisticCommandOptions<TResult, TVariables> {
  queryClient: QueryClient;
  commandId: string;
  updates: OptimisticUpdate<TVariables>[];
  mutationFn: (
    variables: TVariables,
    context: CommandContext,
  ) => Promise<TResult>;
  variables: TVariables;
  correlationId?: string;
  idempotencyKey?: string;
  reconcile?: (
    result: TResult,
    queryClient: QueryClient,
    context: CommandContext,
  ) => void | Promise<void>;
  onConflict?: (
    error: AppError,
    snapshots: readonly OptimisticSnapshot[],
    context: CommandContext,
  ) => Promise<"rollback" | "refetch"> | "rollback" | "refetch";
  invalidate?: readonly QueryKey[];
}

export async function executeOptimisticCommand<TResult, TVariables>({
  queryClient,
  commandId,
  updates,
  mutationFn,
  variables,
  correlationId = commandId,
  idempotencyKey = commandId,
  reconcile,
  onConflict,
  invalidate,
}: ExecuteOptimisticCommandOptions<TResult, TVariables>): Promise<TResult> {
  // Check for duplicate query keys
  const keyStrings = updates.map((u) => JSON.stringify(u.queryKey));
  const uniqueKeys = new Set(keyStrings);
  if (uniqueKeys.size !== keyStrings.length) {
    throw new Error(
      "[OptimisticCommand] Duplicate query keys passed in single command",
    );
  }

  // 1. Cancel ongoing refetches for all target queries
  await Promise.all(
    updates.map((update) =>
      queryClient.cancelQueries({ queryKey: update.queryKey }),
    ),
  );

  const context: CommandContext = {
    commandId,
    correlationId,
    idempotencyKey,
  };

  const snapshots: OptimisticSnapshot[] = [];
  const invalidateKeys = invalidate ?? updates.map((update) => update.queryKey);

  try {
    // 2. Snapshot & apply optimistic updates sequentially
    for (const update of updates) {
      snapshots.push(update.apply(queryClient, variables));
    }

    // 3. Run the actual mutation
    const result = await mutationFn(variables, context);
    await reconcile?.(result, queryClient, context);
    return result;
  } catch (error) {
    let conflictPolicy: "rollback" | "refetch" | null = null;
    if (onConflict && isConflictError(error)) {
      conflictPolicy = await onConflict(error, snapshots, context);
    }

    // 4. ROLLBACK in reverse order if anything fails
    if (conflictPolicy !== "refetch") {
      rollbackSnapshots(queryClient, snapshots);
    }
    throw error;
  } finally {
    // 5. Invalidate explicit target queries to ensure authoritative server convergence.
    await Promise.all(
      invalidateKeys.map((queryKey) =>
        queryClient.invalidateQueries({ queryKey }),
      ),
    );
  }
}

function rollbackSnapshots(
  queryClient: QueryClient,
  snapshots: readonly OptimisticSnapshot[],
): void {
  for (let i = snapshots.length - 1; i >= 0; i--) {
    const snapshot = snapshots[i]!;
    if (snapshot.existed) {
      queryClient.setQueryData(snapshot.queryKey, snapshot.value);
    } else {
      queryClient.removeQueries({ queryKey: snapshot.queryKey, exact: true });
    }
  }
}

function isConflictError(error: unknown): error is AppError {
  return (
    typeof error === "object" &&
    error !== null &&
    "kind" in error &&
    (error as { kind?: unknown }).kind === "conflict"
  );
}
