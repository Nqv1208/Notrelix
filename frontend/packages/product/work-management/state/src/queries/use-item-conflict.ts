import {
  useQuery,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import type { StaleSchemaDetail } from "@notrelix/work-management-core";

export type { StaleSchemaDetail };

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

/** Why the server refused to give someone work: the person cannot see the board yet (share it first), or only views it. */
export type AssigneeRefusal = "board-not-shared" | "viewer";

/** The refusal reason of an assignee write, read from the error's stable codes; null for any other failure. */
export function assigneeRefusalOf(error: unknown): AssigneeRefusal | null {
  const e = error as { code?: unknown; ruleCode?: unknown } | null;
  if (e?.code === "work-management.assignee.board-not-shared")
    return "board-not-shared";
  return e?.ruleCode === "WorkManagement_BoardItem_AssigneeIsViewer"
    ? "viewer"
    : null;
}

/** A write that clears a required field, or leaves it empty on create, is refused with this rule. */
export function isRequiredRefusal(error: unknown): boolean {
  return (
    (error as { ruleCode?: unknown } | null)?.ruleCode ===
    "WorkManagement_FieldValue_Required"
  );
}

const STALE_CODES = {
  "work-management.field.deleted": "field-deleted",
  "work-management.field.option-removed": "option-removed",
  "work-management.field.type-changed": "type-changed",
} as const;

/** The stale-schema detail of a refused write, when the server named one (D12). */
export function staleSchemaOf(error: unknown): StaleSchemaDetail | null {
  const body = (error as { details?: unknown } | null)?.details as
    Record<string, unknown> | null | undefined;
  const kind =
    typeof body?.errorCode === "string"
      ? (STALE_CODES as Record<string, StaleSchemaDetail["kind"]>)[
          body.errorCode
        ]
      : undefined;
  if (!body || !kind) return null;
  const text = (key: string) =>
    typeof body[key] === "string" ? (body[key] as string) : undefined;
  return {
    kind,
    fieldName: text("fieldName") ?? "",
    byName: text("changedByName") ?? null,
    optionLabel: text("optionLabel"),
    fromType: text("fromType"),
    toType: text("toType"),
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
    staleSchema: detail && detail.kind !== "stale-version" ? detail : null,
    /** Who changed the item first, when the refused write was a plain lost race and the server named them. */
    changedBy: detail?.kind === "stale-version" ? detail.byName : null,
    clear: () =>
      queryClient.setQueryData(conflictKey(workspaceId, itemId), false),
    report: () => reportItemConflict(queryClient, workspaceId, itemId),
  };
}
