import { useCallback, useMemo, useState } from "react";
import { toast } from "sonner";
import {
  appendPlacement,
  buildSpaceIndex,
  describeWorkspaceFailure,
  moveTargets,
  type SpaceNode,
} from "@notrelix/features-workspace/core";
import {
  formatCopy,
  MoveToSpaceDialog,
  useWorkspaceSpacesCopy,
  type MoveToSpaceSubject,
} from "@notrelix/features-workspace/web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import {
  useBoardPlacements,
  useMoveBoardToSpace,
} from "./board-placement-bridge";
import { useWorkspaceViewer } from "./use-workspace-viewer";
import { spaceRecoveryHint, useSpaceHooks } from "./use-workspace-spaces";
import { useAttemptKey } from "./work-management-bridge";

/**
 * Move a Space (with its subtree, WSP-021) or a Board (GOV-028) to another
 * place in the org tree. Both moves are optimistic with rollback; the
 * producer authorizes the source and the target, and a stale version shows
 * as a conflict in the dialog after the latest tree is reloaded.
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
  const [subject, setSubject] = useState<MoveToSpaceSubject | null>(null);
  const [error, setError] = useState<string | null>(null);

  const index = useMemo(() => buildSpaceIndex(tree.data ?? []), [tree.data]);
  const targets = useMemo(
    () =>
      moveTargets(
        index,
        subject?.kind === "space"
          ? { kind: "space", spaceId: subject.space.id }
          : { kind: "board" },
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

  const onFailure = useCallback(
    (failure: unknown) => {
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
    (target: string | null) => {
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
      const placement = placements.data?.find((item) => item.id === subject.id);
      moveBoard.mutate(
        {
          boardId: subject.id,
          spaceId: target,
          expectedVersion: placement?.version ?? 0,
          attemptKey: nextAttemptKey(),
        },
        { onSuccess: () => done(subject.name), onError: onFailure },
      );
    },
    [
      copy,
      index,
      move,
      moveBoard,
      nextAttemptKey,
      onFailure,
      placements.data,
      subject,
      workspace?.name,
    ],
  );

  const dialog = (
    <MoveToSpaceDialog
      copy={copy}
      subject={subject}
      workspaceName={workspace?.name ?? null}
      nodes={tree.data ?? []}
      targets={targets}
      pending={move.isPending || moveBoard.isPending}
      error={error}
      onOpenChange={(open) => {
        if (!open) setSubject(null);
      }}
      onSubmit={onSubmit}
    />
  );

  return { openForSpace, openForBoard, dialog };
}
