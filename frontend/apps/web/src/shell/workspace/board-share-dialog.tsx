import { useCallback, useMemo } from "react";
import { toast } from "sonner";
import { useCurrentUser } from "@notrelix/features-auth";
import {
  buildSpaceIndex,
  initialsFor,
  markToneFor,
} from "@notrelix/features-workspace/core";
import {
  createUseWorkspaceSettings,
  formatCopy,
  useWorkspaceSpacesCopy,
} from "@notrelix/features-workspace/web";
import { useAppRuntime } from "@notrelix/runtime-web";
import type { Board } from "@notrelix/work-management-core";
import { useBoardAccess } from "@notrelix/work-management-state";
import {
  BoardShareDialogSurface,
  buildShareRows,
  buildSharingImpact,
  buildSharingLog,
  buildSharingRows,
  fillBoardShareCopy,
  SHARING_LEVEL_OF,
  type ShareLevel,
  useBoardShareCopy,
  type BoardShareView,
} from "@notrelix/work-management-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useBoardPlacements } from "./board-placement-bridge";
import { useBoardSharing } from "./use-board-sharing";
import { toastFailure } from "./failure-toast";
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
  const spacesCopy = useWorkspaceSpacesCopy();
  const sharing = useBoardSharing(workspaceId, boardId, open);

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
    // A department is named "Phòng X" on the page and in this dialog.
    const spaceName = spaceNode
      ? spaceNode.type === "Department"
        ? formatCopy(spacesCopy.page.departmentName, { name: spaceNode.name })
        : spaceNode.name
      : "";
    const space = spaceNode
      ? {
          name: spaceName,
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
      rows: sharing.data
        ? buildSharingRows(
            sharing.data,
            {
              owner: copy.people.owner,
              team: (count) => fillBoardShareCopy(copy.people.team, { count }),
              guestOnly: copy.people.guestOnly,
              inSpace: (role) =>
                fillBoardShareCopy(copy.people.inSpace, {
                  role: copy.people.roles[role],
                  space: spaceName,
                }),
              department: (name) =>
                formatCopy(spacesCopy.page.departmentName, { name }),
              outsideSpace: space
                ? fillBoardShareCopy(copy.people.outsideDepartment, {
                    space: lowerFirst(spaceName),
                  })
                : null,
              formatDay: shareDay,
            },
            initialsFor,
          )
        : buildShareRows(
            board.members,
            access,
            user?.id ?? null,
            {
              owner: copy.people.owner,
              team: (count) => fillBoardShareCopy(copy.people.team, { count }),
              guestOnly: copy.people.guestOnly,
              teamName: copy.people.teamName,
              guests: (count) =>
                fillBoardShareCopy(copy.people.guests, { count }),
            },
            initialsFor,
          ),
      policy: settings.data
        ? settings.data.allowPublicSharing
          ? "on"
          : "off"
        : "unknown",
      canManage: sharing.data?.canManage ?? access.myLevel === "manager",
    };
  }, [
    access,
    board,
    copy,
    sharing.data,
    spacesCopy,
    settings.data,
    spaceNode,
    user?.id,
    workspace?.name,
  ]);

  const logWords = useMemo(
    () => ({
      levels: {
        manage: copy.levels.manage,
        edit: copy.levels.edit,
        comment: copy.levels.comment,
        view: copy.levels.view,
      },
      text: copy.log,
      expiredNote: copy.log.expiredNote,
      fill: fillBoardShareCopy,
      when: (iso: string) => {
        const date = new Date(iso);
        const pad = (value: number) => String(value).padStart(2, "0");
        const day = `${pad(date.getDate())}/${pad(date.getMonth() + 1)}`;
        const recent = Date.now() - date.getTime() < 7 * 86_400_000;
        return recent
          ? `${day} · ${pad(date.getHours())}:${pad(date.getMinutes())}`
          : day;
      },
    }),
    [copy],
  );
  const log = useMemo(
    () =>
      sharing.data
        ? buildSharingLog(sharing.data, logWords, initialsFor)
        : null,
    [logWords, sharing.data],
  );
  const impact = useMemo(
    () =>
      sharing.data
        ? buildSharingImpact(
            sharing.data,
            {
              keepInSpace: (names) =>
                fillBoardShareCopy(copy.privateConfirm.keepInSpace, { names }),
              keepGuest: (name) =>
                fillBoardShareCopy(copy.privateConfirm.keepGuest, { name }),
              and: copy.privateConfirm.keepAnd,
            },
            initialsFor,
          )
        : null,
    [copy, sharing.data],
  );
  const write = useCallback(
    (run: () => Promise<void>) => {
      run().catch((error: unknown) =>
        toastFailure(copy.footer.writeFailed, error, ""),
      );
    },
    [copy],
  );

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
      log={log}
      impact={impact}
      onChangeLevel={
        view.canManage && sharing.data
          ? (rowId: string, level: ShareLevel) =>
              write(() => sharing.changeLevel(rowId, SHARING_LEVEL_OF[level]))
          : undefined
      }
      onRemove={
        view.canManage && sharing.data
          ? (rowId: string) => write(() => sharing.remove(rowId))
          : undefined
      }
      onChangeGeneral={
        view.canManage && sharing.data ? () => undefined : undefined
      }
      onConfirmPrivate={
        view.canManage && sharing.data
          ? (ids: readonly string[]) => write(() => sharing.makePrivate(ids))
          : undefined
      }
    />
  );
}

function lowerFirst(value: string): string {
  return value.charAt(0).toLowerCase() + value.slice(1);
}

function shareDay(iso: string): string {
  const date = new Date(iso);
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}`;
}
