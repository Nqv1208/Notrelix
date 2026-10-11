import { useCallback, useMemo, useState } from "react";
import { toast } from "sonner";
import {
  buildLibraryEntries,
  buildSpaceIndex,
  libraryEntryKey,
  spacePathLabel,
  type LibraryEntry,
} from "@notrelix/features-workspace/core";
import {
  createUseTogglePin,
  createUseWorkspacePins,
  formatCopy,
  useWorkspaceHomeCopy,
  useWorkspaceLibraryCopy,
  WorkspaceLibrarySurface,
  type SectionState,
} from "@notrelix/features-workspace/web";
import { useFormatters } from "@notrelix/i18n";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useBoardPlacements } from "@/shell/workspace/board-placement-bridge";
import { useMoveToSpace } from "@/shell/workspace/use-move-to-space";
import { useSpaceHooks } from "@/shell/workspace/use-workspace-spaces";
import { toastFailure } from "@/shell/workspace/failure-toast";
import { useCreateResource } from "@/shell/workspace/use-create-resource";
import { useNow } from "@/shell/workspace/use-now";
import { useShellNavigation } from "@/shell/workspace/use-shell-navigation";
import { useWorkspaceResources } from "@/shell/workspace/use-workspace-resources";
import { useWorkspaceViewer } from "@/shell/workspace/use-workspace-viewer";

/**
 * Boards and Docs directory (WX-DIR-001): the authorized Board and Doc lists
 * joined with the member's pins. Pins are written one resource at a time;
 * each write is idempotent on the producer, so a partial bulk failure leaves
 * the saved pins accurate and a retry is safe.
 */
export function LibraryPage() {
  const { workspaceId, workspace } = useWorkspaceContext();
  const copy = useWorkspaceLibraryCopy();
  const homeCopy = useWorkspaceHomeCopy();
  const formatters = useFormatters();
  const navigation = useShellNavigation();
  const now = useNow();
  const { api: runtimeClient } = useAppRuntime();
  const hooks = useMemo(() => {
    const deps = { api: runtimeClient.api, endpoints: runtimeClient.endpoints };
    return {
      usePins: createUseWorkspacePins(deps),
      useTogglePin: createUseTogglePin(deps),
    };
  }, [runtimeClient]);

  const viewer = useWorkspaceViewer(workspaceId);
  const resources = useWorkspaceResources(workspaceId);
  const pins = hooks.usePins(workspaceId);
  const togglePin = hooks.useTogglePin(workspaceId);
  const create = useCreateResource(workspaceId, homeCopy);
  const spaceTree = useSpaceHooks().useSpaceTree(workspaceId);
  const mover = useMoveToSpace();
  const placements = useBoardPlacements(workspaceId);
  const spaces = useMemo(
    () => (spaceTree.data ?? []).filter((node) => !node.isArchived),
    [spaceTree.data],
  );
  const [pendingPinKeys, setPendingPinKeys] = useState<ReadonlySet<string>>(
    () => new Set(),
  );

  const spaceIndex = useMemo(
    () => buildSpaceIndex(spaceTree.data ?? []),
    [spaceTree.data],
  );
  // Placements are optional for the table: until they load, Boards show
  // without a Space and the Space filter treats them as unplaced.
  const boardSpace = useMemo(
    () =>
      new Map(
        (placements.data ?? []).map((placement) => [
          placement.id,
          placement.spaceId,
        ]),
      ),
    [placements.data],
  );

  const state = useMemo<SectionState<readonly LibraryEntry[]>>(() => {
    if (resources.failed || pins.isError) return { status: "error" };
    if (!resources.ready || !pins.data) return { status: "loading" };
    return {
      status: "ready",
      data: buildLibraryEntries({
        boards: resources.boards.map((board) => {
          const spaceId = boardSpace.get(board.id) ?? null;
          return {
            id: board.id,
            name: board.name,
            updatedAt: board.updatedAt,
            spaceId,
            location: spaceId
              ? spacePathLabel(spaceIndex, spaceId) || workspace?.name || null
              : (workspace?.name ?? null),
          };
        }),
        docs: resources.docs.map((doc) => ({
          id: doc.id,
          name: doc.name,
          updatedAt: doc.updatedAt,
          spaceId: doc.spaceId,
          location:
            doc.parentName ??
            ((doc.spaceId && spacePathLabel(spaceIndex, doc.spaceId)) ||
              workspace?.name ||
              null),
        })),
        pins: pins.data,
      }),
    };
  }, [
    boardSpace,
    pins.data,
    pins.isError,
    resources,
    spaceIndex,
    workspace?.name,
  ]);

  const onSetPinned = useCallback(
    async (entries: readonly LibraryEntry[], pinned: boolean) => {
      const changes = entries.filter((entry) => entry.pinned !== pinned);
      if (changes.length === 0) return;
      const keys = changes.map(libraryEntryKey);
      setPendingPinKeys((current) => new Set([...current, ...keys]));
      let saved = 0;
      let failure: unknown = null;
      for (const entry of changes) {
        try {
          await togglePin.mutateAsync({
            kind: entry.kind,
            resourceId: entry.id,
            pinned,
          });
          saved += 1;
        } catch (error) {
          failure = error;
          break;
        }
      }
      setPendingPinKeys((current) => {
        const next = new Set(current);
        keys.forEach((key) => next.delete(key));
        return next;
      });
      if (failure) {
        toastFailure(copy.toasts.pinFailed, failure, copy.toasts.pinFailedHint);
        return;
      }
      const forms = pinned
        ? { one: copy.toasts.pinned_one, other: copy.toasts.pinned_other }
        : { one: copy.toasts.unpinned_one, other: copy.toasts.unpinned_other };
      toast.success(
        formatCopy(saved === 1 ? forms.one : forms.other, { count: saved }),
      );
    },
    [copy, togglePin],
  );

  return (
    <>
      <WorkspaceLibrarySurface
        copy={copy}
        navigation={navigation}
        formatters={formatters}
        now={now}
        workspaceName={workspace?.name ?? null}
        state={state}
        pendingPinKeys={pendingPinKeys}
        canCreate={viewer.role !== null && viewer.role !== "guest"}
        spaces={spaces}
        onMoveBoard={
          viewer.role !== null && viewer.role !== "guest" && spaces.length > 0
            ? (entry) => mover.openForBoard({ id: entry.id, name: entry.name })
            : undefined
        }
        onCreateBoard={() => create.open("board")}
        onCreateDoc={() => create.open("doc")}
        onSetPinned={(entries, pinned) => void onSetPinned(entries, pinned)}
        onRetry={() => {
          resources.retry();
          void pins.refetch();
        }}
      />
      {create.dialog}
      {mover.dialog}
    </>
  );
}
