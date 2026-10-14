import { useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";
import {
  formatCopy,
  useWorkspaceHomeCopy,
} from "@notrelix/features-workspace/web";
import {
  buildSpaceIndex,
  identityToneFor,
  initialsFor,
  MARK_TONE_IDS,
  markInitialsFor,
  markToneFor,
  type MarkToneId,
} from "@notrelix/features-workspace/core";
import type { Board } from "@notrelix/work-management-core";
import { useBoardAccess } from "@notrelix/work-management-state";
import {
  useKanbanLaneCopy,
  type BoardHeaderData,
} from "@notrelix/work-management-web";
import { useBoardPlacements } from "./board-placement-bridge";
import { useSpaceHooks } from "./use-workspace-spaces";

function toMarkTone(color: string | null, fallbackId: string): MarkToneId {
  return (MARK_TONE_IDS as readonly string[]).includes(color ?? "")
    ? (color as MarkToneId)
    : markToneFor(fallbackId);
}

/**
 * The board header data for the board route: the board's Space (from the
 * board placements and the Space tree — the Board model has no `spaceId`),
 * who can see it, the caller's access summary and the members' faces.
 * Settings stays without a handler until its page exists, so the header shows
 * it disabled with the reason; Share opens the share dialog the route hosts.
 */
export function useBoardHeaderData(
  workspaceId: string,
  boardId: string,
  board: Board | undefined,
  /** Opens the Share dialog; absent: Share stays disabled with the reason. */
  onOpenShare?: () => void,
): BoardHeaderData {
  const navigate = useNavigate();
  const laneCopy = useKanbanLaneCopy();
  const homeCopy = useWorkspaceHomeCopy();
  const placements = useBoardPlacements(workspaceId);
  const spaceHooks = useSpaceHooks();
  const tree = spaceHooks.useSpaceTree(workspaceId);
  const { access } = useBoardAccess(boardId, workspaceId);

  const spaceId =
    placements.data?.find((placement) => placement.id === boardId)?.spaceId ??
    null;
  const spaceNode = useMemo(() => {
    if (!spaceId) return null;
    return buildSpaceIndex(tree.data ?? []).byId.get(spaceId) ?? null;
  }, [spaceId, tree.data]);

  const placement = placements.data?.find((entry) => entry.id === boardId);
  return useMemo<BoardHeaderData>(() => {
    const spaceHref = spaceNode
      ? `/workspaces/${workspaceId}/spaces/${spaceNode.id}`
      : undefined;
    return {
      space: spaceNode
        ? {
            name: spaceNode.name,
            label:
              spaceNode.type === "Department"
                ? formatCopy(homeCopy.list.departmentOf, {
                    name: spaceNode.name,
                  })
                : spaceNode.name,
            initials: initialsFor(spaceNode.name),
            markTone: markToneFor(spaceNode.id),
            href: spaceHref,
          }
        : placement?.departmentName
          ? {
              // A Board shared with a member who cannot see its Space: the
              // department is named, there is no page to open.
              name: placement.departmentName,
              label: formatCopy(homeCopy.list.departmentOf, {
                name: placement.departmentName,
              }),
              initials: markInitialsFor(placement.departmentName),
              markTone: toMarkTone(placement.departmentColor, boardId),
            }
          : null,
      visibility:
        board?.visibility === "private"
          ? "private"
          : spaceNode || placement?.departmentName
            ? "space"
            : "workspace",
      access,
      people: (board?.members ?? []).map((member) => ({
        id: member.userId,
        name: member.name,
        initials: member.initials || initialsFor(member.name),
        tone: identityToneFor(member.userId),
      })),
      onOpenShare,
      onOpenSpace: spaceNode
        ? () =>
            void navigate({
              to: "/workspaces/$workspaceId/spaces/$spaceId",
              params: { workspaceId, spaceId: spaceNode.id },
            })
        : undefined,
      onCopyLink: () => {
        const link = `${window.location.origin}${window.location.pathname}`;
        void Promise.resolve()
          .then(() => navigator.clipboard.writeText(link))
          .then(
            () => toast.success(laneCopy.linkCopied),
            () => toast.error(laneCopy.linkCopyFailed),
          );
      },
    };
  }, [
    access,
    board,
    homeCopy,
    laneCopy,
    navigate,
    onOpenShare,
    placement,
    boardId,
    spaceNode,
    workspaceId,
  ]);
}
