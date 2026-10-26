import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createImportApi, type ImportPageInput } from "../../api/import.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** Imports a file as a new page (see `createImportApi`). */
export function createUseImportPage(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const importApi = createImportApi(api, endpoints);
  return function useImportPage(workspaceId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (input: ImportPageInput) =>
        importApi.importPage(workspaceId, input),
      onSettled: () =>
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.list(workspaceId),
        }),
    });
  };
}
