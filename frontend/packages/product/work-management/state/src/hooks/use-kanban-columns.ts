import { useMemo } from "react";
import { useFullBoard } from "../queries/use-full-board";

export function useKanbanColumns(boardId: string, workspaceId: string) {
  const { groups, isLoading, error } = useFullBoard(boardId, workspaceId);

  const sortedColumns = useMemo(() => {
    if (!groups) return [];
    return [...groups].sort((a, b) => a.position - b.position);
  }, [groups]);

  return {
    columns: sortedColumns,
    isLoading,
    error,
  };
}
