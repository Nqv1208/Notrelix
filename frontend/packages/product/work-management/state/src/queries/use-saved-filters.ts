import { useQuery } from "@tanstack/react-query";
import type { SavedFilter } from "@notrelix/work-management-core";
import { useWorkManagementServices } from "../services";
import { wmQueryKeys } from "./keys";

/** The saved filters of a board that the signed-in member sees (theirs and the shared ones). */
export function useSavedFilters(
  boardId: string,
  workspaceId: string,
  enabled = true,
) {
  const { savedFilters } = useWorkManagementServices();
  return useQuery<SavedFilter[]>({
    queryKey: wmQueryKeys.boardSavedFilters(workspaceId, boardId),
    queryFn: () => savedFilters.list(boardId),
    enabled: enabled && Boolean(boardId && workspaceId),
    staleTime: 30_000,
  });
}
