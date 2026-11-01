import { useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";
import { useFormatters } from "@notrelix/i18n";
import { useRetiredBoards } from "@notrelix/work-management-state";
import {
  RetiredBoardsSurface,
  useRetiredBoardsCopy,
  type RetiredBoardFailure,
  type RetiredBoardsTab,
} from "@notrelix/work-management-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";

/**
 * The archived and deleted boards of the workspace. A Manager unarchives an archived board or
 * restores a deleted one inside its 30-day window; each write sends the board version the list
 * showed, so a stale one changes nothing and the lists are read again.
 */
export function ArchivedBoardsPage({
  tab: initialTab,
}: {
  tab?: RetiredBoardsTab;
}) {
  const { workspaceId } = useWorkspaceContext();
  const copy = useRetiredBoardsCopy();
  const formatters = useFormatters();
  const navigate = useNavigate();
  const retired = useRetiredBoards(workspaceId);
  const [tab, setTab] = useState<RetiredBoardsTab>(initialTab ?? "archived");
  const [pendingId, setPendingId] = useState<string | null>(null);
  const [failed, setFailed] = useState<{
    id: string;
    reason: RetiredBoardFailure;
  } | null>(null);

  const source = tab === "archived" ? retired.archived : retired.deleted;
  const rows = source.map((board) => ({
    id: board.id ?? "",
    title: board.title ?? "",
    background: board.background ?? "var(--muted-foreground)",
    retiredAt: board.retiredAt ?? "",
    restorableUntil: board.restorableUntil ?? null,
    href: `/workspaces/${workspaceId}/boards/${board.id}`,
  }));

  async function act(id: string) {
    const board = source.find((candidate) => candidate.id === id);
    if (!board || typeof board.version !== "number") return;
    setPendingId(id);
    setFailed(null);
    const result =
      tab === "archived"
        ? await retired.unarchive(id, board.version)
        : await retired.restore(id, board.version);
    setPendingId(null);
    if (result.ok)
      toast.success(
        tab === "archived" ? copy.done.unarchive : copy.done.restore,
      );
    else
      setFailed({
        id,
        reason: result.reason === "failed" ? "failed" : result.reason,
      });
  }

  return (
    <RetiredBoardsSurface
      copy={copy}
      tab={tab}
      onTab={(next) => {
        setTab(next);
        setFailed(null);
      }}
      rows={rows}
      loading={retired.isLoading}
      error={retired.isError}
      onRetry={() => void retired.refetch()}
      pendingId={pendingId}
      failed={failed}
      formatDate={(iso) => formatters.date(iso, { dateStyle: "medium" })}
      onAct={(id) => void act(id)}
      onOpen={(id) =>
        void navigate({
          to: "/workspaces/$workspaceId/boards/$boardId",
          params: { workspaceId, boardId: id },
        })
      }
    />
  );
}
