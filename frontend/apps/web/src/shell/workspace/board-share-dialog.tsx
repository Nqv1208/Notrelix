import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { useCurrentUser } from "@notrelix/features-auth";
import {
  buildSpaceIndex,
  initialsFor,
  markToneFor,
} from "@notrelix/features-workspace/core";
import {
  createUseWorkspaceMembers,
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
  shareToneFor,
  type ShareCandidate,
  type ShareLevel,
  useBoardShareCopy,
  type BoardShareView,
  type ShareRowView,
} from "@notrelix/work-management-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useBoardPlacements } from "./board-placement-bridge";
import { useBoardSharing } from "./use-board-sharing";
import { toastFailure } from "./failure-toast";
import { useSpaceHooks } from "./use-workspace-spaces";

const LAST_MANAGER_RULE = "WorkManagement_Board_LastManager";

/**
 * The Board share dialog of the board header's Share button: the Board access
 * read (who opens it and through what), the Board's members, and the workspace
 * sharing policy, and the Board's sharing read (permissions given on the Board,
 * the history and who loses the Board on Private). A reader the producer
 * discloses no entries to sees how many people and guests can open the Board.
 * Changing who can open the Board has no write behind it, so the dialog draws
 * it inert; inviting gives a workspace member a permission on the Board; "Copy link" copies the address of the Board, which opens only for
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
  const useMembers = useMemo(
    () => createUseWorkspaceMembers({ api: runtimeClient.api }),
    [runtimeClient],
  );
  const spacesCopy = useWorkspaceSpacesCopy();
  const sharing = useBoardSharing(workspaceId, boardId, open);
  const members = useMembers(workspaceId, {
    enabled: open && (sharing.data?.canManage ?? false),
  });

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
              levelCapped: copy.people.levelCapped,
              outsideSpace: space
                ? fillBoardShareCopy(copy.people.outsideDepartment, {
                    space: lowerFirst(spaceName),
                  })
                : null,
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
      hiddenCounts:
        sharing.data && !sharing.data.entriesDisclosed
          ? {
              people: sharing.data.peopleCount,
              guests: sharing.data.guestCount,
            }
          : null,
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
      sharing.data?.canManage
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
  // The workspace's members the Board is not shared with yet.
  const candidates = useMemo<readonly ShareCandidate[]>(() => {
    const listed = new Set(sharing.data?.entries.map((entry) => entry.id));
    if (user?.id) listed.add(user.id);
    return (members.data ?? [])
      .filter(
        (member) => member.membership !== "suspended" && !listed.has(member.id),
      )
      .map((member) => ({
        id: member.id,
        name: member.name,
        person: {
          id: member.id,
          name: member.name,
          initials: initialsFor(member.name),
          tone: shareToneFor(member.id),
        },
      }));
  }, [members.data, sharing.data, user?.id]);

  // The last Manager refused: the dialog says so at its head instead of a toast.
  const [refused, setRefused] = useState<{
    row: ShareRowView;
    removal: boolean;
  } | null>(null);
  useEffect(() => {
    if (!open) setRefused(null);
  }, [open]);

  const write = useCallback(
    (
      run: () => Promise<void>,
      subject?: { row: ShareRowView; removal: boolean },
    ) => {
      setRefused(null);
      run().catch((error: unknown) => {
        if (
          subject &&
          (error as { ruleCode?: string } | null)?.ruleCode ===
            LAST_MANAGER_RULE
        ) {
          setRefused(subject);
          return;
        }
        toastFailure(copy.footer.writeFailed, error, (failure) =>
          failure.kind === "conflict" ? copy.footer.writeConflict : "",
        );
      });
    },
    [copy],
  );

  const notice = useMemo(() => {
    const text = copy.lastManager;
    if (refused) {
      const name = refused.row.name;
      const team = refused.row.kind === "team";
      return {
        kind: "refused" as const,
        title: fillBoardShareCopy(
          team
            ? text.teamTitle
            : refused.removal
              ? text.removeTitle
              : text.lowerTitle,
          { name },
        ),
        body: fillBoardShareCopy(
          team
            ? text.teamBody
            : refused.removal
              ? text.removeBody
              : text.lowerBody,
          { name },
        ),
        addManager: true,
      };
    }
    // Managing through Governance: the person is no Manager entry of the Board.
    if (
      view?.canManage &&
      !view.hiddenCounts &&
      !view.rows.some((row) => row.isYou && row.level === "manage")
    ) {
      return {
        kind: "info" as const,
        title: text.viaAdminTitle,
        body: text.viaAdminBody,
      };
    }
    return null;
  }, [copy, refused, view]);

  const copyLink = useCallback(() => {
    const link = `${window.location.origin}/workspaces/${encodeURIComponent(workspaceId)}/boards/${encodeURIComponent(boardId)}`;
    void Promise.resolve()
      .then(() => navigator.clipboard.writeText(link))
      .then(
        () => toast.success(copy.footer.copied),
        () => toast.error(copy.footer.copyFailed),
      );
  }, [boardId, copy, workspaceId]);

  const subjectOf = (rowId: string, removal: boolean) => {
    const row = view?.rows.find((candidate) => candidate.id === rowId);
    return row ? { row, removal } : undefined;
  };

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
      notice={notice}
      candidates={view.canManage && sharing.data ? candidates : undefined}
      onInvite={
        view.canManage && sharing.data
          ? async (userId: string, level: ShareLevel) => {
              try {
                await sharing.invite(userId, SHARING_LEVEL_OF[level]);
              } catch (error: unknown) {
                toastFailure(copy.footer.writeFailed, error, "");
              }
            }
          : undefined
      }
      onChangeLevel={
        view.canManage && sharing.data
          ? (rowId: string, level: ShareLevel) =>
              write(
                () => sharing.changeLevel(rowId, SHARING_LEVEL_OF[level]),
                subjectOf(rowId, false),
              )
          : undefined
      }
      onRemove={
        view.canManage && sharing.data
          ? (rowId: string) =>
              write(() => sharing.remove(rowId), subjectOf(rowId, true))
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
