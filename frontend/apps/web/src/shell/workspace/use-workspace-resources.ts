import { useCallback, useMemo } from "react";
import { createUsePageList } from "@notrelix/docs-state";
import { useFeatureRuntimeDependencies } from "@notrelix/runtime-web";
import { useWorkspaceBoards } from "@notrelix/work-management-state";

export interface WorkspaceResource {
  readonly id: string;
  readonly name: string;
  /** Parent Doc title for nested Docs; `null` at the workspace root. */
  readonly parentName: string | null;
  readonly updatedAt: string | null;
}

export interface WorkspaceResources {
  readonly ready: boolean;
  readonly failed: boolean;
  readonly boards: readonly WorkspaceResource[];
  readonly docs: readonly WorkspaceResource[];
  readonly retry: () => void;
}

/**
 * The authorized Board (Work Management) and Doc (Documents) lists of the
 * workspace, mapped to what workspace pages display. Shares the owning
 * packages' query keys, so the shell and pages read one cache entry.
 */
export function useWorkspaceResources(workspaceId: string): WorkspaceResources {
  const { api, endpoints } = useFeatureRuntimeDependencies();
  const usePageList = useMemo(
    () => createUsePageList(api, endpoints),
    [api, endpoints],
  );
  const boards = useWorkspaceBoards(workspaceId);
  const pages = usePageList(workspaceId);

  const retry = useCallback(() => {
    if (boards.isError) void boards.refetch();
    if (pages.isError) void pages.refetch();
  }, [boards, pages]);

  return useMemo(() => {
    const titleById = new Map(
      (pages.data ?? []).map((page) => [page.id, page.title]),
    );
    return {
      ready: boards.isSuccess && pages.isSuccess,
      failed: boards.isError || pages.isError,
      boards: (boards.data ?? [])
        .filter((board) => !board.isArchived)
        .map((board) => ({
          id: board.id,
          name: board.title,
          parentName: null,
          // The Board list does not report a last change (open item WX-DIR-001).
          updatedAt: board.updatedAt ?? null,
        })),
      docs: (pages.data ?? []).map((page) => ({
        id: page.id,
        name: page.title,
        parentName: page.parentId
          ? (titleById.get(page.parentId) ?? null)
          : null,
        updatedAt: page.updatedAt ?? null,
      })),
      retry,
    };
  }, [
    boards.data,
    boards.isError,
    boards.isSuccess,
    pages.data,
    pages.isError,
    pages.isSuccess,
    retry,
  ]);
}
