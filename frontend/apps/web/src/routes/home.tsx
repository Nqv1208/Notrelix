import { useMemo } from "react";
import { Link } from "@tanstack/react-router";
import { Activity, Clock3, FileText, Search, SquareKanban } from "lucide-react";
import { createUseDocsFavorites, createUsePageList } from "@notrelix/docs-state";
import {
  createUseWorkspaceList,
  WorkspaceDirectory,
} from "@notrelix/features-workspace/web";
import type { WorkspaceSummary } from "@notrelix/features-workspace/core";
import { useFeatureRuntimeDependencies, useAppRuntime } from "@notrelix/runtime-web";
import { useWorkspaceBoards } from "@notrelix/work-management-state";
import { Button, Skeleton } from "@notrelix/ui-web";
import { AuthGuard } from "@/shell/guards/auth-guard";
import { HomeShell } from "@/shell/home-shell";

function HomeContent({
  workspaceId,
  workspaces,
  pages,
  boards,
  pagesLoading,
  boardsLoading,
}: {
  workspaceId: string;
  workspaces: readonly WorkspaceSummary[];
  pages: readonly { id: string; title: string }[];
  boards: readonly { id: string; title: string; description?: string }[];
  pagesLoading: boolean;
  boardsLoading: boolean;
}) {
  return (
    <div className="mx-auto max-w-[1240px] space-y-6">
      <section className="rounded-2xl border border-border bg-card p-6 shadow-[rgba(205,208,223,0.22)_0px_2px_24px]">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <div className="mb-3 inline-flex items-center gap-2 rounded-full border border-border bg-muted px-3 py-1 text-xs font-medium text-muted-foreground">
              <Clock3 className="size-3.5 text-primary" />
              Work hub
            </div>
            <h1 className="text-3xl font-semibold tracking-[-0.015em] text-foreground">
              Home
            </h1>
            <p className="mt-2 max-w-2xl text-sm leading-6 text-muted-foreground">
              Jump back into recent workspaces, docs, boards, and team updates.
            </p>
          </div>
          <Button variant="outline" className="w-fit bg-card">
            <Search className="size-4" />
            Search all work
          </Button>
        </div>
      </section>

      <WorkspaceDirectory workspaces={workspaces} />

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_360px]">
        <section className="rounded-2xl border border-border bg-card p-5">
          <div className="mb-4 flex items-center gap-2">
            <FileText className="size-4 text-primary" />
            <h2 className="text-sm font-semibold text-foreground">Recent docs</h2>
          </div>
          {pagesLoading ? (
            <div className="grid gap-3 md:grid-cols-3">
              {[0, 1, 2].map((item) => (
                <Skeleton key={item} className="h-28 rounded-xl" />
              ))}
            </div>
          ) : pages.length === 0 ? (
            <p className="text-sm text-muted-foreground">No recent documents.</p>
          ) : (
            <div className="grid gap-3 md:grid-cols-3">
              {pages.slice(0, 3).map((page) => (
                <Link
                  key={page.id}
                  to="/workspaces/$workspaceId/docs/$docId"
                  params={{ workspaceId, docId: page.id }}
                  className="rounded-xl border border-border bg-muted p-4 transition hover:bg-card"
                >
                  <span className="mb-4 block text-2xl">📝</span>
                  <h3 className="line-clamp-1 text-sm font-semibold text-foreground">
                    {page.title}
                  </h3>
                  <p className="mt-1 text-xs text-muted-foreground">
                    Recently updated
                  </p>
                </Link>
              ))}
            </div>
          )}
        </section>

        <section className="rounded-2xl border border-border bg-card p-5">
          <div className="mb-4 flex items-center gap-2">
            <Activity className="size-4 text-primary" />
            <h2 className="text-sm font-semibold text-foreground">Activity</h2>
          </div>
          <p className="text-sm text-muted-foreground">
            No recent workspace activity.
          </p>
        </section>
      </div>

      <section className="rounded-2xl border border-border bg-card p-5">
        <div className="mb-4 flex items-center gap-2">
          <SquareKanban className="size-4 text-primary" />
          <h2 className="text-sm font-semibold text-foreground">Recent boards</h2>
        </div>
        {boardsLoading ? (
          <div className="grid gap-3 md:grid-cols-3">
            {[0, 1, 2].map((item) => (
              <Skeleton key={item} className="h-24 rounded-xl" />
            ))}
          </div>
        ) : boards.length === 0 ? (
          <p className="text-sm text-muted-foreground">No recent boards.</p>
        ) : (
          <div className="grid gap-3 md:grid-cols-3">
            {boards.slice(0, 3).map((board) => (
              <Link
                key={board.id}
                to="/workspaces/$workspaceId/boards/$boardId"
                params={{ workspaceId, boardId: board.id }}
                className="rounded-xl border border-border p-4 transition hover:bg-muted"
              >
                <div className="mb-3 flex items-center gap-2">
                  <span className="size-2 rounded-full bg-primary" />
                  <h3 className="text-sm font-semibold text-foreground">
                    {board.title}
                  </h3>
                </div>
                <p className="mt-3 text-xs text-muted-foreground">
                  {board.description || "Recently updated"}
                </p>
              </Link>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

export function HomePage() {
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
  const { data: workspaces = [], isLoading, isError, refetch } =
    useWorkspaceList();
  const primaryWorkspaceId = workspaces[0]?.id ?? "";
  const usePageList = useMemo(
    () => createUsePageList(api, endpoints),
    [api, endpoints],
  );
  const useDocsFavorites = useMemo(
    () => createUseDocsFavorites(api, endpoints),
    [api, endpoints],
  );
  const { data: pages = [], isLoading: pagesLoading } =
    usePageList(primaryWorkspaceId);
  const { data: favoriteDocs = [] } = useDocsFavorites(primaryWorkspaceId);
  const { data: boards = [], isLoading: boardsLoading } =
    useWorkspaceBoards(primaryWorkspaceId);

  const shellData = {
    workspaces,
    favoriteDocs: favoriteDocs.map((page) => ({
      id: page.id,
      title: page.title,
      workspaceId: primaryWorkspaceId,
    })),
    recentDocs: pages.map((page) => ({
      id: page.id,
      title: page.title,
      workspaceId: primaryWorkspaceId,
    })),
    recentBoards: boards.map((board) => ({
      id: board.id,
      title: board.title,
      workspaceId: primaryWorkspaceId,
    })),
  };

  return (
    <AuthGuard>
      <HomeShell data={shellData}>
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
          <HomeContent
            workspaceId={primaryWorkspaceId}
            workspaces={workspaces}
            pages={pages}
            boards={boards}
            pagesLoading={pagesLoading}
            boardsLoading={boardsLoading}
          />
        )}
      </HomeShell>
    </AuthGuard>
  );
}
