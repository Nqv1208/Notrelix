import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createPageExtrasApi,
  type PagePermissionLevel,
} from "../../api/page-extras.api";
import type { DocsApiClient, PageApiEndpoints } from "../../api/page.api";
import { docsQueryKeys } from "../keys";

/** Gives a workspace member a permission on the page (`Governance.ResourcePermissions.Grant`). */
export function createUseSharePage(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  const extras = createPageExtrasApi(api, endpoints);
  return function useSharePage(workspaceId: string, pageId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (input: { userId: string; level: PagePermissionLevel }) =>
        extras.grantPermission(pageId, input.userId, input.level),
      onSettled: () =>
        queryClient.invalidateQueries({
          queryKey: docsQueryKeys.permissions(workspaceId, pageId),
        }),
    });
  };
}
