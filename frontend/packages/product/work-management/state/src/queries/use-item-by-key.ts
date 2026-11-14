import { useQuery } from "@tanstack/react-query";
import type { ResolvedItemKey } from "../api/item.api";
import { useWorkManagementServices } from "../services";
import { wmQueryKeys } from "./keys";

const ITEM_ID =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/**
 * Whether an item reference in a link is a key ("ERP-142", "12") rather than an
 * item id: ids are GUIDs, and a card not yet confirmed carries an `optimistic-` id.
 */
export function isItemKeyRef(ref: string | undefined): ref is string {
  if (!ref) return false;
  const value = ref.trim();
  return (
    value.length > 0 &&
    value.length <= 64 &&
    !ITEM_ID.test(value) &&
    !value.startsWith("optimistic-")
  );
}

/**
 * Resolves a key opened from a link (UC-WM-A10): the item it names on this
 * board, or the item that kept it as an old key after moving to a board the
 * viewer can see. A key that names nothing for the viewer fails as NotFound.
 */
export function useItemByKey(
  boardId: string,
  workspaceId: string,
  ref: string | undefined,
) {
  const { cards } = useWorkManagementServices();
  const key = isItemKeyRef(ref) ? ref.trim() : undefined;
  const query = useQuery<ResolvedItemKey>({
    queryKey: wmQueryKeys.itemByKey(workspaceId, boardId, key ?? ""),
    queryFn: () => cards.resolveItemKey(boardId, key!),
    enabled: Boolean(key && boardId),
    // A NotFound is the answer, not a blip: do not ask again.
    retry: false,
    staleTime: 30_000,
  });
  return {
    /** The reference is a key, so the item to open is not known until it resolves. */
    isKey: Boolean(key),
    resolved: query.data,
    isPending: Boolean(key) && query.isPending,
    failed: Boolean(key) && query.isError,
  };
}
