import { useMemo, useState } from "react";
import { toast } from "sonner";
import {
  describeWorkspaceFailure,
  isAccountReadOnly,
} from "@notrelix/features-workspace/core";
import {
  ArchivedWorkspaceBanner,
  createUseWorkspaceSettingsMutations,
  formatCopy,
  useWorkspaceShellCopy,
  type ArchivedBannerState,
} from "@notrelix/features-workspace/web";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useWorkspaceViewer } from "./use-workspace-viewer";

/**
 * The read-only banner of an archived workspace for every member, with Unarchive for the Owner. The
 * action quotes the workspace version the shell holds; a stale version shows the conflict state, a
 * refusal says only the owner may, and success reads the workspace again so the banner leaves and
 * writes work again (D02, I05).
 */
export function ArchivedWorkspaceBannerHost() {
  const copy = useWorkspaceShellCopy();
  const { api: runtimeClient } = useAppRuntime();
  const { workspaceId, workspace, refetch } = useWorkspaceContext();
  const viewer = useWorkspaceViewer(workspaceId);
  const useMutations = useMemo(
    () =>
      createUseWorkspaceSettingsMutations({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );
  const mutations = useMutations(workspaceId);
  const [outcome, setOutcome] = useState<ArchivedBannerState>("idle");

  // The Account state is the stronger one and has its own banner.
  if (workspace?.isArchived !== true || isAccountReadOnly(workspace))
    return null;

  const version = workspace.version;
  const state: ArchivedBannerState = mutations.unarchive.isPending
    ? "pending"
    : outcome;

  return (
    <ArchivedWorkspaceBanner
      copy={copy}
      canUnarchive={viewer.role === "owner" && version !== undefined}
      state={state}
      onReload={() => {
        setOutcome("idle");
        void refetch();
      }}
      onUnarchive={() => {
        if (version === undefined) return;
        setOutcome("idle");
        mutations.unarchive.mutate(version, {
          onSuccess: () =>
            toast.success(
              formatCopy(copy.archived.done, { workspace: workspace.name }),
            ),
          onError: (error) => {
            const kind = describeWorkspaceFailure(error).kind;
            setOutcome(
              kind === "conflict"
                ? "conflict"
                : kind === "forbidden"
                  ? "denied"
                  : "failed",
            );
          },
        });
      }}
    />
  );
}
