import { useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";
import {
  createUseDocsFavorites,
  createUsePageList,
} from "@notrelix/docs-state";
import { useCurrentUser } from "@notrelix/features-auth";
import {
  createUseCreateWorkspace,
  createUseWorkspaceList,
} from "@notrelix/features-workspace/web";
import {
  useAppRuntime,
  useFeatureRuntimeDependencies,
} from "@notrelix/runtime-web";
import { Button, Skeleton } from "@notrelix/ui-web";
import { useWorkspaceBoards } from "@notrelix/work-management-state";
import { HomeSurface } from "@/home/home-surface";
import type { HomeResourceItem, HomeViewModel } from "@/home/home-model";
import { AuthGuard } from "@/shell/guards/auth-guard";
import { HomeShell } from "@/shell/home-shell";

function slugifyWorkspaceName(name: string): string {
  return (
    name
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/(^-|-$)/g, "") || "workspace"
  );
}

export function HomePage() {
  const navigate = useNavigate();
  const user = useCurrentUser();
  const { api: runtimeClient } = useAppRuntime();
  const { api, endpoints } = useFeatureRuntimeDependencies();

  const useWorkspaceList = useMemo(
    () =>
      createUseWorkspaceList({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );
  const useCreateWorkspace = useMemo(
    () =>
      createUseCreateWorkspace({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );
  const usePageList = useMemo(
    () => createUsePageList(api, endpoints),
    [api, endpoints],
  );
  const useDocsFavorites = useMemo(
    () => createUseDocsFavorites(api, endpoints),
    [api, endpoints],
  );

  const {
    data: workspaces = [],
    isLoading,
    isError,
    refetch,
  } = useWorkspaceList();
  const primaryWorkspace = workspaces[0];
  const primaryWorkspaceId = primaryWorkspace?.id ?? "";
  const primaryWorkspaceName = primaryWorkspace?.name ?? "Workspace";
  const { data: pages = [] } = usePageList(primaryWorkspaceId);
  const { data: favoriteDocs = [] } = useDocsFavorites(primaryWorkspaceId);
  const { data: boards = [] } = useWorkspaceBoards(primaryWorkspaceId);
  const createWorkspaceMutation = useCreateWorkspace();

  const viewModel = useMemo<HomeViewModel>(() => {
    const pageItems: HomeResourceItem[] = pages.map((page) => ({
      id: page.id,
      kind: "doc",
      title: page.title,
      workspaceId: primaryWorkspaceId,
      workspaceName: primaryWorkspaceName,
      subtitle: "Document",
      updatedLabel: "Recently updated",
    }));
    const boardItems: HomeResourceItem[] = boards.map((board) => ({
      id: board.id,
      kind: "board",
      title: board.title,
      workspaceId: primaryWorkspaceId,
      workspaceName: primaryWorkspaceName,
      subtitle: board.description || "Board",
      updatedLabel: "Recently updated",
    }));

    return {
      userName: user?.name || "there",
      workspaces,
      favoriteDocs: favoriteDocs.map((page) => ({
        id: page.id,
        kind: "doc",
        title: page.title,
        workspaceId: primaryWorkspaceId,
        workspaceName: primaryWorkspaceName,
        subtitle: "Document",
        updatedLabel: "Recently updated",
      })),
      continueItems: [...pageItems, ...boardItems],
      tasks: {
        status: "unavailable",
        message:
          "Tasks will appear here when the workspace task feed is available.",
      },
      activity: {
        status: "unavailable",
        message:
          "Activity will appear here when the workspace activity feed is available.",
      },
    };
  }, [
    boards,
    favoriteDocs,
    pages,
    primaryWorkspaceId,
    primaryWorkspaceName,
    user?.name,
    workspaces,
  ]);

  const shellData = useMemo(
    () => ({
      workspaces: viewModel.workspaces,
      favoriteDocs: viewModel.favoriteDocs.map((item) => ({
        id: item.id,
        title: item.title,
        workspaceId: item.workspaceId,
      })),
      recentDocs: viewModel.continueItems
        .filter((item) => item.kind === "doc")
        .map((item) => ({
          id: item.id,
          title: item.title,
          workspaceId: item.workspaceId,
        })),
      recentBoards: viewModel.continueItems
        .filter((item) => item.kind === "board")
        .map((item) => ({
          id: item.id,
          title: item.title,
          workspaceId: item.workspaceId,
        })),
    }),
    [viewModel],
  );

  const openResource = (resource: HomeResourceItem) => {
    if (resource.kind === "board") {
      navigate({
        to: "/workspaces/$workspaceId/boards/$boardId",
        params: { workspaceId: resource.workspaceId, boardId: resource.id },
      });
      return;
    }

    navigate({
      to: "/workspaces/$workspaceId/docs/$docId",
      params: { workspaceId: resource.workspaceId, docId: resource.id },
    });
  };

  const openWorkspace = (workspaceId: string) => {
    navigate({ to: "/workspaces/$workspaceId", params: { workspaceId } });
  };

  const createWorkspace = (name: string) => {
    createWorkspaceMutation.mutate(
      {
        name,
        slug: slugifyWorkspaceName(name),
        isPersonal: false,
      },
      {
        onSuccess: (workspace) => {
          navigate({
            to: "/workspaces/$workspaceId",
            params: { workspaceId: workspace.id },
          });
        },
      },
    );
  };

  return (
    <AuthGuard>
      <HomeShell data={shellData} onCreateWorkspace={createWorkspace}>
        {isLoading ? (
          <div className="mx-auto max-w-[1240px] space-y-6">
            <Skeleton className="h-40 rounded-2xl" />
            <div className="grid gap-3 md:grid-cols-3">
              {[0, 1, 2].map((item) => (
                <Skeleton key={item} className="h-36 rounded-2xl" />
              ))}
            </div>
          </div>
        ) : isError ? (
          <div className="mx-auto max-w-[1240px] rounded-2xl border border-border bg-card p-6">
            <h1 className="text-xl font-semibold">Unable to load workspaces</h1>
            <p className="mt-2 text-sm text-muted-foreground">
              An error occurred while connecting to the workspace service.
            </p>
            <Button className="mt-4" onClick={() => refetch()}>
              Retry
            </Button>
          </div>
        ) : (
          <HomeSurface
            data={viewModel}
            onOpenResource={openResource}
            onOpenWorkspace={openWorkspace}
          />
        )}
      </HomeShell>
    </AuthGuard>
  );
}
