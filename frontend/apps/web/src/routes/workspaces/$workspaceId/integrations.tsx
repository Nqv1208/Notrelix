import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  IntegrationConnectDialog,
  IntegrationDetailSurface,
  IntegrationDisconnectDialog,
  IntegrationSecretDialog,
  FeatureUnavailableSurface,
  IntegrationsListSurface,
  WorkspaceSetupFrame,
  formatCopy,
  useFeatureAvailabilityCopy,
  useIntegrationsCopy,
  useWorkspaceTeamsCopy,
  type SectionState,
} from "@notrelix/features-workspace/web";
import { backendCapabilities } from "@/config/backend-capabilities";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useSetupFrame } from "@/shell/workspace/use-setup-frame";
import { useWorkspaceViewer } from "@/shell/workspace/use-workspace-viewer";
import type { IntegrationsSearch } from "../../../router/schemas/integrations-search-schema";
import {
  detailOf,
  listOf,
  memberListOf,
} from "./integrations/integrations-view";
import {
  integrationsKeys,
  useIntegrationsApi,
  useIntegrationsLinks,
} from "./integrations/use-integrations";

const ROUTE = "/workspaces/$workspaceId/integrations";

function stateOf<T>(query: {
  data: T | undefined;
  isError: boolean;
}): SectionState<T> {
  return query.data !== undefined
    ? { status: "ready", data: query.data }
    : query.isError
      ? { status: "error" }
      : { status: "loading" };
}

function isForbidden(error: unknown): boolean {
  return (
    typeof error === "object" &&
    error !== null &&
    "kind" in error &&
    error.kind === "forbidden"
  );
}

/**
 * Integrations: the connections in use and the catalog, or one connection when
 * `item` names it; `connect` opens the connect dialog and `disconnect` the
 * confirmation. `connected` and `error` are where the provider's redirect
 * lands the member back.
 */
export function IntegrationsPage() {
  return backendCapabilities.integrations ? (
    <AvailableIntegrationsPage />
  ) : (
    <UnavailableIntegrationsPage />
  );
}

/** Integrations on a backend that has no producer for them yet. */
function UnavailableIntegrationsPage() {
  const copy = useFeatureAvailabilityCopy();
  const teamsCopy = useWorkspaceTeamsCopy();
  const frame = useSetupFrame("integrations");
  return (
    <WorkspaceSetupFrame copy={teamsCopy} frame={frame}>
      <FeatureUnavailableSurface copy={copy} feature="integrations" />
    </WorkspaceSetupFrame>
  );
}

