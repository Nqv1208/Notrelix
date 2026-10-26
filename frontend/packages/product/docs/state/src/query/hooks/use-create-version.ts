import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createVersionApi } from "../../api/version.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** "Save version": the page as it is now, under a name. */
export function createUseCreateVersion(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const versionApi = createVersionApi(api, endpoints);
  return function useCreateVersion(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (name: string) => versionApi.create(pageId, name),
      onSuccess: () =>
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.versions(workspaceId, pageId),
        }),
    });
  };
}
