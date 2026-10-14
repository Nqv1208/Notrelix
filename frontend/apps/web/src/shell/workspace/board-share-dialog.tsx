import { useCallback, useMemo } from "react";
import { toast } from "sonner";
import { useCurrentUser } from "@notrelix/features-auth";
import {
  buildSpaceIndex,
  initialsFor,
  markToneFor,
} from "@notrelix/features-workspace/core";
import { createUseWorkspaceSettings } from "@notrelix/features-workspace/web";
import { useAppRuntime } from "@notrelix/runtime-web";
import type { Board } from "@notrelix/work-management-core";
import { useBoardAccess } from "@notrelix/work-management-state";
import {
  BoardShareDialogSurface,
  buildShareRows,
  fillBoardShareCopy,
  useBoardShareCopy,
  type BoardShareView,
} from "@notrelix/work-management-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useBoardPlacements } from "./board-placement-bridge";
import { useSpaceHooks } from "./use-workspace-spaces";

/**
 * The Board share dialog of the board header's Share button: the Board access
 * read (who opens it and through what), the Board's members, and the workspace
 * sharing policy. Inviting, changing a permission or who can open the Board,
 * and the history have no write or read behind them, so the dialog draws them
 * inert; "Copy link" copies the address of the Board, which opens only for
 * people who already have access (GOV-032).
 */
export function BoardShareDialog({
  workspaceId,
  boardId,
  board,
  open,
  onClose,
}: {
  workspaceId: string;
  boardId: string;
  board: Board | undefined;
  open: boolean;
  onClose: () => void;
}) {
  const copy = useBoardShareCopy();
  const { workspace } = useWorkspaceContext();
  const user = useCurrentUser();
  const { api: runtimeClient } = useAppRuntime();
  const placements = useBoardPlacements(workspaceId);
  const spaceHooks = useSpaceHooks();
  const tree = spaceHooks.useSpaceTree(workspaceId);
  const { access } = useBoardAccess(boardId, workspaceId);
  const useSettings = useMemo(
    () =>
      createUseWorkspaceSettings({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );
  const settings = useSettings(workspaceId);

  const spaceId =
    placements.data?.find((placement) => placement.id === boardId)?.spaceId ??
    null;
  const spaceNode = useMemo(
    () =>
      spaceId
        ? (buildSpaceIndex(tree.data ?? []).byId.get(spaceId) ?? null)
        : null,
    [spaceId, tree.data],
  );

  const view = useMemo<BoardShareView | null>(() => {
    if (!board || !access) return null;
    const spaceSource = access.sources.find(
      (source) => source.kind === "space",
    );
    const space = spaceNode
      ? {
          name: spaceNode.name,
          initials: initialsFor(spaceNode.name),
          markTone: markToneFor(spaceNode.id),
        }
      : null;
    return {
      boardTitle: board.title,
      space,
      workspaceName: workspace?.name ?? "",
      general:
        board.visibility === "private"
          ? { kind: "private" }
          : space
            ? {
                kind: "space",
                name: space.name,
                count: spaceSource ? spaceSource.count : null,
                mark: { initials: space.initials, tone: space.markTone },
              }
            : { kind: "workspace" },
      rows: buildShareRows(
        board.members,
        access,
        user?.id ?? null,
        {
          owner: copy.people.owner,
          team: (count) => fillBoardShareCopy(copy.people.team, { count }),
          guestOnly: copy.people.guestOnly,
          teamName: copy.people.teamName,
          guests: (count) => fillBoardShareCopy(copy.people.guests, { count }),
        },
        initialsFor,
      ),
      policy: settings.data
        ? settings.data.allowPublicSharing
          ? "on"
          : "off"
        : "unknown",
      canManage: access.myLevel === "manager",
    };
  }, [
    access,
    board,
    copy,
    settings.data,
    spaceNode,
    user?.id,
    workspace?.name,
  ]);

  const copyLink = useCallback(() => {
    const link = `${window.location.origin}/workspaces/${encodeURIComponent(workspaceId)}/boards/${encodeURIComponent(boardId)}`;
    void Promise.resolve()
      .then(() => navigator.clipboard.writeText(link))
      .then(
        () => toast.success(copy.footer.copied),
        () => toast.error(copy.footer.copyFailed),
      );
  }, [boardId, copy, workspaceId]);

  if (!view) return null;
  return (
    <BoardShareDialogSurface
      copy={copy}
      open={open}
      view={view}
      onClose={onClose}
      onCopyLink={copyLink}
    />
  );
}
