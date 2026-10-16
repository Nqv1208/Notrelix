import { useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";
import { workspaceQueryKey } from "@notrelix/query";
import { useAppRuntime } from "@notrelix/runtime-web";
import type { IntegrationsLinks } from "@notrelix/features-workspace/web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import {
  createIntegrationsApi,
  type IntegrationsApiClient,
} from "./integrations-api";

export function useIntegrationsApi() {
  const { api: runtimeClient } = useAppRuntime();
  return useMemo(
    () =>
      createIntegrationsApi(
        runtimeClient.api as unknown as IntegrationsApiClient,
      ),
    [runtimeClient],
  );
}

export const integrationsKeys = {
  all: (workspaceId: string) => workspaceQueryKey(workspaceId, "integrations"),
  list: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "integrations", "list"),
  detail: (workspaceId: string, key: string) =>
    workspaceQueryKey(workspaceId, "integrations", "detail", key),
} as const;

const ROUTE = "/workspaces/$workspaceId/integrations";

/** Where the Integrations pages and their neighbours live. */
export function useIntegrationsLinks(): IntegrationsLinks {
  const { workspaceId } = useWorkspaceContext();
  const navigate = useNavigate();
  return useMemo(() => {
    const base = `/workspaces/${encodeURIComponent(workspaceId)}`;
    const page = (search?: Record<string, string>) => ({
      href: `${base}/integrations${search ? `?${new URLSearchParams(search).toString()}` : ""}`,
      onNavigate: () =>
        void navigate({ to: ROUTE, params: { workspaceId }, search } as never),
    });
    return {
      list: page(),
      detail: (key) => page({ item: key }),
      billing: {
        href: `${base}/billing`,
        onNavigate: () =>
          void navigate({
            to: "/workspaces/$workspaceId/billing",
            params: { workspaceId },
          } as never),
      },
      automation: (boardId) => ({
        href: `${base}/boards/${encodeURIComponent(boardId)}/settings?section=automations`,
        onNavigate: () =>
          void navigate({
            to: "/workspaces/$workspaceId/boards/$boardId/settings",
            params: { workspaceId, boardId },
            search: { section: "automations" },
          } as never),
      }),
    };
  }, [navigate, workspaceId]);
}
