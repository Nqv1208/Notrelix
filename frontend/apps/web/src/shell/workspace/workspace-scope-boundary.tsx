import { useMemo, type ReactNode } from "react";
import { useNavigate } from "@tanstack/react-router";
import {
  createUseWorkspaceList,
  useWorkspaceShellCopy,
  WorkspaceScopeStateSurface,
} from "@notrelix/features-workspace/web";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { resolveWorkspaceScope } from "./workspace-scope";

function errorKind(error: unknown): string | undefined {
  return error && typeof error === "object" && "kind" in error
    ? String((error as { kind: unknown }).kind)
    : undefined;
}

/**
 * Replaces the silent redirect of `shell/guards/workspace-guard.tsx`: a
 * failed load or lost access renders an explicit state with a retry and a
 * way out (WX-SHELL-004).
 */
export function WorkspaceScopeBoundary({ children }: { children: ReactNode }) {
  const navigate = useNavigate();
  const copy = useWorkspaceShellCopy();
  const { api: runtimeClient } = useAppRuntime();
  const workspaceContext = useWorkspaceContext();
  const useWorkspaceList = useMemo(
    () =>
      createUseWorkspaceList({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );
  const list = useWorkspaceList();

  const state = resolveWorkspaceScope({
    workspaceId: workspaceContext.workspaceId,
    list: {
      isLoading: list.isLoading,
      isError: list.isError,
      ids: list.data?.map((workspace) => workspace.id),
    },
    detail: {
      isLoading: workspaceContext.isLoading,
      isError: workspaceContext.isError,
      hasData: workspaceContext.workspace !== null,
      errorKind: errorKind(workspaceContext.error),
    },
  });

  if (state === null) return <>{children}</>;

  return (
    <WorkspaceScopeStateSurface
      copy={copy}
      state={state}
      onRetry={() => {
        void list.refetch();
        void workspaceContext.refetch();
      }}
      exitHref="/home"
      onExit={() => void navigate({ to: "/home" })}
    />
  );
}
