import { useCallback, useMemo, useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { createPageApi, docsQueryKeys } from "@notrelix/docs-state";
import {
  CreateResourceDialog,
  formatCopy,
  type CreatableResource,
  type WorkspaceHomeCopy,
} from "@notrelix/features-workspace/web";
import { useFeatureRuntimeDependencies } from "@notrelix/runtime-web";
import { useAttemptKey, useCreateBoard } from "./work-management-bridge";

/**
 * Create a Board or a Doc from a workspace page (WX-HOME-002, WX-DIR-001):
 * the dialog state, the producer call, then navigation to the new resource.
 * A failure keeps the dialog open with the typed name.
 */
export function useCreateResource(
  workspaceId: string,
  copy: WorkspaceHomeCopy,
  /** Create in this Space (OT-5); omitted = workspace level. */
  space: { readonly id: string; readonly name: string } | null = null,
) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { api, endpoints } = useFeatureRuntimeDependencies();
  const pageApi = useMemo(
    () => createPageApi(api, endpoints),
    [api, endpoints],
  );
  const createBoard = useCreateBoard(workspaceId);
  const nextAttemptKey = useAttemptKey();
  const createDoc = useMutation({
    // Called as a method: `create` reads its sibling `getDetail` via `this`.
    mutationFn: (title: string) =>
      pageApi.create({ title, workspaceId, spaceId: space?.id ?? null }),
    onSettled: () =>
      queryClient.invalidateQueries({
        queryKey: docsQueryKeys.list(workspaceId),
      }),
  });

  const [kind, setKind] = useState<CreatableResource | null>(null);
  const [error, setError] = useState<string | null>(null);

  const open = useCallback((next: CreatableResource) => {
    setError(null);
    setKind(next);
  }, []);

  const onSubmit = useCallback(
    (resource: CreatableResource, name: string) => {
      setError(null);
      const done = (id: string | null) => {
        setKind(null);
        toast.success(formatCopy(copy.toasts.created, { name }));
        if (!id) return;
        if (resource === "board") {
          void navigate({
            to: "/workspaces/$workspaceId/boards/$boardId",
            params: { workspaceId, boardId: id },
          } as never);
        } else {
          void navigate({
            to: "/workspaces/$workspaceId/docs/$docId",
            params: { workspaceId, docId: id },
          } as never);
        }
      };
      const failed = () => setError(copy.create.error);
      if (resource === "board") {
        createBoard.mutate(
          {
            title: name,
            attemptKey: nextAttemptKey(),
            spaceId: space?.id ?? null,
          },
          { onSuccess: done, onError: failed },
        );
      } else {
        createDoc.mutate(name, {
          onSuccess: (page) => done(page.id),
          onError: failed,
        });
      }
    },
    [
      copy,
      createBoard,
      createDoc,
      navigate,
      nextAttemptKey,
      space,
      workspaceId,
    ],
  );

  const dialog = (
    <CreateResourceDialog
      copy={copy}
      kind={kind}
      pending={createBoard.isPending || createDoc.isPending}
      error={error}
      spaceName={space?.name ?? null}
      onOpenChange={(next) => {
        if (!next) setKind(null);
      }}
      onSubmit={onSubmit}
    />
  );

  return { open, dialog };
}
