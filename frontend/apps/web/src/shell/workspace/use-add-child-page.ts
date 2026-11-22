import { useCallback, useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";
import {
  PENDING_PAGE_ID_PREFIX,
  createUseCreatePage,
} from "@notrelix/docs-state";
import {
  describeWorkspaceFailure,
  type WorkspaceFailure,
} from "@notrelix/features-workspace/core";
import {
  formatCopy,
  type WorkspaceShellCopy,
} from "@notrelix/features-workspace/web";
import { useFeatureRuntimeDependencies } from "@notrelix/runtime-web";
import { useAttemptKey } from "./work-management-bridge";

/** The producer's `ruleCode` for a page nested past the depth limit. */
const MAX_DEPTH_RULE = "Documents_PageTree_MaxDepthExceeded";

const isPendingPage = (id: string) => id.startsWith(PENDING_PAGE_ID_PREFIX);

/** The sentence that says why a child page was not added, by the kind of refusal. */
export function addChildFailureText(
  failure: WorkspaceFailure,
  copy: WorkspaceShellCopy["sidebar"],
  name: string,
): string {
  const text =
    failure.ruleCode === MAX_DEPTH_RULE
      ? copy.docsAddChildTooDeep
      : failure.kind === "forbidden"
        ? copy.docsAddChildDenied
        : failure.kind === "not-found"
          ? copy.docsAddChildGone
          : copy.docsAddChildFailed;
  return formatCopy(text, { name });
}

/**
 * Adds a page inside a page from the sidebar tree. The row shows at once and
 * goes away if the server refuses; on success the parent opens and the new
 * page is opened for writing. The refusal says why (no permission, parent
 * gone, too deep, or a retryable failure).
 */
export function useAddChildPage({
  workspaceId,
  copy,
  expand,
}: {
  workspaceId: string;
  copy: WorkspaceShellCopy["sidebar"];
  expand: (pageId: string) => void;
}) {
  const navigate = useNavigate();
  const { api, endpoints } = useFeatureRuntimeDependencies();
  const useCreatePage = useMemo(
    () => createUseCreatePage(api, endpoints),
    [api, endpoints],
  );
  const create = useCreatePage(workspaceId);
  const nextKey = useAttemptKey();
  const { mutate, isPending, variables } = create;

  const onAddChild = useCallback(
    (parent: { id: string; title: string }) => {
      expand(parent.id);
      mutate(
        {
          title: copy.docsUntitled,
          workspaceId,
          parentId: parent.id,
          idempotencyKey: nextKey(),
        },
        {
          onSuccess: (created) =>
            void navigate({
              to: "/workspaces/$workspaceId/docs/$docId",
              params: { workspaceId, docId: created.id },
            } as never),
          onError: (error) =>
            toast.error(
              addChildFailureText(
                describeWorkspaceFailure(error),
                copy,
                parent.title,
              ),
            ),
        },
      );
    },
    [copy, expand, mutate, navigate, nextKey, workspaceId],
  );

  return {
    onAddChild,
    addingChildOf: isPending ? (variables?.parentId ?? null) : null,
    isPending: isPendingPage,
  };
}
