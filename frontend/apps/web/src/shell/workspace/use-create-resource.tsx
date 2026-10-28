import { useCallback, useMemo, useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  createPageApi,
  createTemplateApi,
  docsQueryKeys,
} from "@notrelix/docs-state";
import {
  CreateResourceDialog,
  formatCopy,
  type CreatableResource,
  type WorkspaceHomeCopy,
} from "@notrelix/features-workspace/web";
import { useFeatureRuntimeDependencies } from "@notrelix/runtime-web";
import {
  useAttemptKey,
  useBoardTemplateChoices,
  useCreateBoard,
  useCreateBoardFromTemplate,
} from "./work-management-bridge";

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
  const templateApi = useMemo(
    () => createTemplateApi(api, endpoints),
    [api, endpoints],
  );
  const createBoard = useCreateBoard(workspaceId);
  const createBoardFromTemplate = useCreateBoardFromTemplate(workspaceId);
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
  // The starters and published templates a new Board offers, read only while that dialog is open.
  const boardTemplatesQuery = useBoardTemplateChoices(
    workspaceId,
    kind === "board",
  );
  const boardTemplates = useMemo(
    () =>
      (boardTemplatesQuery.data ?? [])
        .filter((entry) => entry.status === "published")
        .map((entry) => ({
          id: entry.id,
          name: entry.name,
          description: entry.description,
          builtIn: entry.builtInKey !== null,
        })),
    [boardTemplatesQuery.data],
  );
  // The workspace's page templates, read only while the Doc dialog is open.
  const templatesQuery = useQuery({
    queryKey: docsQueryKeys.templates(workspaceId),
    queryFn: () => templateApi.list(workspaceId),
    enabled: kind === "doc" && !!workspaceId,
  });
  const createFromTemplate = useMutation({
    mutationFn: (input: { templateId: string; title: string }) =>
      templateApi.createPage(workspaceId, {
        templateId: input.templateId,
        title: input.title,
        spaceId: space?.id ?? null,
      }),
    onSettled: () =>
      queryClient.invalidateQueries({
        queryKey: docsQueryKeys.list(workspaceId),
      }),
  });
  const [error, setError] = useState<string | null>(null);

  const open = useCallback((next: CreatableResource) => {
    setError(null);
    setKind(next);
  }, []);

  const onSubmit = useCallback(
    (resource: CreatableResource, name: string, templateId?: string) => {
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
      if (resource === "board" && templateId) {
        createBoardFromTemplate.mutate(
          { templateId, name, spaceId: space?.id ?? null },
          { onSuccess: done, onError: failed },
        );
      } else if (resource === "board") {
        createBoard.mutate(
          {
            title: name,
            attemptKey: nextAttemptKey(),
            spaceId: space?.id ?? null,
          },
          { onSuccess: done, onError: failed },
        );
      } else if (templateId) {
        createFromTemplate.mutate(
          { templateId, title: name },
          {
            onSuccess: (made) => {
              done(made.pageId);
              if (made.droppedBlockCount > 0) {
                toast.info(
                  formatCopy(copy.toasts.templateDropped, {
                    count: String(made.droppedBlockCount),
                  }),
                );
              }
            },
            onError: failed,
          },
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
      createBoardFromTemplate,
      createDoc,
      createFromTemplate,
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
      pending={
        createBoard.isPending ||
        createBoardFromTemplate.isPending ||
        createDoc.isPending ||
        createFromTemplate.isPending
      }
      templates={templatesQuery.data?.map((entry) => ({
        id: entry.id,
        name: entry.name,
      }))}
      boardTemplates={boardTemplates}
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
