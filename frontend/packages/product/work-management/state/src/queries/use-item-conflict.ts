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

/** Who saved first when a write lost the race (the server's `changedByName`); null when it names no one: a Guest viewer, someone out of reach, a system write. */
export function changedByNameOf(error: unknown): string | null {
  const body = (error as { details?: unknown } | null)?.details as
    Record<string, unknown> | null | undefined;
  return typeof body?.changedByName === "string" ? body.changedByName : null;
}

/** The stale write that was refused on a version, with who won the race when the server said. */
export interface StaleVersionDetail {
  readonly kind: "stale-version";
  readonly byName: string | null;
}

/** What a refused stale write tells the open item: a schema change, or a plain lost race. */
export function conflictDetailOf(
  error: unknown,
): StaleSchemaDetail | StaleVersionDetail {
  return (
    staleSchemaOf(error) ?? {
      kind: "stale-version",
      byName: changedByNameOf(error),
    }
  );
}

/**
 * The item was deleted: the server answers not-found with its own code to a person who reads the board, and says who
 * deleted it unless that person is out of reach or the reader is a Guest. Null: an ordinary not-found.
 */
export function deletedItemOf(
  error: unknown,
): { byName: string | null } | null {
  const e = error as { code?: unknown; details?: unknown } | null;
  if (e?.code !== "work-management.item.deleted") return null;
  const name = (e.details as Record<string, unknown> | null)?.deletedByName;
  return { byName: typeof name === "string" ? name : null };
}

/** Records that a write on the item was refused as stale, so any screen showing it can say so. */
export function reportItemConflict(
  queryClient: QueryClient,
  workspaceId: string,
  itemId: string,
  detail?: StaleSchemaDetail | StaleVersionDetail | null,
): void {
  queryClient.setQueryData(conflictKey(workspaceId, itemId), detail ?? true);
}

/** Whether a write on the item was refused as stale since the person last reloaded it. */
export function useItemConflict(workspaceId: string, itemId: string) {
  const queryClient = useQueryClient();
  const { data } = useQuery({
    queryKey: conflictKey(workspaceId, itemId),
    queryFn: (): boolean | StaleSchemaDetail | StaleVersionDetail => false,
    enabled: false,
    initialData: false as boolean | StaleSchemaDetail | StaleVersionDetail,
    staleTime: Infinity,
  });
  const detail = data && typeof data === "object" ? data : null;
  return {
    conflict: data === true || detail !== null,
    staleSchema: detail?.kind === "field-deleted" ? detail : null,
    /** Who changed the item first, when the refused write was a plain lost race and the server named them. */
    changedBy: detail?.kind === "stale-version" ? detail.byName : null,
    clear: () =>
      queryClient.setQueryData(conflictKey(workspaceId, itemId), false),
    report: () => reportItemConflict(queryClient, workspaceId, itemId),
  };
}
