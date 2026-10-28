import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { useMobileRuntime } from "@notrelix/runtime-mobile";
import { blocksFromWire } from "@notrelix/docs-mobile";
import { createDocumentApi, isNoAccess } from "./document-api";

/** The page query key carries the workspace: a page is never shared across scopes. */
export const documentQueryKey = (workspaceId: string, pageId: string) =>
  ["docs", "workspace", workspaceId, "page", pageId, "read"] as const;

export function useDocument(workspaceId: string, pageId: string) {
  const runtime = useMobileRuntime();
  const api = useMemo(
    () => createDocumentApi({ baseUrl: runtime.env.apiUrl }),
    [runtime.env.apiUrl],
  );
  const query = useQuery({
    queryKey: documentQueryKey(workspaceId, pageId),
    enabled: Boolean(workspaceId && pageId),
    retry: (count, error) => !isNoAccess(error) && count < 2,
    queryFn: async ({ signal }) => {
      const read = await api.read(workspaceId, pageId, signal);
      return {
        title: read.page.title,
        blocks: blocksFromWire(read.blocks),
      };
    },
  });
  return {
    title: query.data?.title,
    blocks: query.data?.blocks,
    isLoading: query.isPending,
    noAccess: query.isError && isNoAccess(query.error),
    failed: query.isError && !isNoAccess(query.error),
    retry: () => void query.refetch(),
  };
}
