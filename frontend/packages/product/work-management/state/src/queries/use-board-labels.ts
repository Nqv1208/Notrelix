import { useQuery } from "@tanstack/react-query";
import type { CardLabel } from "@notrelix/work-management-core";
import { useWorkManagementServices } from "../services";
import { wmQueryKeys } from "./keys";

/** The labels a board offers (the choices behind an item's label editor). */
export function useBoardLabels(
  boardId: string,
  workspaceId: string,
  enabled = true,
) {
  const { labels } = useWorkManagementServices();
  return useQuery<CardLabel[]>({
    queryKey: wmQueryKeys.boardLabels(workspaceId, boardId),
    queryFn: () => labels.getBoardLabels(boardId),
    enabled: enabled && Boolean(boardId && workspaceId),
    staleTime: 30_000,
  });
}