function AvailableIntegrationsPage() {
  const { workspaceId, workspace } = useWorkspaceContext();
  const copy = useIntegrationsCopy();
  const teamsCopy = useWorkspaceTeamsCopy();
  const frame = useSetupFrame("integrations");
  const api = useIntegrationsApi();
  const links = useIntegrationsLinks();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const search: IntegrationsSearch = useSearch({ from: ROUTE });
  const viewer = useWorkspaceViewer(workspaceId);
  const [pollUntil, setPollUntil] = useState(0);
  const [url, setUrl] = useState("");
  const [urlError, setUrlError] = useState<string | null>(null);
  const [secret, setSecret] = useState<string | null>(null);
  const locale = document.documentElement.lang || "vi";

  const list = useQuery({
    queryKey: integrationsKeys.list(workspaceId),
    queryFn: () => api.list(workspaceId),
    retry: false,
    // After a test delivery the result lands a moment later: look for it.
    refetchInterval: () => (Date.now() < pollUntil ? 1000 : false),
  });
  const forbidden = isForbidden(list.error);
  const members = useMemo(
    () =>
      new Map(
        (viewer.members.data ?? []).map((member) => [
          member.userId,
          member.name,
        ]),
      ),
    [viewer.members.data],
  );
  const workspaceName = workspace?.name ?? copy.real.workspaceName;
  const now = new Date(list.dataUpdatedAt || Date.now());
  const listDto = useMemo(
    () =>
      list.data
        ? listOf(list.data, copy, { workspaceName, now, locale, members })
        : forbidden
          ? memberListOf(workspaceName, now, locale)
          : undefined,
    // `now` follows the data it describes.
    [list.data, forbidden, copy, workspaceName, locale, members],
  );
  const canManage = listDto?.canManage ?? true;
  const itemKey = canManage ? search.item : undefined;
  const connection = list.data?.connections?.find(
    (entry) => entry.id === itemKey,
  );
  const detailDto = useMemo(
    () =>
      connection
        ? detailOf(connection, copy, { now, locale, members })
        : undefined,
    [connection, copy, locale, members],
  );

  const refresh = useCallback(
    () =>
      queryClient.invalidateQueries({
        queryKey: integrationsKeys.all(workspaceId),
      }),
    [queryClient, workspaceId],
  );
  const setSearch = useCallback(
    (next: IntegrationsSearch, replace = true) =>
      void navigate({
        to: ROUTE,
        params: { workspaceId },
        search: next,
        replace,
      } as never),
    [navigate, workspaceId],
  );
  const failed = () => toast.error(copy.unavailable);

  // The provider's redirect lands here with `connected` or `error`.
  useEffect(() => {
    if (search.connected === undefined && search.error === undefined) return;
    const entry = listDto?.catalog.find(
      (item) => item.key === search.connected,
    );
    if (search.connected !== undefined) {
      toast.success(
        formatCopy(copy.connected, {
          name: entry?.name ?? copy.real.providers["slack"].name,
        }),
      );
      void refresh();
    } else {
      toast.error(formatCopy(copy.authFailed, { name: "" }).trim());
    }
    setSearch({});
    // Once per arrival.
  }, [search.connected, search.error]);

  const goAuthorize = useMutation({
    mutationFn: async (provider: string) => {
      const result = await api.authorize(workspaceId, provider);
      window.location.assign(result.authorizeUrl);
    },
    onError: failed,
  });
  const pause = useMutation({
    mutationFn: (paused: boolean) =>
      paused
        ? api.pause(workspaceId, itemKey ?? "")
        : api.resume(workspaceId, itemKey ?? ""),
    onSuccess: async (_data, paused) => {
      await refresh();
      toast.success(paused ? copy.paused : copy.resumed);
    },
    onError: failed,
  });
  const test = useMutation({
    mutationFn: () => api.test(workspaceId, itemKey ?? ""),
    onSuccess: () => {
      setPollUntil(Date.now() + 15_000);
      toast.message(copy.testQueued);
    },
    onError: failed,
  });
  const disconnect = useMutation({
    mutationFn: () => api.disconnect(workspaceId, itemKey ?? ""),
    onSuccess: async () => {
      toast.success(
        formatCopy(copy.disconnected, { name: detailDto?.name ?? "" }),
      );
      await refresh();
      setSearch({}, false);
    },
    onError: failed,
  });
  const connectItem =
    listDto?.catalog.find((entry) => entry.key === search.connect) ?? null;
  const isWebhook = connectItem?.key === "webhook";
  const connectWebhook = useMutation({
    mutationFn: () => api.connectWebhook(workspaceId, url.trim()),
    onSuccess: async (created) => {
      setSecret(created.signingSecret);
      setUrl("");
      setUrlError(null);
      await refresh();
      setSearch({});
    },
    onError: (error) =>
      setUrlError(
        (error as { message?: string }).message?.trim() || copy.unavailable,
      ),
  });

  const body = itemKey ? (
    <IntegrationDetailSurface
      copy={copy}
      links={links}
      state={
        detailDto
          ? { status: "ready", data: detailDto }
          : list.isError
            ? { status: "error" }
            : { status: "loading" }
      }
      busy={false}
      onReload={() => void list.refetch()}
      onReauthorize={() =>
        connection?.provider && goAuthorize.mutate(connection.provider)
      }
      onDisconnect={() => setSearch({ item: itemKey, disconnect: true })}
      onTest={() => test.mutate()}
      onPause={(paused) => pause.mutate(paused)}
      onRegenerate={() => undefined}
    />
  ) : (
    <IntegrationsListSurface
      copy={copy}
      links={links}
      state={stateOf({
        data: listDto,
        isError: list.isError && !forbidden,
      })}
      onReload={() => void list.refetch()}
      onConnect={(key) => {
        setUrl("");
        setUrlError(null);
        setSearch({ connect: key }, false);
      }}
    />
  );

  return (
    <WorkspaceSetupFrame
      copy={teamsCopy}
      frame={frame}
      only={listDto && !listDto.canManage ? "integrations" : undefined}
      title={listDto && !listDto.canManage ? copy.memberTitle : undefined}
    >
      {body}
      <IntegrationConnectDialog
        copy={copy}
        links={links}
        open={canManage && connectItem !== null}
        item={connectItem}
        workspaceName={workspaceName}
        scope="workspace"
        hideScope
        submitting={connectWebhook.isPending || goAuthorize.isPending}
        onScope={() => undefined}
        onNext={() =>
          isWebhook
            ? connectWebhook.mutate()
            : connectItem && goAuthorize.mutate(connectItem.key)
        }
        onClose={() => setSearch({})}
        {...(isWebhook
          ? { url: { value: url, error: urlError, onChange: setUrl } }
          : {})}
      />
      <IntegrationSecretDialog
        copy={copy}
        links={links}
        open={secret !== null}
        secret={secret}
        onClose={() => setSecret(null)}
      />
      <IntegrationDisconnectDialog
        copy={copy}
        links={links}
        open={canManage && search.disconnect === true && itemKey !== undefined}
        detail={detailDto ?? null}
        submitting={disconnect.isPending}
        onConfirm={() => disconnect.mutate()}
        onClose={() => setSearch({ item: itemKey })}
      />
    </WorkspaceSetupFrame>
  );
}
