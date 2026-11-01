import {
  useQuery,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";
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

/** The schema change a stale write ran into (a field the board no longer has) and who made it. */
export interface StaleSchemaDetail {
  readonly kind: "field-deleted";
  readonly fieldName: string;
  readonly byName: string | null;
}

/** The stale-schema detail of a refused write, when the server named one (D12). */
export function staleSchemaOf(error: unknown): StaleSchemaDetail | null {
  const body = (error as { details?: unknown } | null)?.details as
    Record<string, unknown> | null | undefined;
  if (body?.errorCode !== "work-management.field.deleted") return null;
  return {
    kind: "field-deleted",
    fieldName: typeof body.fieldName === "string" ? body.fieldName : "",
    byName: typeof body.changedByName === "string" ? body.changedByName : null,
  };
}

/** Records that a write on the item was refused as stale, so any screen showing it can say so. */
export function reportItemConflict(
  queryClient: QueryClient,
  workspaceId: string,
  itemId: string,
  detail?: StaleSchemaDetail | null,
): void {
  queryClient.setQueryData(conflictKey(workspaceId, itemId), detail ?? true);
}

/** Whether a write on the item was refused as stale since the person last reloaded it. */
export function useItemConflict(workspaceId: string, itemId: string) {
  const queryClient = useQueryClient();
  const { data } = useQuery({
    queryKey: conflictKey(workspaceId, itemId),
    queryFn: (): boolean | StaleSchemaDetail => false,
    enabled: false,
    initialData: false as boolean | StaleSchemaDetail,
    staleTime: Infinity,
  });
  return {
    conflict: data === true || typeof data === "object",
    staleSchema:
      data && typeof data === "object" ? (data as StaleSchemaDetail) : null,
    clear: () =>
      queryClient.setQueryData(conflictKey(workspaceId, itemId), false),
    report: () => reportItemConflict(queryClient, workspaceId, itemId),
  };
}
