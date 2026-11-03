import { useCallback, useMemo, useState } from "react";
import { toast } from "sonner";
import {
  createUseMovePageToSpace,
  createUsePageList,
} from "@notrelix/docs-state";
import {
  appendPlacement,
  buildSpaceIndex,
  describeWorkspaceFailure,
  initialsFor,
  markToneFor,
  moveTargets,
  visibleSpaceRows,
  type SpaceNode,
} from "@notrelix/features-workspace/core";
import {
  formatCopy,
  MoveToSpaceDialog,
  useWorkspaceSpacesCopy,
  type MoveToSpaceSubject,
} from "@notrelix/features-workspace/web";
import { useFeatureRuntimeDependencies } from "@notrelix/runtime-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import {
  BoardMoveDialog,
  useBoardLifecycleCopy,
  type BoardMoveImpact,
  type MovePlaceView,
} from "@notrelix/work-management-web";
import {
  useBoardMoveImpact,
  useBoardPlacements,
  useMoveBoardToSpace,
} from "./board-placement-bridge";
import { useWorkspaceViewer } from "./use-workspace-viewer";
import { spaceRecoveryHint, useSpaceHooks } from "./use-workspace-spaces";
import { useAttemptKey } from "./work-management-bridge";

/**
 * Move a Space (with its subtree, WSP-021), a Board or a root Doc (with its
 * pages, GOV-028, docs/product/workspaces.md §79) to another place in the
 * org tree. Every move is optimistic with rollback; the producer authorizes
 * the source and the target, and a stale version shows as a conflict in the
 * dialog after the latest data is reloaded.
 */
