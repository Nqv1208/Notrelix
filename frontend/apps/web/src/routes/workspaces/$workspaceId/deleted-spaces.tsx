import { useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";
import { useFormatters } from "@notrelix/i18n";
import { describeWorkspaceFailure } from "@notrelix/features-workspace/core";
import {
  formatCopy,
  SpaceTrashSurface,
  useWorkspaceSpacesCopy,
  type DeletedSpaceFailure,
  type DeletedSpaceRow,
} from "@notrelix/features-workspace/web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useSpaceHooks } from "@/shell/workspace/use-workspace-spaces";

/**
 * The deleted departments of the workspace. A workspace Owner or Admin restores one inside its 30-day
 * window; the producer refuses a restore under a parent that is not active, a name a sibling now uses, or
 * after the window, and its own words are shown. Anyone else is told whom to ask.
 */
export function DeletedSpacesPage() {
  const { workspaceId } = useWorkspaceContext();
  const copy = useWorkspaceSpacesCopy();
  const formatters = useFormatters();
  const navigate = useNavigate();
  const hooks = useSpaceHooks();
  const deleted = hooks.useDeletedSpaces(workspaceId);
  const mutations = hooks.useSpaceMutations(workspaceId);
  const [pendingId, setPendingId] = useState<string | null>(null);
  const [failed, setFailed] = useState<{
    id: string;
    reason: DeletedSpaceFailure | "rule";
    detail: string | null;
  } | null>(null);

  const rows = deleted.isPending
    ? ({ status: "loading" } as const)
    : deleted.isError
      ? ({
          status: "error",
          reason:
            describeWorkspaceFailure(deleted.error).kind === "forbidden"
              ? "forbidden"
              : undefined,
        } as const)
      : ({
          status: "ready",
          data: deleted.data.map((row): DeletedSpaceRow => ({
            id: row.id,
            name: row.name,
            deletedAt: row.deletedAt,
            restorableUntil: row.restorableUntil,
          })),
        } as const);

  const restore = (id: string) => {
    const name = deleted.data?.find((row) => row.id === id)?.name ?? "";
    setPendingId(id);
    setFailed(null);
    mutations.restore.mutate(
      { spaceId: id },
      {
        onSuccess: () =>
          toast.success(formatCopy(copy.toasts.restored, { name })),
        onError: (error) => {
          const failure = describeWorkspaceFailure(error);
          setFailed({
            id,
            reason:
              failure.kind === "rule"
                ? "rule"
                : failure.kind === "conflict"
                  ? "conflict"
                  : failure.kind === "forbidden"
                    ? "forbidden"
                    : failure.kind === "not-found"
                      ? "gone"
                      : "failed",
            detail: failure.detail,
          });
        },
        onSettled: () => setPendingId(null),
      },
    );
  };

  return (
    <SpaceTrashSurface
      copy={copy}
      rows={rows}
      pendingId={pendingId}
      failed={failed}
      formatDate={(iso) => formatters.date(iso, { dateStyle: "medium" })}
      onRestore={restore}
      onRetry={() => void deleted.refetch()}
      onBack={() =>
        void navigate({
          to: "/workspaces/$workspaceId",
          params: { workspaceId },
        })
      }
    />
  );
}
