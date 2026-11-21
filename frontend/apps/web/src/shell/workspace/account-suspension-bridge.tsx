import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  markAccountSuspended,
  type WorkspaceSummary,
} from "@notrelix/features-workspace/core";
import { useWorkspaceShellCopy } from "@notrelix/features-workspace/web";
import { useAppRuntime } from "@notrelix/runtime-web";

const REFUSED_TOAST_ID = "account-suspended";

// `workspaceQueryKeys.detail(id)` is `["workspace", id, "workspace", "detail"]`.
const isWorkspaceDetail = (key: readonly unknown[]) =>
  key.length === 4 &&
  key[0] === "workspace" &&
  key[2] === "workspace" &&
  key[3] === "detail";

/**
 * When the producer refuses a write with `account.suspended`, says so once (one toast id, however many
 * writes fail) and marks the cached workspaces read-only so the shell shows the banner at once, with no new
 * request. The Account is the principal here, so every cached workspace belongs to it. Renders nothing.
 */
export function AccountSuspensionBridge() {
  const { accountSuspension } = useAppRuntime();
  const queryClient = useQueryClient();
  const copy = useWorkspaceShellCopy();
  const refused = copy.accountReadOnly.refused;

  useEffect(
    () =>
      accountSuspension.subscribe(() => {
        toast.error(refused, { id: REFUSED_TOAST_ID });
        queryClient.setQueriesData<WorkspaceSummary>(
          { predicate: (query) => isWorkspaceDetail(query.queryKey) },
          (workspace) =>
            workspace ? markAccountSuspended(workspace) : workspace,
        );
      }),
    [accountSuspension, queryClient, refused],
  );

  return null;
}
