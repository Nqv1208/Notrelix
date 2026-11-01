import { useMemo, useState } from "react";
import { Navigate, useNavigate } from "@tanstack/react-router";
import { CloudOff, LayoutGrid } from "lucide-react";
import {
  createUseWorkspaceList,
  useWorkspaceShellCopy,
} from "@notrelix/features-workspace/web";
import { createUseAccountWorkspaces } from "@notrelix/features-account";
import { useAppRuntime } from "@notrelix/runtime-web";
import {
  Button,
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
  NotrelixLogoMark,
  Skeleton,
} from "@notrelix/ui-web";
import { AuthGuard } from "@/shell/guards/auth-guard";
import { CreateWorkspaceDialog } from "@/shell/workspace/create-workspace-dialog";

/**
 * Account-level landing (`/home`). The workspace shell is workspace-scoped,
 * so `/home` resolves into a workspace instead of rendering a second shell:
 * members with a workspace land on the one they chose as default (the first
 * by name when they chose none or the choice cannot be read); members without
 * one get an onboarding state. The duplicate home sidebar and switcher are retired.
 */
function HomeLanding() {
  const copy = useWorkspaceShellCopy();
  const navigate = useNavigate();
  const { api: runtimeClient } = useAppRuntime();
  const useWorkspaceList = useMemo(
    () =>
      createUseWorkspaceList({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );
  const useAccountWorkspaces = useMemo(
    () =>
      createUseAccountWorkspaces({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );
  const {
    data: workspaces,
    isLoading: listLoading,
    isError,
    refetch,
  } = useWorkspaceList();
  const account = useAccountWorkspaces();
  const [createOpen, setCreateOpen] = useState(false);

  const first = workspaces?.[0];
  // The default is a hint: the person's own choice opens when it is still one they can open; a list
  // that cannot be read must not keep them out, so a failed read falls back to the first.
  const defaultSettled = account.workspaces !== undefined || account.isError;
  const isLoading = listLoading || (first !== undefined && !defaultSettled);
  const chosen = account.workspaces?.find(
    (workspace) => workspace.isDefault && !workspace.isArchived,
  );
  const target =
    workspaces?.find((workspace) => workspace.id === chosen?.id) ?? first;
  // A member with no workspace yet sets one up (the Onboarding mockup).
  if (!first && !isLoading && !isError) {
    return <Navigate to="/onboarding" replace />;
  }
  if (target && !isLoading) {
    return (
      <Navigate
        to="/workspaces/$workspaceId"
        params={{ workspaceId: target.id }}
        replace
      />
    );
  }

  return (
    <main className="flex min-h-svh flex-col bg-surface-canvas">
      <header className="flex h-12 items-center border-b border-border px-4">
        <NotrelixLogoMark className="h-5 w-auto" aria-hidden="true" />
        <span className="ml-2 text-label text-foreground">Notrelix</span>
      </header>
      <div className="flex flex-1 items-center justify-center p-4">
        {isLoading ? (
          <div
            role="status"
            aria-label={copy.landing.loadingTitle}
            className="flex w-full max-w-sm flex-col gap-3"
          >
            <Skeleton className="h-6 w-2/3" />
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-5/6" />
          </div>
        ) : isError ? (
          <Empty className="max-w-md border-0">
            <EmptyHeader>
              <EmptyMedia variant="icon">
                <CloudOff aria-hidden="true" className="text-icon" />
              </EmptyMedia>
              <EmptyTitle>
                <h1 className="text-section-heading text-foreground">
                  {copy.landing.errorTitle}
                </h1>
              </EmptyTitle>
              <EmptyDescription className="text-body">
                {copy.landing.errorDescription}
              </EmptyDescription>
            </EmptyHeader>
            <EmptyContent>
              <Button onClick={() => void refetch()}>
                {copy.landing.retry}
              </Button>
            </EmptyContent>
          </Empty>
        ) : (
          <Empty className="max-w-md border-0">
            <EmptyHeader>
              <EmptyMedia variant="icon">
                <LayoutGrid aria-hidden="true" className="text-icon" />
              </EmptyMedia>
              <EmptyTitle>
                <h1 className="text-section-heading text-foreground">
                  {copy.landing.emptyTitle}
                </h1>
              </EmptyTitle>
              <EmptyDescription className="text-body">
                {copy.landing.emptyDescription}
              </EmptyDescription>
            </EmptyHeader>
            <EmptyContent>
              <Button onClick={() => setCreateOpen(true)}>
                {copy.createWorkspace.submit}
              </Button>
            </EmptyContent>
          </Empty>
        )}
      </div>
      <CreateWorkspaceDialog
        copy={copy}
        open={createOpen}
        onOpenChange={setCreateOpen}
        onCreated={(created) =>
          void navigate({
            to: "/workspaces/$workspaceId",
            params: { workspaceId: created.id },
          })
        }
      />
    </main>
  );
}

export function HomePage() {
  return (
    <AuthGuard>
      <HomeLanding />
    </AuthGuard>
  );
}