export function useMoveToSpace() {
  const { workspaceId, workspace } = useWorkspaceContext();
  const copy = useWorkspaceSpacesCopy();
  const hooks = useSpaceHooks();
  const tree = hooks.useSpaceTree(workspaceId);
  const { move } = hooks.useSpaceMutations(workspaceId);
  const placements = useBoardPlacements(workspaceId);
  const moveBoard = useMoveBoardToSpace(workspaceId);
  const viewer = useWorkspaceViewer(workspaceId);
  const nextAttemptKey = useAttemptKey();
  const { api, endpoints } = useFeatureRuntimeDependencies();
  const docs = useMemo(
    () => ({
      usePageList: createUsePageList(api, endpoints),
      useMovePageToSpace: createUseMovePageToSpace(api, endpoints),
    }),
    [api, endpoints],
  );
  // The same cache entry the Space page and the directory read.
  const pages = docs.usePageList(workspaceId);
  const movePage = docs.useMovePageToSpace(workspaceId);
  const [subject, setSubject] = useState<MoveToSpaceSubject | null>(null);
  const [error, setError] = useState<string | null>(null);
  const lifecycle = useBoardLifecycleCopy();
  // The place picked in the board dialog (`undefined`: none yet; `null`: the top of the workspace).
  const [picked, setPicked] = useState<string | null | undefined>(undefined);
  const impactQuery = useBoardMoveImpact(
    subject?.kind === "board" ? subject.id : null,
    picked,
  );

  const index = useMemo(() => buildSpaceIndex(tree.data ?? []), [tree.data]);
  const targets = useMemo(
    () =>
      moveTargets(
        index,
        subject?.kind === "space"
          ? { kind: "space", spaceId: subject.space.id }
          : { kind: subject?.kind === "page" ? "page" : "board" },
        viewer.role,
      ),
    [index, subject, viewer.role],
  );

  const openForSpace = useCallback((space: SpaceNode) => {
    setError(null);
    setSubject({ kind: "space", space });
  }, []);

  const openForBoard = useCallback(
    (board: { id: string; name: string }) => {
      setError(null);
      setPicked(undefined);
      const placement = placements.data?.find((item) => item.id === board.id);
      setSubject({
        kind: "board",
        id: board.id,
        name: board.name,
        spaceId: placement?.spaceId ?? null,
      });
    },
    [placements.data],
  );

  /** A root Doc; the Space page and the directory never offer a nested one. */
  const openForPage = useCallback(
    (doc: { id: string; name: string }) => {
      setError(null);
      const page = pages.data?.find((item) => item.id === doc.id);
      setSubject({
        kind: "page",
        id: doc.id,
        name: doc.name,
        spaceId: page?.spaceId ?? null,
      });
    },
    [pages.data],
  );

  const onFailure = useCallback(
    (failure: unknown) => {
      // Some page below the page being moved is not the caller's to change.
      if (
        (failure as { code?: unknown } | null)?.code ===
        "documents.page.subtree-not-permitted"
      ) {
        setError(copy.toasts.pageSubtreeRefused);
        return;
      }
      const described = describeWorkspaceFailure(failure);
      setError(
        described.kind === "rule" || described.kind === "validation"
          ? (described.detail ?? copy.toasts.unknown)
          : spaceRecoveryHint(copy)(described),
      );
    },
    [copy],
  );

  const onSubmit = useCallback(
    (target: string | null, addUserIds?: readonly string[]) => {
      if (!subject) return;
      setError(null);
      const where = target
        ? (index.byId.get(target)?.name ?? "")
        : (workspace?.name ?? "");
      const done = (name: string) => {
        setSubject(null);
        toast.success(formatCopy(copy.toasts.moved, { name }), {
          description: where,
        });
      };
      if (subject.kind === "space") {
        const moving = subject.space;
        move.mutate(
          {
            spaceId: moving.id,
            parentId: target,
            ...appendPlacement(index, target, moving.id),
            expectedVersion: moving.version,
          },
          { onSuccess: () => done(moving.name), onError: onFailure },
        );
        return;
      }
      if (subject.kind === "page") {
        const page = pages.data?.find((item) => item.id === subject.id);
        movePage.mutate(
          {
            pageId: subject.id,
            spaceId: target,
            // The producer rejects 0 (no version read) instead of guessing.
            expectedVersion: page?.version ?? 0,
          },
          { onSuccess: () => done(subject.name), onError: onFailure },
        );
        return;
      }
      const placement = placements.data?.find((item) => item.id === subject.id);
      moveBoard.mutate(
        {
          boardId: subject.id,
          spaceId: target,
          expectedVersion: placement?.version ?? 0,
          attemptKey: nextAttemptKey(),
          ...(addUserIds?.length ? { addUserIds } : {}),
        },
        { onSuccess: () => done(subject.name), onError: onFailure },
      );
    },
    [
      copy,
      index,
      move,
      moveBoard,
      movePage,
      nextAttemptKey,
      onFailure,
      pages.data,
      placements.data,
      subject,
      workspace?.name,
    ],
  );

  const boardImpact = useMemo<BoardMoveImpact | null>(() => {
    const dto = impactQuery.data;
    if (!dto) return null;
    return {
      unchanged: dto.unchanged ?? false,
      targetName: dto.targetSpaceName ?? null,
      targetPeople: dto.targetPeople ?? null,
      losingCount: dto.losingCount ?? null,
      everyoneGains: dto.everyoneGains ?? false,
      everyoneLoses: dto.everyoneLoses ?? false,
      gainingCount: dto.gainingCount ?? 0,
      stranded: (dto.stranded ?? []).flatMap((person) =>
        person.userId
          ? [
              {
                userId: person.userId,
                name: person.name ?? "",
                items: person.items ?? 0,
                overdue: person.overdue ?? 0,
              },
            ]
          : [],
      ),
      strandedCount: dto.strandedCount ?? 0,
      strandedItems: dto.strandedItems ?? 0,
      strandedOverdue: dto.strandedOverdue ?? 0,
      keptNames: dto.keptNames ?? [],
      keptCount: dto.keptCount ?? 0,
    };
  }, [impactQuery.data]);
  const placementOfSubject = subject?.kind === "board" ? subject.spaceId : null;
  // A department is named "Phòng X" on the page, as in the share dialog.
  const placeName = useCallback(
    (node: SpaceNode) =>
      node.type === "Department"
        ? formatCopy(copy.page.departmentName, { name: node.name })
        : node.name,
    [copy],
  );
  const placementNode = placementOfSubject
    ? index.byId.get(placementOfSubject)
    : undefined;
  const fromName = placementNode
    ? placeName(placementNode)
    : (workspace?.name ?? "");
  const places = useMemo<MovePlaceView[]>(() => {
    if (subject?.kind !== "board") return [];
    const spaces = visibleSpaceRows(index, new Set(index.byId.keys()))
      .filter((row) => !row.node.isArchived)
      .map(({ node }): MovePlaceView => ({
        id: node.id,
        name: placeName(node),
        initials: initialsFor(node.name),
        markTone: markToneFor(node.id),
        state:
          node.id === subject.spaceId
            ? "current"
            : targets.spaceIds.has(node.id)
              ? "open"
              : "blocked",
      }));
    return [
      ...spaces,
      {
        id: null,
        name: workspace?.name ?? "",
        initials: "",
        markTone: "blue",
        state:
          subject.spaceId === null
            ? "current"
            : targets.root
              ? "open"
              : "blocked",
      },
    ];
  }, [index, placeName, subject, targets, workspace?.name]);

  const pending = move.isPending || moveBoard.isPending || movePage.isPending;
  const dialog =
    subject?.kind === "board" ? (
      <BoardMoveDialog
        copy={lifecycle.move}
        open
        boardName={subject.name}
        fromName={fromName}
        workspaceName={workspace?.name ?? ""}
        places={places}
        picked={picked}
        onPick={setPicked}
        impactState={
          impactQuery.isError
            ? "error"
            : impactQuery.isSuccess
              ? "ready"
              : "loading"
        }
        impact={boardImpact}
        onRetryImpact={() => void impactQuery.refetch()}
        pending={pending}
        error={error}
        onClose={() => setSubject(null)}
        onSubmit={onSubmit}
      />
    ) : (
      <MoveToSpaceDialog
        copy={copy}
        subject={subject}
        workspaceName={workspace?.name ?? null}
        nodes={tree.data ?? []}
        targets={targets}
        pending={pending}
        error={error}
        onOpenChange={(open) => {
          if (!open) setSubject(null);
        }}
        onSubmit={onSubmit}
      />
    );

  return { openForSpace, openForBoard, openForPage, dialog };
}
