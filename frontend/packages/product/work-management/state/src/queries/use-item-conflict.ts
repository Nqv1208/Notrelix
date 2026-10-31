import { useQuery, useQueryClient, type QueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";

const conflictKey = (workspaceId: string, itemId: string) =>
  workspaceQueryKey(workspaceId, "work-management", "item-conflict", itemId);

/** Whether a failure is the server refusing a stale item version (412). */
export function isVersionConflict(error: unknown): boolean {
  return (
    typeof error === "object" &&
    error !== null &&
    ((error as { status?: unknown }).status === 412 ||
      (error as { kind?: unknown }).kind === "conflict")
  );
}

/** Records that a write on the item was refused as stale, so any screen showing it can say so. */
export function reportItemConflict(
  queryClient: QueryClient,
  workspaceId: string,
  itemId: string,
): void {
  queryClient.setQueryData(conflictKey(workspaceId, itemId), true);
}

/** Whether a write on the item was refused as stale since the person last reloaded it. */
export function useItemConflict(workspaceId: string, itemId: string) {
  const queryClient = useQueryClient();
  const { data } = useQuery({
    queryKey: conflictKey(workspaceId, itemId),
    queryFn: () => false,
    enabled: false,
    initialData: false,
    staleTime: Infinity,
  });
  return {
    conflict: data === true,
    clear: () =>
      queryClient.setQueryData(conflictKey(workspaceId, itemId), false),
    report: () => reportItemConflict(queryClient, workspaceId, itemId),
  };
}
